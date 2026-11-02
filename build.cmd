@echo off
rem Builds and packages viBro. Double-click friendly: installs the .NET 10 SDK
rem per-user into .dotnet\ only if it is missing (no admin rights needed).
rem Usage: build.cmd [-Compress]
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build\build.ps1" -InstallDotnet %*
if errorlevel 1 goto failed
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build\package.ps1"
if errorlevel 1 goto failed
echo.
echo Build OK: artifacts\publish\viBro.exe
pause
exit /b 0
:failed
echo.
echo BUILD FAILED - copy the messages above.
pause
exit /b 1
