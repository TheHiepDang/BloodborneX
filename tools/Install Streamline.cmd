@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\Install Streamline.ps1" -PackageRoot "%~dp0"
echo.
pause
exit /b %errorlevel%
