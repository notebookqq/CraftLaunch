@echo off
title CraftLaunch Starter
echo ============================================
echo   CraftLaunch - first run helper
echo ============================================
echo.
echo This will remove Windows "downloaded file" blocking
echo and then start CraftLaunch.
echo.
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-ChildItem -Path '%~dp0' -File | Unblock-File; Write-Host 'Unblocked OK.'"
echo Starting CraftLaunch...
start "" "%~dp0CraftLaunch.exe"
exit
