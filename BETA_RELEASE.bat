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

set "PWSH=pwsh"
where pwsh >nul 2>nul
if errorlevel 1 if exist "%ProgramFiles%\PowerShell\7\pwsh.exe" set "PWSH=%ProgramFiles%\PowerShell\7\pwsh.exe"
if "%PWSH%"=="pwsh" (
  where pwsh >nul 2>nul
  if errorlevel 1 (
    echo PowerShell 7 is required. Install it with: winget install --id Microsoft.PowerShell --source winget
    exit /b 1
  )
)

"%PWSH%" -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" -Action "%ACTION%" -Channel beta
exit /b %ERRORLEVEL%
