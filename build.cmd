@echo off
rem Builds and packages viBro without changing the PowerShell execution policy.
rem Usage: build.cmd [-InstallDotnet] [-Compress]
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build\build.ps1" %*
if errorlevel 1 exit /b 1
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build\package.ps1"
