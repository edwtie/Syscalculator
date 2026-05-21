@echo off
setlocal

set "CHANNEL=daily"
if not "%~1"=="" set "CHANNEL=%~1"

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

"%PWSH%" -NoProfile -ExecutionPolicy Bypass -File "%~dp0src\syscalculator\Build\BuildInstaller.ps1" -Channel "%CHANNEL%"
exit /b %ERRORLEVEL%
