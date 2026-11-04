using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ViBro.Util;

namespace ViBro;

// Extract (1.5.0): I = start marker, O = end marker, F9 = save the part between them as a new file in
// the same folder ("name-extract-yyMMdd-HHmm.ext"), with the ffmpeg.exe found next to viBro.exe.
// Stream copy: the extract starts at the keyframe at or before the start marker.
public partial class MainWindow
{
    private const int MarkOsdMs = 1200;

    private VideoItem? _markItem; // the file the markers belong to
    private double? _markIn, _markOut;

    /// <summary>Markers of the loaded file; markers set on another file no longer count.</summary>
    private bool MarkersValid => _markItem != null && ReferenceEquals(_markItem, _currentItem);

    private void SetMarker(bool start)
    {
        if (!FfmpegExtractor.IsAvailable)
        {
            ReportExtractUnavailable();
            return;
        }
        if (_player == null || _currentItem == null || _player.Duration <= 0)
        {
            SetStatus("No video loaded: nothing to mark.");
            return;
        }
        if (!MarkersValid)
        {
            _markItem = _currentItem;
            _markIn = _markOut = null;
        }
        double position = Math.Clamp(_player.TimePos, 0, _player.Duration);
        if (start) _markIn = position;
        else _markOut = position;

        string text = MarkersText();
        SetStatus(text + (_markIn != null && _markOut != null ? "  ·  F9 extracts" : ""));
        if (_isFullscreen) _player.ShowText(text, MarkOsdMs);
    }

    private string MarkersText() =>
        $"In {(_markIn is double i ? FormatMark(i) : "--:--:--")}  ·  Out {(_markOut is double o ? FormatMark(o) : "--:--:--")}";

    private static string FormatMark(double seconds) =>
        FormatTime(seconds) + "." + ((int)(Math.Max(seconds, 0) * 10 % 10)).ToString(CultureInfo.InvariantCulture);

    private void ReportExtractUnavailable() =>
        SetStatus($"Extract not available: put {FfmpegExtractor.ExeName} in the viBro folder ({AppInfo.ExeDirectory}).");

    private async void ExtractMarked()
    {
        if (_fileOpBusy) return;
        if (!FfmpegExtractor.IsAvailable)
        {
            ReportExtractUnavailable();
            return;
        }
        if (!MarkersValid || _markIn is not double start || _markOut is not double end || _currentItem is not VideoItem item)
        {
            SetStatus("Set the start (I) and end (O) markers first.");
            return;
        }
        if (end - start < 0.1)
        {
            SetStatus($"The end marker must be after the start marker ({MarkersText()}).");
            return;
        }

        string folder = Path.GetDirectoryName(item.FullPath) ?? "";
        _fileOpBusy = true;
        using var cts = new CancellationTokenSource();
        ProgressDialog? progressDialog = null;
        var progress = new Progress<double>(p => progressDialog?.Report(p));
        try
        {
            string name = await Task.Run(() => FfmpegExtractor.OutputName(folder, item.Name, DateTime.Now));
            string target = Path.Combine(folder, name);
            SetStatus($"Extracting {FormatMark(start)} – {FormatMark(end)} of {item.Name} ...");
            var extract = FfmpegExtractor.ExtractAsync(item.FullPath, target, start, end, progress, cts.Token);
            if (await Task.WhenAny(extract, Task.Delay(400)) != extract)
            {
                progressDialog = new ProgressDialog(this, item.Name, folder, cts, verb: "Extracting");
                IsEnabled = false;
                progressDialog.Show();
            }
            string finalPath = await extract;
            AddExtractToList(finalPath, folder, item.Storage);
            SetStatus($"Extracted {FormatMark(start)} – {FormatMark(end)} to {Path.GetFileName(finalPath)} " +
                      "(starts at the keyframe at or before the start marker)");
        }
        catch (OperationCanceledException)
        {
            SetStatus("Extract cancelled: no file was written.");
        }
        catch (Exception ex)
        {
            ErrorLog.Write("extract", "Extract of " + item.Name + " failed", ex);
            progressDialog = CloseProgress(progressDialog);
            MessageBox.Show(this, "The extract was not created.\n\n" + ex.Message, "Extract", MessageBoxButton.OK, MessageBoxImage.Warning);
            SetStatus("Extract failed: " + ex.Message.Split('\n')[0].Trim());
        }
        finally
        {
            progressDialog = CloseProgress(progressDialog);
            _fileOpBusy = false;
        }
    }

    /// <summary>The new file appears in the list (if its folder is still shown); the selection does not change.</summary>
    private void AddExtractToList(string path, string folder, string storage)
    {
        if (_currentFolder == null || !FileOps.IsSameFolder(_currentFolder, folder)) return;
        try
        {
            var fi = new FileInfo(path);
            var list = new List<VideoItem>(_items) { new VideoItem(fi.Name, fi.FullName, fi.Length, fi.LastWriteTime, storage) };
            SetItems(list, keepSelection: true);
            UpdateFolderSummary();
        }
        catch
        {
            // the file exists on disk; the list shows it at the next refresh
        }
    }
}
