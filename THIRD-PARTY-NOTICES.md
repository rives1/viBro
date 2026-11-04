# Third-party components / Componenti di terze parti

viBro.exe embeds the following components. / viBro.exe incorpora i componenti seguenti.

| Component | Version | License | Source |
|---|---|---|---|
| libmpv (mpv) | build `20261101-git-48fca90e8c`, mpv commit `48fca90e8ce5011d0a4c689178ad739329ea253d` | GPL-2.0-or-later (GPL build; mpv is LGPL-2.1+ only when built with `-Dgpl=false`) | https://github.com/mpv-player/mpv |
| FFmpeg and the libraries statically linked into libmpv-2.dll (among them x264, x265, dav1d, aom, SVT-AV1, libvpx, libass, HarfBuzz, FreeType, FriBidi, libplacebo, OpenSSL, libssh, libbluray, …) | pinned by the build above | FFmpeg configured with `--enable-gpl --enable-version3` (no `--enable-nonfree`): **GPL-3.0-or-later** for the combination; each library keeps its own GPL-compatible license | https://github.com/shinchiro/mpv-winbuild-cmake (configuration: `packages/ffmpeg.cmake`) |
| .NET runtime and WPF | 10.0 | MIT | https://github.com/dotnet |
| Vulkan loader (vulkan-1.dll) | 1.4.357.0 | Apache-2.0 (`third_party/vulkan-loader/LICENSE.txt`) | https://github.com/KhronosGroup/Vulkan-Loader — binary from conda-forge, provenance in `third_party/vulkan-loader/SOURCE.md` |

Because libmpv-2.dll is GPL-3.0 (FFmpeg `--enable-version3`), **viBro.exe as distributed is covered by the GPL version 3**; viBro's own code is GPL-3.0-or-later and compatible with it. Apache-2.0 (Vulkan loader, OpenSSL 3) and MIT (.NET) are compatible with GPLv3.

Optional, supplied by the user (not included, not distributed): `ffmpeg.exe` next to `viBro.exe` enables the extract (F9, 1.5.0); viBro only starts it as a separate program. / Facoltativo, fornito dall'utente: `ffmpeg.exe` accanto a `viBro.exe` abilita l'estratto (F9).

Build-only tool (not included in the EXE): 7-Zip `7zr.exe` 26.05 (LGPL-2.1), https://github.com/ip7z/7zip.

## Distribution policy / Politica di distribuzione

**viBro is distributed as source code only; no EXE is published** (decision D-052, 2026-11-03). / **viBro si distribuisce solo come sorgente; non si pubblica l'EXE.**

Why: when viBro.exe is conveyed to others, GPLv3 §6 requires the Corresponding Source of everything it contains, libmpv-2.dll included, for as long as it is offered. The pinned libmpv comes from shinchiro's daily build, whose workflow runs `ninja update` (latest revision of every library at build time) and does not record those revisions; shinchiro also keeps only ~30 builds. The exact source of that DLL therefore cannot be provided, and an approximate one (libraries "as of that date") would not be the Corresponding Source. / Perché: trasmettendo l'EXE servirebbe il sorgente corrispondente esatto della DLL, che non è ricostruibile.

What this means / Cosa comporta:
- [x] The GitHub workflow builds and packages viBro as a check but uploads no artifact and creates no release (`.github/workflows/build.yml`).
- [x] `build.cmd` downloads libmpv directly from its publisher (shinchiro) and verifies its SHA-256: whoever builds viBro gets libmpv from the original distributor.
- [x] License texts and notices are in the repository and in the locally built zip (`package.ps1`).
- [ ] One-off cleanup on GitHub: delete the EXE artifacts uploaded by earlier workflow runs and the draft releases created for tags (see `docs/DECISIONS.md`, D-052).
- Personal use and copies inside one organization are not distribution according to the FSF; giving a locally built EXE to someone outside it would bring back the obligations above.
- To publish an EXE again in the future: build libmpv in-house from pinned scripts, record every library revision and publish the matching source archive with each release.
- Not a GPL matter, but worth checking before any public or commercial binary release: patents on video formats (H.264, HEVC).
