# viBro

viBro is a desktop application for Windows 11 x64 to browse folders of videos, view them instantly and organize them (delete, rename, move, copy). Responsiveness comes first: the first frame must appear at once, switching from one video to the next must be immediate and seeking must be fast, on a NAS too.

Technology: C#/WPF (.NET 10) for the interface, [libmpv](https://mpv.io) for decoding and playback. The program ships as a single portable `viBro.exe`.

> **Status: 1.5.0 in test** (extract of a part of a video with I / O / F9); 1.4.0 released. Version 1.0.0 was the first complete and verified version. Version 0.1.0 validated the technology on SSD and NAS. The road to 1.0.0 is described in [SPEC.md](SPEC.md) (in Italian).
>
> Italian version of this file: [README.it.md](README.it.md).

## Features

- A single self-contained EXE: no installation, no administrator rights, no codecs to install.
- Path bar: local folders, mapped drives and UNC paths (`\\server\share`). Folders open in the background, so an unreachable share never blocks the interface.
- Lists the subfolders and the videos of the folder (MP4, MKV, MOV, WMV), non-recursive: `..` (parent folder) first, then the folders, then the videos, each group in the chosen order (names in natural order, the same as File Explorer). A double click or Enter opens a folder; D/E, the wheel and file operations skip folders.
- **Play/Pause is kept when the file changes**: if the video was paused (or at startup) the new one is loaded paused on its first frame; if it was playing, the new one starts playing.
- Seeking: by default ±1 s and ±5 s exact, ±1 min and ±5 min on keyframes (fast, approximate) or exact with *Exact long seeks*; every step can be changed in `[Seek]` of `viBro.ini`. Repeated requests are coalesced, so no queue builds up.
- Play/Pause, timeline, volume, mute, fullscreen.
- **Automatic measurements** in `viBro-metrics.csv`, next to the EXE: time to first frame, video switch, seek latency, memory, startup time.
- Folder tree (user folders, local and network drives, open UNC shares), loaded progressively and kept in sync with the open folder.
- Sorting by Name, Size, Date modified; three resizable panes; settings in `viBro.ini`.
- Delete, rename, move and copy files, with confirmations, progress and cancellation.
- **Extract a part of a video** (1.5.0): I and O mark start and end, F9 writes the part to a new file in the same folder. It uses an `ffmpeg.exe` that you put next to `viBro.exe` (not included; without it the function is disabled). No re-encoding (fast, original quality): the extract starts at the keyframe at or before the start marker, which can be several seconds earlier; the end is close to the end marker. The markers appear on the timeline (green I, red O, yellow band between them) and a message confirms the extract when it is done.
- Light/Dark themes (*Theme* button), control bar (timeline, volume) below the video and above the file list, a timeline band below the video in fullscreen.
- Folder icon in front of the path bar; icon buttons for Play/Pause, Mute (shows the state), Browse and Theme.
- Time under the pointer on the timeline; the loaded file is marked in the list (▶ playing, ❚❚ paused, bold name); the status bar shows the position, e.g. `12 / 128 videos · 412.5 GB`.
- In fullscreen, the time appears over the video for about 1 s after a seek and the file name for 0.8 s after a change (mpv on-screen display).
- Compact status bar: number of videos in the folder and their total size, status, measurements.
- Error log `viBro-errors.log` next to the EXE.

## viBro.ini

It sits next to `viBro.exe` and is written when the program closes. Edit it only while viBro is closed. An invalid value is replaced by its default and reported in the status bar. Besides the window, panes, columns, last folder, last move/copy destinations and audio, which the program saves by itself, it contains:

| Section / key | Default | Meaning |
|---|---|---|
| `[Playback] HardwareDecoding` | `auto-safe` | Hardware decoding (`no` = software only, or any mpv `hwdec` value) |
| `[Playback] ExactLongSeeks` | `false` | Makes every seek and the timeline exact, whatever the step |
| `[Playback] ExactSeekThresholdSeconds` | `30` | Seeks shorter than this are always exact; longer ones go to the nearest keyframe (fast, off by a few seconds) |
| `[Seek] LeftRightSeconds` | `1` | Step of ← / → |
| `[Seek] UpDownSeconds` | `5` | Step of ↓ / ↑ |
| `[Seek] WheelSeconds` | `3` | Step of the wheel over the video |
| `[Seek] PageSeconds` | `60` | Step of PgDown / PgUp |
| `[Seek] AltPageSeconds` | `300` | Step of Alt + PgDown / PgUp |
| `[Cache] MaxMB` | `0` | 0 = mpv cache disabled; >0 = cache enabled with this limit |
| `[Diagnostics] MetricsLog` | `true` | Measurement log `viBro-metrics.csv`; `false` = the file is not created or written |
| `[Appearance] Theme` | `System` | `Light`, `Dark`, or `System` (follows Windows until a theme is chosen with the *Theme* button) |

## Keyboard shortcuts

Seek steps are the defaults; they can be changed in `[Seek]` of `viBro.ini` (1.4.0), and Ctrl+F2 shows the values in use.

| Key | Action | Since |
|---|---|---|
| ← / → | Back / forward about 1 s | ✔ |
| ↓ / ↑ | Back / forward about 5 s | ✔ |
| PgDown / PgUp | Back / forward about 1 min | ✔ |
| Alt + PgDown / Alt + PgUp | Back / forward about 5 min | ✔ |
| Space | Play/Pause (at the end of the video it restarts from the beginning) | ✔ |
| E / D | Previous / next video (Play/Pause kept), without wrapping around | ✔ |
| M | Mute on/off | ✔ |
| F11 / Esc | Enter / leave fullscreen | ✔ |
| Ctrl + L | Focus the path bar (Enter opens, Esc cancels) | ✔ |
| Enter | Open the selected folder of the list (`..` = parent folder) | 1.3.0 |
| Shift + Enter | Open the selected video with the Windows default application (viBro pauses) | 1.3.0 |
| Del or R | Delete the file after confirmation (Recycle Bin if local, permanent if on the network) | 0.3.0 |
| F2 | Rename the file (name only; the extension is kept; the cursor starts at the beginning of the name) | 0.3.0 |
| W | Choose a destination folder and move the file | 0.3.0 |
| Shift + W | Move to the last destination used | 0.3.0 |
| C | Choose a destination folder and copy the file (same rules as moving; the original stays) | 0.6.0 |
| Shift + C | Copy to the last copy destination (separate from the move destination) | 0.6.0 |
| I / O | Set the start / end marker of an extract at the current position (status bar; also over the video in fullscreen) | 1.5.0 |
| F9 | Save the part between I and O as a new file in the same folder, `name-extract-yyMMdd-HHmm.ext` (e.g. `clip.mp4` → `clip-extract-261104-1445.mp4`; `-2`, `-3` … if it exists). Needs `ffmpeg.exe` next to `viBro.exe` | 1.5.0 |
| Ctrl + F2 | Show the commands window (keyboard and mouse) | 1.2.0 |
| Ctrl + W | Close viBro | 0.6.0 |

## Mouse on the video

| Action | Effect | Since |
|---|---|---|
| Click | Play/Pause (the click that brings viBro to the foreground does not change playback) | 0.5.0 |
| Double click | Enter / leave fullscreen; playback stays as it was | 0.5.0 |
| Right click | Menu: *Delete* (Del/R), *Move...* (W), *Move to <last folder>* (Shift+W), *Copy...* (C), *Copy to <last folder>* (Shift+C) | 0.5.0 (copy 0.6.0) |
| Right button held + drag left/right | Seek within the video: the whole video width = the whole duration; the picture follows the mouse, exact seek on release (no menu) | 0.6.0 |
| Wheel over the timeline | Down: next video; up: previous video (like D / E), in the normal window and in fullscreen | 1.3.0 |
| Wheel over the video (in fullscreen: anywhere but the timeline) | Up: forward 3 s; down: back 3 s (`[Seek] WheelSeconds`) | 1.3.0 |
| Click on the timeline and drag | Seek within the video; the thumb follows the mouse | 0.4.0 |

Shortcuts are suspended while typing in the path bar (except Ctrl+W). As specified, the arrow keys always seek, even when the tree or the list has the focus: the tree is navigated with the mouse.

## Usage

```
viBro.exe [folder] [--exact-seeks] [--trace-input] [--mpv-opt name=value]...
```

- `folder`: folder to open at startup.
- `--trace-input`: diagnostics; writes every key and wheel message received by viBro, with its target window, to `viBro-errors.log`.
- `--exact-seeks`: makes long seeks and the timeline exact for this session (not saved; the permanent setting is `ExactLongSeeks` in `viBro.ini`).
- `--mpv-opt name=value`: overrides an mpv option. Meant for performance tests, e.g. `--mpv-opt cache=yes --mpv-opt demuxer-max-bytes=64MiB`, or `--mpv-opt log-file=C:\temp\mpv.log` for diagnostics.

On the first start .NET extracts the native libraries, libmpv among them, to the per-user cache `%TEMP%\.net\viBro\<hash>`. Later starts reuse the cache; a new build uses a new hash.

## Getting viBro

No ready-made EXE is published: build it yourself with `build.cmd` (a few minutes, no administrator rights). The reason is the GPL: the EXE embeds a third-party libmpv build whose exact source code cannot be provided, because that build does not record the revisions of the libraries it contains. Building locally downloads libmpv directly from its publisher. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).

## Building

Requirements: Windows 10/11 x64 and an Internet connection for the first download. No administrator rights are needed.

```
build.cmd
```

The script:
1. uses the .NET 10 SDK if present; otherwise, with `-InstallDotnet`, installs it for the current user in `.dotnet\`;
2. downloads the libmpv build pinned in `build/dependencies.json`, checks its SHA-256 and extracts `libmpv-2.dll` to `deps\libmpv\`;
3. publishes `artifacts\publish\viBro.exe` and checks that it is the only file produced;
4. creates `dist\viBro-<version>-win-x64.zip` with its `.sha256`.

Individual commands: `build\build.ps1 [-Compress] [-DepsOnly]` and `build\package.ps1`. The default variant is the normal EXE; `-Compress` produces a smaller EXE (−58%) that starts more slowly (+165 ms) and uses about 70 MB more memory (measurements in `docs/DECISIONS.md`, D-047). On GitHub, the workflow `.github/workflows/build.yml` runs the same scripts as a build check; it publishes nothing.

The version lives **only** in `Directory.Build.props` and is shown in the window title and in the EXE properties.

## Measurements

```
powershell -ExecutionPolicy Bypass -File build\analyze-metrics.ps1 -Path <exe-folder>\viBro-metrics.csv
```

The test protocol is described in [docs/TEST-PLAN-0.1.0.md](docs/TEST-PLAN-0.1.0.md) (in Italian).

## License

GPL-3.0-or-later (see [LICENSE](LICENSE)). The EXE embeds a GPL build of libmpv whose FFmpeg is built with `--enable-gpl --enable-version3`, so the distributed EXE as a whole is under GPL version 3. See [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
