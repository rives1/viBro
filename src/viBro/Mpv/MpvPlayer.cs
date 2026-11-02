using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ViBro.Mpv;

internal sealed record MediaInfo(string? Codec, long Width, long Height, string? Hwdec, string? Vo);

internal sealed record LoadResult(string Path, string Kind, double ElapsedMs, double Duration, MediaInfo Info, object? Tag);

internal sealed record SeekResult(double ElapsedMs, double Target, double Delta, bool Exact, int Coalesced, bool TimedOut,
    double? Landed, object? Tag);

/// <summary>
/// One long-lived libmpv instance rendering into a native child window.
/// Public methods are called from the UI thread and never block on mpv
/// (all commands are asynchronous). Events are raised on the mpv event thread.
/// </summary>
internal sealed class MpvPlayer : IDisposable
{
    private const ulong UdOther = 0, UdSeek = 2;
    private const ulong UdLoadBase = 1UL << 32; // loadfile requests carry UdLoadBase + load sequence
    private const long AnyEntry = -2;           // fallback when mpv does not report the entry id
    private const ulong ObsTimePos = 1, ObsDuration = 2, ObsPause = 3, ObsEof = 4, ObsVolume = 5, ObsMute = 6, ObsIdle = 7;
    private static readonly TimeSpan SeekTimeout = TimeSpan.FromSeconds(3);

    private IntPtr _handle;
    private Thread? _eventThread;
    private volatile bool _stopping;
    private readonly object _gate = new();

    // Observed state, written by the event thread.
    private double _timePos;
    private double _duration;
    private double _volume = 100;
    private volatile bool _paused = true;
    private volatile bool _eof;
    private volatile bool _muted;
    private volatile bool _idle = true;
    private TaskCompletionSource<bool>? _releaseTcs;

    // Load tracking (guarded by _gate). The reply to "loadfile" reports the playlist entry id
    // of the new file; START_FILE / END_FILE carry the same id. Files replaced before they
    // start produce no START_FILE at all, so ids (not counts) identify the newest request.
    private long _loadSeq;
    private long _expectedEntryId = -1;
    private long _lastStartedEntryId = -1;
    private PendingLoad? _pendingLoad;
    private bool _loadArmed;
    private bool _hasFile;
    private object? _currentTag;

    // Seek tracking (guarded by _gate). At most one seek is in flight; newer requests
    // replace the queued one, so key auto-repeat never builds a backlog.
    private bool _seekInFlight;
    private double _inFlightTarget;
    private double _inFlightDelta;
    private bool _inFlightExact;
    private int _inFlightCoalesced;
    private double? _queuedTarget;
    private bool _queuedExact;
    private int _queuedCount;
    private readonly Stopwatch _seekWatch = new();

    private sealed class PendingLoad
    {
        public PendingLoad(string path, string kind, object? tag)
        {
            Path = path;
            Kind = kind;
            Tag = tag;
        }

        public string Path { get; }
        public string Kind { get; }
        public object? Tag { get; }
        public Stopwatch Watch { get; } = Stopwatch.StartNew();
    }

    public event Action<LoadResult>? LoadCompleted;
    public event Action<string, string>? LoadFailed;
    public event Action<SeekResult>? SeekCompleted;
    /// <summary>mpv messages at level "error" or worse (level, module, text).</summary>
    public event Action<string, string, string>? EngineMessage;
    /// <summary>A file that was already playing stopped with an error (e.g. network share lost).</summary>
    public event Action<string>? PlaybackFailed;

    public List<string> OptionErrors { get; } = new();

    public double TimePos => Volatile.Read(ref _timePos);
    public double Duration => Volatile.Read(ref _duration);
    public double Volume => Volatile.Read(ref _volume);
    public bool IsPaused => _paused;
    public bool IsMuted => _muted;
    public bool IsEof => _eof;

    public bool HasFile
    {
        get { lock (_gate) return _hasFile; }
    }

    public MpvPlayer(IntPtr wid, IEnumerable<KeyValuePair<string, string>> options)
    {
        _handle = MpvNative.mpv_create();
        if (_handle == IntPtr.Zero)
            throw new InvalidOperationException("mpv_create() failed.");

        try
        {
            long w = wid.ToInt64();
            Check(MpvNative.mpv_set_option(_handle, "wid", MpvFormat.Int64, ref w), "wid");

            foreach (var kv in options)
            {
                int r = MpvNative.mpv_set_option_string(_handle, kv.Key, kv.Value);
                if (r < 0)
                    OptionErrors.Add($"{kv.Key}={kv.Value}: {MpvNative.ErrorString(r)}");
            }

            Check(MpvNative.mpv_initialize(_handle), "initialize");
            MpvNative.mpv_request_log_messages(_handle, "error");

            Observe(ObsTimePos, "time-pos", MpvFormat.Double);
            Observe(ObsDuration, "duration", MpvFormat.Double);
            Observe(ObsPause, "pause", MpvFormat.Flag);
            Observe(ObsEof, "eof-reached", MpvFormat.Flag);
            Observe(ObsVolume, "volume", MpvFormat.Double);
            Observe(ObsMute, "mute", MpvFormat.Flag);
            Observe(ObsIdle, "idle-active", MpvFormat.Flag);
        }
        catch
        {
            MpvNative.mpv_terminate_destroy(_handle);
            _handle = IntPtr.Zero;
            throw;
        }
    }

    public string ApiVersion
    {
        get
        {
            ulong v = MpvNative.mpv_client_api_version();
            return $"{v >> 16}.{v & 0xFFFF}";
        }
    }

    public void Start()
    {
        _eventThread = new Thread(EventLoop) { IsBackground = true, Name = "mpv-events" };
        _eventThread.Start();
    }

    // ---------------------------------------------------------------- commands (UI thread)

    /// <summary>Loads a file and shows its first frame without starting playback.</summary>
    public void Load(string path, object? tag)
    {
        lock (_gate)
        {
            if (_handle == IntPtr.Zero) return;
            string kind = (_hasFile || _pendingLoad != null) ? "switch" : "load";
            _pendingLoad = new PendingLoad(path, kind, tag);
            _loadArmed = false;
            _loadSeq++;
            _expectedEntryId = -1;
            _hasFile = false;
            _currentTag = tag;
            ResetSeekLocked();
            Volatile.Write(ref _duration, 0);
            Volatile.Write(ref _timePos, 0);

            // Requests are processed by mpv in order: pause first, then replace the file.
            Command(UdOther, "set", "pause", "yes");
            Command(UdLoadBase + (ulong)_loadSeq, "loadfile", path, "replace");
        }
    }

    /// <summary>
    /// Unloads the current file so that it can be renamed, moved or deleted. Completes when mpv
    /// reports the end of the file or is idle (callers also wait for the OS handle to close).
    /// </summary>
    public Task ReleaseFileAsync()
    {
        TaskCompletionSource<bool> tcs;
        bool wasActive;
        lock (_gate)
        {
            if (_handle == IntPtr.Zero) return Task.CompletedTask;
            wasActive = _hasFile || _pendingLoad != null;
            _pendingLoad = null;
            _loadArmed = false;
            _hasFile = false;
            _currentTag = null;
            ResetSeekLocked();
            Volatile.Write(ref _duration, 0);
            Volatile.Write(ref _timePos, 0);
            tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _releaseTcs = tcs;
            Command(UdOther, "stop");
        }
        if (!wasActive && _idle) CompleteRelease(); // nothing was loaded: no END_FILE will come
        return tcs.Task;
    }

    private void CompleteRelease()
    {
        TaskCompletionSource<bool>? tcs;
        lock (_gate)
        {
            tcs = _releaseTcs;
            _releaseTcs = null;
        }
        tcs?.TrySetResult(true);
    }

    public void Stop()
    {
        lock (_gate)
        {
            _pendingLoad = null;
            _loadArmed = false;
            _hasFile = false;
            ResetSeekLocked();
            Command(UdOther, "stop");
        }
    }

    public void SeekRelative(double deltaSeconds, bool exact)
    {
        SeekResult? timedOut;
        lock (_gate)
        {
            if (!CanSeekLocked()) return;
            timedOut = CheckSeekTimeoutLocked();
            double basePos = _seekInFlight ? (_queuedTarget ?? _inFlightTarget) : TimePos;
            RequestSeekLocked(basePos + deltaSeconds, exact);
        }
        if (timedOut != null) SeekCompleted?.Invoke(timedOut);
    }

    public void SeekAbsolute(double seconds, bool exact)
    {
        SeekResult? timedOut;
        lock (_gate)
        {
            if (!CanSeekLocked()) return;
            timedOut = CheckSeekTimeoutLocked();
            RequestSeekLocked(seconds, exact);
        }
        if (timedOut != null) SeekCompleted?.Invoke(timedOut);
    }

    public void TogglePause()
    {
        if (!HasFile) return;
        if (_eof && _paused)
        {
            // At the end (keep-open): Play restarts from the beginning.
            Command(UdOther, "seek", "0", "absolute+keyframes");
            Command(UdOther, "set", "pause", "no");
        }
        else
        {
            Command(UdOther, "cycle", "pause");
        }
    }

    public void SetVolume(double volume)
        => Command(UdOther, "set", "volume", Math.Clamp(volume, 0, 100).ToString("0.#", CultureInfo.InvariantCulture));

    public void ToggleMute() => Command(UdOther, "cycle", "mute");

    public void SetHwdec(string mode) => Command(UdOther, "set", "hwdec", mode);

    // ---------------------------------------------------------------- seek internals

    private bool CanSeekLocked()
        => _handle != IntPtr.Zero && _hasFile && _pendingLoad == null && Duration > 0;

    private SeekResult? CheckSeekTimeoutLocked()
    {
        if (!_seekInFlight || _seekWatch.Elapsed < SeekTimeout) return null;
        // No PLAYBACK_RESTART arrived: unblock the queue and report it.
        _seekInFlight = false;
        _queuedTarget = null;
        _queuedCount = 0;
        return new SeekResult(_seekWatch.Elapsed.TotalMilliseconds, _inFlightTarget, _inFlightDelta,
            _inFlightExact, _inFlightCoalesced, TimedOut: true, Landed: null, _currentTag);
    }

    private void RequestSeekLocked(double target, bool exact)
    {
        target = Math.Clamp(target, 0, Duration);
        if (_seekInFlight)
        {
            _queuedTarget = target;
            _queuedExact = exact;
            _queuedCount++;
        }
        else
        {
            IssueSeekLocked(target, exact, coalesced: 0);
        }
    }

    private void IssueSeekLocked(double target, bool exact, int coalesced)
    {
        _seekInFlight = true;
        _inFlightTarget = target;
        _inFlightDelta = target - TimePos;
        _inFlightExact = exact;
        _inFlightCoalesced = coalesced;
        _seekWatch.Restart();
        Command(UdSeek, "seek",
            target.ToString("0.###", CultureInfo.InvariantCulture),
            exact ? "absolute+exact" : "absolute+keyframes");
    }

    private void IssueQueuedSeekLocked()
    {
        if (_queuedTarget is double q)
        {
            int coalesced = Math.Max(0, _queuedCount - 1);
            bool exact = _queuedExact;
            _queuedTarget = null;
            _queuedCount = 0;
            IssueSeekLocked(q, exact, coalesced);
        }
    }

    private void ResetSeekLocked()
    {
        _seekInFlight = false;
        _queuedTarget = null;
        _queuedCount = 0;
        _seekWatch.Reset();
    }

    // ---------------------------------------------------------------- event thread

    private void EventLoop()
    {
        while (!_stopping)
        {
            IntPtr p = MpvNative.mpv_wait_event(_handle, -1);
            if (p == IntPtr.Zero) continue;
            var ev = Marshal.PtrToStructure<MpvEvent>(p);
            if (ev.EventId == MpvEventId.None) continue;
            if (ev.EventId == MpvEventId.Shutdown) break;
            try
            {
                HandleEvent(ev);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("viBro mpv event error: " + ex);
            }
        }
    }

    private void HandleEvent(MpvEvent ev)
    {
        switch (ev.EventId)
        {
            case MpvEventId.PropertyChange:
                OnPropertyChange(ev.ReplyUserdata, Marshal.PtrToStructure<MpvEventProperty>(ev.Data));
                break;

            case MpvEventId.StartFile:
                long started = Marshal.PtrToStructure<MpvEventStartFile>(ev.Data).PlaylistEntryId;
                lock (_gate)
                {
                    _lastStartedEntryId = started;
                    _loadArmed = _pendingLoad != null &&
                                 (started == _expectedEntryId || _expectedEntryId == AnyEntry);
                }
                break;

            case MpvEventId.EndFile:
                OnEndFile(Marshal.PtrToStructure<MpvEventEndFile>(ev.Data));
                break;

            case MpvEventId.CommandReply:
                OnCommandReply(ev.ReplyUserdata, ev.Error, ev.Data);
                break;

            case MpvEventId.PlaybackRestart:
                OnPlaybackRestart();
                break;

            case MpvEventId.LogMessage:
                var msg = Marshal.PtrToStructure<MpvEventLogMessage>(ev.Data);
                EngineMessage?.Invoke(
                    Marshal.PtrToStringUTF8(msg.Level) ?? "",
                    Marshal.PtrToStringUTF8(msg.Prefix) ?? "",
                    (Marshal.PtrToStringUTF8(msg.Text) ?? "").TrimEnd());
                break;
        }
    }

    private void OnPropertyChange(ulong id, MpvEventProperty prop)
    {
        bool has = prop.Data != IntPtr.Zero && prop.Format != MpvFormat.None;
        switch (id)
        {
            case ObsTimePos:
                Volatile.Write(ref _timePos, has ? ReadDouble(prop.Data) : 0);
                break;
            case ObsDuration:
                Volatile.Write(ref _duration, has ? ReadDouble(prop.Data) : 0);
                break;
            case ObsPause:
                _paused = !has || Marshal.ReadInt32(prop.Data) != 0;
                break;
            case ObsEof:
                _eof = has && Marshal.ReadInt32(prop.Data) != 0;
                break;
            case ObsVolume:
                if (has) Volatile.Write(ref _volume, ReadDouble(prop.Data));
                break;
            case ObsMute:
                _muted = has && Marshal.ReadInt32(prop.Data) != 0;
                break;
            case ObsIdle:
                _idle = has && Marshal.ReadInt32(prop.Data) != 0;
                if (_idle) CompleteRelease();
                break;
        }
    }

    private void OnEndFile(MpvEventEndFile ef)
    {
        CompleteRelease();
        PendingLoad? failed = null;
        bool playbackFailed = false;
        lock (_gate)
        {
            if (_pendingLoad == null && _hasFile && ef.Reason == (int)MpvEndFileReason.Error)
            {
                _hasFile = false;
                playbackFailed = true;
            }
            bool isExpected = _pendingLoad != null &&
                              (ef.PlaylistEntryId == _expectedEntryId || (_expectedEntryId == AnyEntry && _loadArmed));
            if (isExpected && ef.Reason == (int)MpvEndFileReason.Error)
            {
                failed = _pendingLoad;
                _pendingLoad = null;
                _hasFile = false;
            }
            if (isExpected) _loadArmed = false;
        }
        if (failed != null)
            LoadFailed?.Invoke(failed.Path, MpvNative.ErrorString(ef.Error));
        if (playbackFailed)
            PlaybackFailed?.Invoke(MpvNative.ErrorString(ef.Error));
    }

    private void OnCommandReply(ulong ud, int error, IntPtr data)
    {
        PendingLoad? failed = null;
        lock (_gate)
        {
            if (ud >= UdLoadBase)
            {
                // Replies to superseded loadfile requests are ignored.
                if (_pendingLoad == null || (long)(ud - UdLoadBase) != _loadSeq) return;
                if (error < 0)
                {
                    failed = _pendingLoad;
                    _pendingLoad = null;
                }
                else
                {
                    _expectedEntryId = ReadPlaylistEntryId(data) ?? AnyEntry;
                    if (_expectedEntryId >= 0 && _lastStartedEntryId == _expectedEntryId)
                        _loadArmed = true; // START_FILE arrived before the reply
                }
            }
            else if (ud == UdSeek && error < 0 && _seekInFlight)
            {
                _seekInFlight = false;
                IssueQueuedSeekLocked();
            }
        }
        if (failed != null)
            LoadFailed?.Invoke(failed.Path, MpvNative.ErrorString(error));
    }

    /// <summary>Extracts "playlist_entry_id" from the node map returned by the loadfile command.</summary>
    private static long? ReadPlaylistEntryId(IntPtr commandResult)
    {
        if (commandResult == IntPtr.Zero) return null;
        var node = Marshal.PtrToStructure<MpvNode>(commandResult); // mpv_event_command starts with its result node
        if (node.Format != MpvFormat.NodeMap || node.U == IntPtr.Zero) return null;

        var list = Marshal.PtrToStructure<MpvNodeList>(node.U);
        int nodeSize = Marshal.SizeOf<MpvNode>();
        for (int i = 0; i < list.Num; i++)
        {
            string? key = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(list.Keys, i * IntPtr.Size));
            if (key != "playlist_entry_id") continue;
            var value = Marshal.PtrToStructure<MpvNode>(list.Values + i * nodeSize);
            return value.Format == MpvFormat.Int64 ? value.U.ToInt64() : null;
        }
        return null;
    }

    private void OnPlaybackRestart()
    {
        PendingLoad? loaded = null;
        double loadMs = 0;
        SeekResult? seek = null;

        // Read the position reached BEFORE a queued seek is issued below; otherwise the
        // value could already belong to the next seek (measurement artifact seen in tests).
        double? landed = GetDouble("time-pos");

        lock (_gate)
        {
            if (_pendingLoad != null)
            {
                // A restart that arrives before the newest START_FILE belongs to an older file.
                if (_loadArmed)
                {
                    loaded = _pendingLoad;
                    loadMs = loaded.Watch.Elapsed.TotalMilliseconds;
                    _pendingLoad = null;
                    _loadArmed = false;
                    _hasFile = true;
                }
            }
            else if (_seekInFlight)
            {
                seek = new SeekResult(_seekWatch.Elapsed.TotalMilliseconds, _inFlightTarget, _inFlightDelta,
                    _inFlightExact, _inFlightCoalesced, TimedOut: false, landed, _currentTag);
                _seekInFlight = false;
                IssueQueuedSeekLocked();
            }
        }

        if (loaded != null)
        {
            double duration = GetDouble("duration") ?? 0;
            Volatile.Write(ref _duration, duration);
            var info = new MediaInfo(
                GetString("current-tracks/video/codec"),
                (long)(GetDouble("width") ?? 0),
                (long)(GetDouble("height") ?? 0),
                GetString("hwdec-current"),
                GetString("current-vo"));
            LoadCompleted?.Invoke(new LoadResult(loaded.Path, loaded.Kind, loadMs, duration, info, loaded.Tag));
        }

        if (seek != null)
            SeekCompleted?.Invoke(seek);
    }

    // ---------------------------------------------------------------- helpers

    private void Observe(ulong id, string name, MpvFormat format)
        => Check(MpvNative.mpv_observe_property(_handle, id, name, format), "observe " + name);

    private int Command(ulong userdata, params string[] args)
    {
        IntPtr h = _handle;
        if (h == IntPtr.Zero) return -1;
        var ptrs = new IntPtr[args.Length + 1];
        try
        {
            for (int i = 0; i < args.Length; i++)
                ptrs[i] = Marshal.StringToCoTaskMemUTF8(args[i]);
            return MpvNative.mpv_command_async(h, userdata, ptrs);
        }
        finally
        {
            foreach (var p in ptrs)
                if (p != IntPtr.Zero) Marshal.FreeCoTaskMem(p);
        }
    }

    private string? GetString(string name)
    {
        IntPtr p = MpvNative.mpv_get_property_string(_handle, name);
        if (p == IntPtr.Zero) return null;
        try { return Marshal.PtrToStringUTF8(p); }
        finally { MpvNative.mpv_free(p); }
    }

    private double? GetDouble(string name)
        => MpvNative.mpv_get_property(_handle, name, MpvFormat.Double, out double v) >= 0 ? v : null;

    private static double ReadDouble(IntPtr p) => BitConverter.Int64BitsToDouble(Marshal.ReadInt64(p));

    private static void Check(int result, string what)
    {
        if (result < 0)
            throw new InvalidOperationException($"mpv {what}: {MpvNative.ErrorString(result)}");
    }

    public void Dispose()
    {
        IntPtr h;
        lock (_gate)
        {
            h = _handle;
            if (h == IntPtr.Zero) return;
        }
        _stopping = true;
        MpvNative.mpv_wakeup(h);
        _eventThread?.Join(TimeSpan.FromSeconds(5));
        lock (_gate) _handle = IntPtr.Zero;
        MpvNative.mpv_terminate_destroy(h);
    }
}
