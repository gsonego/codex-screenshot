@echo off
powershell.exe -NoLogo -NoProfile -STA -ExecutionPolicy Bypass -File "%~dp0plugins\desktop-print\capture.ps1" -CopyToClipboard -DelaySeconds 3
if errorlevel 1 pause
