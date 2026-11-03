# Third-party components / Componenti di terze parti

viBro.exe embeds the following components. / viBro.exe incorpora i componenti seguenti.

| Component | Version | License | Source |
|---|---|---|---|
| libmpv (mpv) | build `20261101-git-48fca90e8c`, mpv commit `48fca90e8ce5011d0a4c689178ad739329ea253d` | GPL-2.0-or-later (GPL build; mpv is LGPL-2.1+ only when built with `-Dgpl=false`) | https://github.com/mpv-player/mpv |
| FFmpeg and the libraries statically linked into libmpv-2.dll (among them x264, x265, dav1d, aom, SVT-AV1, libvpx, libass, HarfBuzz, FreeType, FriBidi, libplacebo, OpenSSL, libssh, libbluray, …) | pinned by the build above | FFmpeg configured with `--enable-gpl --enable-version3` (no `--enable-nonfree`): **GPL-3.0-or-later** for the combination; each library keeps its own GPL-compatible license | https://github.com/shinchiro/mpv-winbuild-cmake (configuration: `packages/ffmpeg.cmake`) |
| .NET runtime and WPF | 10.0 | MIT | https://github.com/dotnet |
| Vulkan loader (vulkan-1.dll) | 1.4.357.0 | Apache-2.0 (`third_party/vulkan-loader/LICENSE.txt`) | https://github.com/KhronosGroup/Vulkan-Loader — binary from conda-forge, provenance in `third_party/vulkan-loader/SOURCE.md` |

Because libmpv-2.dll is GPL-3.0 (FFmpeg `--enable-version3`), **viBro.exe as distributed is covered by the GPL version 3**; viBro's own code is GPL-3.0-or-later and compatible with it. Apache-2.0 (Vulkan loader, OpenSSL 3) and MIT (.NET) are compatible with GPLv3.

Build-only tool (not included in the EXE): 7-Zip `7zr.exe` 26.05 (LGPL-2.1), https://github.com/ip7z/7zip.

## Obligations before giving the EXE to anyone else / Obblighi prima di dare l'EXE ad altri

These apply only when viBro.exe is **conveyed** to others (published, sent, shared outside the organization). Personal use and copies inside one organization are not distribution according to the FSF. / Valgono solo quando viBro.exe viene **trasmesso** ad altri (pubblicato, inviato, condiviso fuori dall'organizzazione). L'uso personale e le copie dentro un'organizzazione non sono distribuzione secondo la FSF.

- [x] License texts and notices in the package: `package.ps1` puts `LICENSE`, this file, `README.md` and `CHANGELOG.md` in the zip.
- [ ] **Corresponding Source** (GPLv3 §6) of everything in the EXE, available for as long as the EXE is offered: viBro's source at the release tag (GitHub) **and** the source of the libmpv build: mpv at the commit above, FFmpeg and every library at the revisions used, and the build scripts (mpv-winbuild-cmake at the commit used). Links to upstream projects are not enough on their own: shinchiro keeps only ~30 builds and the distributor must ensure the source stays available. Practical way: archive those sources (or build libmpv from a pinned mpv-winbuild-cmake and keep the downloaded source tarballs) and publish the archive next to the release.
- [ ] Confirm the exact list of libraries in build `20261101` (the list above comes from the current `ffmpeg.cmake` on master, read on 2026-11-03).
- [ ] GitHub workflow: every push to `main` uploads the zip with the EXE as an *artifact*, and every tag creates a *draft release*. If the repository is public, the artifacts are downloadable by others: either complete the Corresponding Source before pushing, or stop uploading the EXE.
- [ ] Not a GPL matter, but worth checking before a public or commercial release: patents on video formats (H.264, HEVC) in the countries of distribution.
