@echo off
setlocal

set "ROOT=%~dp0"
set "SCRIPT=%ROOT%src\syscalculator\Build\DailyRelease.ps1"

if not exist "%SCRIPT%" (
    echo Daily release script not found:
    echo %SCRIPT%
    exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" %*
exit /b %ERRORLEVEL%
