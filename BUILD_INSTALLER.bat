@echo off
setlocal

set "CHANNEL=daily"
if not "%~1"=="" set "CHANNEL=%~1"

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0src\syscalculator\Build\BuildInstaller.ps1" -Channel "%CHANNEL%"
exit /b %ERRORLEVEL%
