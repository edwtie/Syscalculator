@echo off
setlocal

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0legacy\Syscalculator174.VB6\Build\TestSyscalculator174Legacy.ps1" %*
exit /b %ERRORLEVEL%
