@echo off
powershell.exe -NoLogo -NoProfile -File "%~dp0tools\start-release.ps1" %*
set "release_result=%errorlevel%"
echo.
pause
exit /b %release_result%
