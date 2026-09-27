@echo off
chcp 65001 >nul
setlocal
set "REPO_ROOT=%~dp0.."
set "OUTPUT_ROOT=%~dp0DAGITIM"

echo DogruTemin 4.3.0 imzasiz Setup olusturuluyor...
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%REPO_ROOT%\SETUP_HAZIRLA.ps1" -AllowUnsignedFinal -OutputRoot "%OUTPUT_ROOT%"
set "EXITCODE=%ERRORLEVEL%"

if not "%EXITCODE%"=="0" (
  echo.
  echo EXE olusturma basarisiz oldu. Hata kodu: %EXITCODE%
  pause
  exit /b %EXITCODE%
)

echo.
echo EXE ve SHA-256 dosyasi hazir: "%OUTPUT_ROOT%"
exit /b 0
