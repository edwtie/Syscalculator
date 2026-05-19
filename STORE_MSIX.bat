@echo off
setlocal

set "ROOT=%~dp0"
set "SCRIPT=%ROOT%src\syscalculator\Build\BuildMsix.ps1"

if not exist "%SCRIPT%" (
    echo MSIX build script not found:
    echo %SCRIPT%
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" -Channel production %*
exit /b %ERRORLEVEL%
