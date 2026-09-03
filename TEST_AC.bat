@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo HATA: .NET 8 SDK bulunamadi.
  pause
  exit /b 1
)
echo DogruTemin 4.2.0 RC1 Release derleniyor ve aciliyor...
dotnet run --project "DogruTemin.Wpf\DogruTemin.Wpf.csproj" -c Release
if errorlevel 1 (
  echo.
  echo HATA: Derleme veya calistirma basarisiz.
  pause
  exit /b 1
)
endlocal
