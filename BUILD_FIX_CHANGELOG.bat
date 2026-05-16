@echo off
setlocal

set "ROOT=%~dp0"
set "SCRIPT=%ROOT%src\syscalculator\Build\BuildFixChangelog.ps1"

if not exist "%SCRIPT%" (
    echo Build script not found:
    echo %SCRIPT%
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %*
exit /b %ERRORLEVEL%
