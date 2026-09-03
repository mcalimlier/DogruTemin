@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0SETUP_HAZIRLA.ps1"
set "EXITCODE=%ERRORLEVEL%"
if "%EXITCODE%"=="2" (
  echo.
  echo RC/test surumunde final Setup bilincli olarak uretilmedi.
)
if not "%EXITCODE%"=="0" pause
exit /b %EXITCODE%
