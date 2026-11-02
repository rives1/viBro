# Vulkan loader (vulkan-1.dll)

Official Khronos Vulkan desktop loader, embedded in viBro.exe and used **only** when Windows has no
`vulkan-1.dll` in System32 (PCs or VMs without GPU drivers). See `src/viBro/Util/NativePreload.cs`
and decision D-027.

| | |
|---|---|
| Upstream | https://github.com/KhronosGroup/Vulkan-Loader |
| Version | 1.4.357.0 |
| License | Apache-2.0 (`LICENSE.txt`, from the package) |
| Binary from | conda-forge package `libvulkan-loader-1.4.357.0-h477610d_2.conda` (win-64), built by https://github.com/conda-forge/vulkan-loader-feedstock (commit e61a610f0e72e4e757be6e27b015b0dc421aaf08) |
| Package URL | https://conda.anaconda.org/conda-forge/win-64/libvulkan-loader-1.4.357.0-h477610d_2.conda |
| Package SHA-256 | `4b094443126c5fc38d200eb0ea335b67e64d966d7aa737bb3141217a787b0c73` (matches the conda-forge index) |
| vulkan-1.dll SHA-256 | `8878cdc6e7656fd8c81135eaf546ce97a4dab8e75b2023bc28fb97a33c2d96f8` |
| Imports | ADVAPI32, CFGMGR32, KERNEL32 only (no C runtime to install) |

`build.ps1` checks the SHA-256 of this file before publishing.
