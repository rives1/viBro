# viBro

viBro is a desktop application for Windows 11 x64 to browse folders of videos, view them instantly and organize them (delete, rename, move, copy). Responsiveness comes first: the first frame must appear at once, switching from one video to the next must be immediate and seeking must be fast, on a NAS too.

Technology: C#/WPF (.NET 10) for the interface, [libmpv](https://mpv.io) for decoding and playback. The program ships as a single portable `viBro.exe`.

> **Status: 1.10.0 released** (keyboard shortcuts configurable in `viBro.ini`). Version 1.0.0 was the first complete and verified version. Version 0.1.0 validated the technology on SSD and NAS. The road to 1.0.0 is described in [SPEC.md](SPEC.md) (in Italian).
>
> Italian version of this file: [README.it.md](README.it.md).

## Features

- A single self-contained EXE: no installation, no administrator rights, no codecs to install.
- Path bar: local folders, mapped drives and UNC paths (`\\server\share`). Folders open in the background, so an unreachable share never blocks the interface.
- Lists the subfolders and the videos of the folder (MP4, MKV, MOV, WMV, AVI since 1.8.0, M4V since 1.9.0), non-recursive: `..` (parent folder) first, then the folders, then the videos, each group in the chosen order (names in natural order, the same as File Explorer). A double click or Enter opens a folder; D/E, the wheel and file operations skip folders.
- **Play/Pause is kept when the file changes**: if the video was paused (or at startup) the new one is loaded paused on its first frame; if it was playing, the new one starts playing.
- Seeking: by default ±1 s and ±5 s exact, ±1 min and ±5 min on keyframes (fast, approximate) or exact with *Exact long seeks*; every step can be changed in `[Seek]` of `viBro.ini`. Repeated requests are coalesced, so no queue builds up.
- Play/Pause, timeline, volume, mute, fullscreen.
- **Automatic measurements** in `viBro-metrics.csv`, next to the EXE: time to first frame, video switch, seek latency, memory, startup time.
- Folder tree (user folders, local and network drives, open UNC shares), loaded progressively and kept in sync with the open folder.
- Sorting by Name, Size, Date modified; three resizable panes; settings in `viBro.ini`.
- Delete, rename, move and copy files, with confirmations, progress and cancellation.
- **Extract a part of a video** (1.5.0): I and O mark start and end, F9 writes the part to a new file in the same folder. It uses an `ffmpeg.exe` that you put next to `viBro.exe` (not included; without it the function is disabled). No re-encoding (fast, original quality): the extract starts at the keyframe at or before the start marker, which can be several seconds earlier; the end is close to the end marker. The markers appear on the timeline (green I, red O, yellow band between them) and a message confirms the extract when it is done.
- **Gallery** (1.6.0; right-click menu 1.8.0): F10 opens a separate window with one row per video of the folder and 5 images taken at the same percentages of each video's duration (10, 30, 50, 70, 90 % by default). Clicking a row selects and loads that video in the main window; clicking an image loads it at the image's time. It uses the same `ffmpeg.exe` as the extract. The images are the keyframes nearest to those positions (fast, also on a NAS), made in the background starting from the rows on screen, kept in memory only (nothing written to disk) and reused while viBro stays open. The gallery keeps the folder it was opened on; F10 again makes a new one. Right click on a row or an image (1.8.0) selects and loads that video and opens the same menu as on the video (Delete, Move, Copy); rows of deleted or moved files leave the gallery.
- **Multiple selection** (1.7.0): Ctrl + click adds or removes a file, Shift + click selects a range, Ctrl+A selects all the videos. Delete, move and copy act on all the selected videos: one confirmation, one destination, one progress window; name conflicts are asked file by file, with "do the same for the next conflicts" and *Skip*. The video area loads the file just clicked. Rename (F2), Shift+Enter and the extract (I / O / F9) need a single selected video. The arrow keys keep seeking, so the selection is made with the mouse.
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
| `[Gallery] PositionsPercent` | `10,30,50,70,90` | Positions of the gallery images, in % of each video's duration (1 to 10 values) |
| `[Gallery] Jobs` | `2` | ffmpeg runs at a time for the gallery (1–8; more is faster on an SSD, can be slower on a NAS) |
| `[Diagnostics] MetricsLog` | `true` | Measurement log `viBro-metrics.csv`; `false` = the file is not created or written |
| `[Delete] ConfirmFromVideoMenu` | `false` | `false` = *Delete* in the video's right-click menu deletes one file without confirmation (also on the network, where it is permanent); several files are always confirmed. `true` = always confirm |
| `[Appearance] Theme` | `System` | `Light`, `Dark`, or `System` (follows Windows until a theme is chosen with the *Theme* button) |

## Keyboard shortcuts

Seek steps are the defaults; they can be changed in `[Seek]` of `viBro.ini` (1.4.0), and Ctrl+F2 shows the values in use.

The keys below are the defaults. Since 1.10.0 every keyboard shortcut of the main window can be changed in `[Keys]` of `viBro.ini` (edit it while viBro is closed): one line per command, `Command=Key`, several keys separated by commas, modifiers `Ctrl+`, `Alt+`, `Shift+` (they must match exactly: `Left` does not react to Shift+Left), `none` for no key. Key names are those of Windows/WPF (`Left`, `PageUp`, `Enter`, `Escape`, `Space`, `F5`, letters, digits `0`–`9`; also `Del`, `Esc`, `PgUp`, `PgDn`). An invalid value gives a warning in the status bar and the default; a key given to two commands stays with the one listed first and the other loses it, with a warning. Ctrl+F2 shows the keys in use. Mouse and wheel are not configurable.

| Command | Default | Note |
|---|---|---|
| `SeekBackShort / SeekForwardShort` | `Left / Right` | [Seek] LeftRightSeconds |
| `SeekBackMedium / SeekForwardMedium` | `Down / Up` | [Seek] UpDownSeconds |
| `SeekBackLong / SeekForwardLong` | `PageDown / PageUp` | [Seek] PageSeconds |
| `SeekBackVeryLong / SeekForwardVeryLong` | `Alt+PageDown / Alt+PageUp` | [Seek] AltPageSeconds |
| `PlayPause` | `Space` |  |
| `PreviousVideo / NextVideo` | `E / D` |  |
| `Mute` | `M` |  |
| `Fullscreen / ExitFullscreen` | `F11 / Escape` | ExitFullscreen only in fullscreen |
| `FocusPath` | `Ctrl+L` |  |
| `SelectAll` | `Ctrl+A` |  |
| `Refresh` | `F5` |  |
| `OpenFolder` | `Enter` | only with a folder selected |
| `OpenWithDefaultApp` | `Shift+Enter` |  |
| `Delete` | `Delete,R` |  |
| `Rename` | `F2` |  |
| `Move / MoveToLast` | `W / Shift+W` |  |
| `Copy / CopyToLast` | `C / Shift+C` |  |
| `MarkIn / MarkOut` | `I / O` |  |
| `Extract` | `F9` |  |
| `Gallery` | `F10` |  |
| `Commands` | `Ctrl+F2` |  |
| `Close` | `Ctrl+W` | also while editing a text |


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
| Ctrl + A | Select all the videos of the folder (the loaded video does not change) | 1.7.0 |
| F5 | Refresh the folder tree and the file list from the disk (new and removed files and folders, sizes and dates), keeping expanded folders, selection, loaded video and scroll position | 1.8.0 |
| Enter | Open the selected folder of the list (`..` = parent folder) | 1.3.0 |
| Shift + Enter | Open the selected video with the Windows default application (viBro pauses) | 1.3.0 |
| Del or R | Delete the selected file(s) after one confirmation (Recycle Bin if local, permanent if on the network) | 0.3.0 (several 1.7.0) |
| F2 | Rename the file (one selected video; name only; the extension is kept; the cursor starts at the beginning of the name) | 0.3.0 |
| W | Choose a destination folder and move the selected file(s) | 0.3.0 (several 1.7.0) |
| Shift + W | Move to the last destination used | 0.3.0 |
| C | Choose a destination folder and copy the selected file(s) (same rules as moving; the originals stay) | 0.6.0 (several 1.7.0) |
| Shift + C | Copy to the last copy destination (separate from the move destination) | 0.6.0 |
| I / O | Set the start / end marker of an extract at the current position (status bar; also over the video in fullscreen) | 1.5.0 |
| F9 | Save the part between I and O as a new file in the same folder, `name-extract-yyMMdd-HHmm.ext` (e.g. `clip.mp4` → `clip-extract-261104-1445.mp4`; `-2`, `-3` … if it exists). Needs `ffmpeg.exe` next to `viBro.exe` | 1.5.0 |
| F10 | Gallery of the folder's videos in a separate window (Esc closes it). Needs `ffmpeg.exe` next to `viBro.exe` | 1.6.0 |
| Ctrl + F2 | Show the commands window (keyboard and mouse) | 1.2.0 |
| Ctrl + W | Close viBro | 0.6.0 |

## Mouse on the video

| Action | Effect | Since |
|---|---|---|
| Click | Play/Pause (the click that brings viBro to the foreground does not change playback) | 0.5.0 |
| Double click | Enter / leave fullscreen; playback stays as it was | 0.5.0 |
| Right click | Menu: *Delete* (Del/R), *Move...* (W), *Move to <last folder>* (Shift+W), *Copy...* (C), *Copy to <last folder>* (Shift+C); with several files selected, e.g. *Delete 3 files*. Since 1.8.0 *Delete* on one file does not ask for confirmation (`[Delete] ConfirmFromVideoMenu`) | 0.5.0 (copy 0.6.0, several 1.7.0) |
| Right button held + drag left/right | Seek within the video: the whole video width = the whole duration; the picture follows the mouse, exact seek on release (no menu) | 0.6.0 |
| Wheel over the timeline | Up: next video; down: previous video (like D / E), in the normal window and in fullscreen | 1.3.0 (inverted 1.9.0) |
| Right button held + wheel on the video | Up: next video; down: previous video, as on the timeline; releasing the button opens no menu | 1.9.0 |
| Wheel over the video (in fullscreen: anywhere but the timeline) | Up: forward 3 s; down: back 3 s (`[Seek] WheelSeconds`) | 1.3.0 |

Since 1.9.0 the wheel on the video and on the timeline works also when viBro is not the window in front, if Windows sends the wheel to the window under the pointer (Settings → Bluetooth & devices → Mouse → *Scroll inactive windows when hovering over them*, on by default). It does nothing while a viBro dialog is open.
| Click on the timeline and drag | Seek within the video; the thumb follows the mouse | 0.4.0 |

Shortcuts are suspended while typing in the path bar (except Close, Ctrl+W by default). As specified, the arrow keys always seek, even when the tree or the list has the focus: the tree is navigated with the mouse.

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

The [Releases](https://github.com/rives1/viBro/releases) page (from 1.6.0) lists each version with its notes and the source archive to build; it is created automatically when a version tag is pushed ([docs/RELEASE.md](docs/RELEASE.md), in Italian).

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
