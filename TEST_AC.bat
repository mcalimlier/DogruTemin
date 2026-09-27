@echo off
setlocal
cd /d "%~dp0"
where dotnet >nul 2>nul
if errorlevel 1 (
  echo HATA: .NET 8 SDK bulunamadi.
  pause
  exit /b 1
)

echo [1/2] DogruTemin 4.3.0 otomatik testleri calisiyor...
dotnet test "DogruTemin.Tests\DogruTemin.Tests.csproj" -c Release
if errorlevel 1 (
  echo.
  echo HATA: Otomatik testlerden biri basarisiz. Program acilmadi.
  pause
  exit /b 1
)

echo.
echo [2/2] Testler basarili. DogruTemin 4.3.0 aciliyor...
echo Kisa kontrol: coklu urun/firma, Adet-Miktar, hesaplama, Onay Belgesi, PDF ve kaydet-ac.
echo.
dotnet run --project "DogruTemin.Wpf\DogruTemin.Wpf.csproj" -c Release
if errorlevel 1 (
  echo.
  echo HATA: Derleme veya calistirma basarisiz.
  pause
  exit /b 1
)
endlocal
