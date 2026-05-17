@echo off
setlocal

set "ROOT=%~dp0"
set "SCRIPT=%ROOT%src\syscalculator\Build\DailyRelease.ps1"

if not exist "%SCRIPT%" (
    echo Release script not found:
    echo %SCRIPT%
    exit /b 1
)

set "ACTION=check"
if not "%~1"=="" set "ACTION=%~1"

powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" -Action "%ACTION%" -Channel beta
exit /b %ERRORLEVEL%
