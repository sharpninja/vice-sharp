@echo off
pwsh.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0claude.ps1" %*
exit /b %ERRORLEVEL%
