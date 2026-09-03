@echo off
setlocal EnableExtensions EnableDelayedExpansion
cd /d "%~dp0"

set "ROOT=%~dp0"
set "APP=%~dp0_YAYIN\Uygulama"
set "SETUPOUT=%~dp0_YAYIN\Setup"
set "LOGDIR=%~dp0_YAYIN\Loglar"
set "PUBLISHLOG=%LOGDIR%\YAYIN.log"
set "SETUPLOG=%LOGDIR%\SETUP.log"
set "DISTDIR=%~dp0DAGITIM"
set "NSI=%~dp0Kurulum\DogruTemin_NSIS.nsi"
set "MAKENSIS="

if exist "%~dp0_YAYIN" rmdir /s /q "%~dp0_YAYIN"
if exist "%DISTDIR%" rmdir /s /q "%DISTDIR%"
mkdir "%LOGDIR%" >nul 2>&1
>"%PUBLISHLOG%" echo DogruTemin publish started: %DATE% %TIME%
>"%SETUPLOG%" echo DogruTemin setup build started: %DATE% %TIME%

cls
echo ============================================================
echo DOGRUTEMIN 4.1.0 - ARAYUZ R21 SETUP
echo ============================================================
echo.
echo Self-contained uygulama ve Windows kurulum paketi hazirlanacak.
echo Son kullanici bilgisayarinda .NET veya SDK gerekmez.
echo.

rem 1) .NET SDK kontrolu
where dotnet >nul 2>&1
if errorlevel 1 (
  echo HATA: dotnet komutu bulunamadi.
  echo Gelistirme bilgisayarinda .NET SDK kurulu olmali.
  pause
  exit /b 1
)
for /f "tokens=1" %%V in ('dotnet --version 2^>nul') do set "SDK_VERSION=%%V"
if not defined SDK_VERSION (
  echo HATA: .NET SDK bulunamadi. Yalniz Runtime kurulu olabilir.
  pause
  exit /b 1
)
echo [OK] .NET SDK: !SDK_VERSION!
>>"%PUBLISHLOG%" dotnet --info

rem 2) NSIS kontrolu - PATH gerekmez
rem Once standart yollar, sonra registry/PowerShell aramasi denenir.
if exist "%ProgramFiles(x86)%\NSIS\makensis.exe" set "MAKENSIS=%ProgramFiles(x86)%\NSIS\makensis.exe"
if not defined MAKENSIS if exist "%ProgramFiles%\NSIS\makensis.exe" set "MAKENSIS=%ProgramFiles%\NSIS\makensis.exe"
if not defined MAKENSIS if exist "%LOCALAPPDATA%\NSIS\makensis.exe" set "MAKENSIS=%LOCALAPPDATA%\NSIS\makensis.exe"
if not defined MAKENSIS if exist "%LOCALAPPDATA%\Programs\NSIS\makensis.exe" set "MAKENSIS=%LOCALAPPDATA%\Programs\NSIS\makensis.exe"

rem Registry ve yaygin klasorlerde otomatik ara.
if not defined MAKENSIS for /f "usebackq delims=" %%I in (`powershell -NoProfile -ExecutionPolicy Bypass -Command "$c=@(); $roots=@($env:ProgramFiles,${env:ProgramFiles(x86)},$env:LOCALAPPDATA); foreach($rk in @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*','HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*','HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*')){ try { Get-ItemProperty $rk -ErrorAction SilentlyContinue ^| Where-Object { $_.DisplayName -match 'NSIS^|Nullsoft Scriptable Install System' } ^| ForEach-Object { if($_.InstallLocation){ $c += (Join-Path $_.InstallLocation 'makensis.exe') } } } catch {} }; foreach($x in $c){ if(Test-Path $x){ Write-Output $x; exit 0 } }; foreach($r in $roots){ if($r -and (Test-Path $r)){ $f=Get-ChildItem -Path $r -Filter makensis.exe -File -Recurse -ErrorAction SilentlyContinue ^| Select-Object -First 1 -ExpandProperty FullName; if($f){ Write-Output $f; exit 0 } } }"`) do if not defined MAKENSIS set "MAKENSIS=%%I"

rem Son olarak PATH'i dene.
if not defined MAKENSIS for /f "delims=" %%I in ('where makensis.exe 2^>nul') do if not defined MAKENSIS set "MAKENSIS=%%I"

if not defined MAKENSIS (
  echo HATA: NSIS kurulu gorunuyor olabilir ancak makensis.exe bulunamadi.
  echo.
  echo Lutfen su komutu calistirip sonucu kontrol edin:
  echo winget list --id NSIS.NSIS
  echo.
  echo Gerekirse bilgisayari yeniden baslatip bu BAT dosyasini tekrar calistirin.
  pause
  exit /b 1
)
if not exist "!MAKENSIS!" (
  echo HATA: NSIS yolu bulundu ancak makensis.exe mevcut degil:
  echo !MAKENSIS!
  pause
  exit /b 1
)
echo [OK] NSIS: !MAKENSIS!
>>"%SETUPLOG%" echo MAKENSIS=!MAKENSIS!
"!MAKENSIS!" /VERSION >>"%SETUPLOG%" 2>&1

rem 3) Kaynak kontrolleri
if not exist "%~dp0DogruTemin.Wpf\DogruTemin.Wpf.csproj" (
  echo HATA: DogruTemin.Wpf.csproj bulunamadi.
  pause
  exit /b 1
)
if not exist "%~dp0DogruTemin.Core\DogruTemin.Core.csproj" (
  echo HATA: DogruTemin.Core.csproj bulunamadi.
  pause
  exit /b 1
)
if not exist "%~dp0DogruTemin.Wpf\Assets\AppIcon.ico" (
  echo HATA: AppIcon.ico bulunamadi.
  pause
  exit /b 1
)
if not exist "%NSI%" (
  echo HATA: Kurulum\DogruTemin_NSIS.nsi bulunamadi.
  pause
  exit /b 1
)

rem NSIS kaynagi UTF-8 BOM olmali; Turkce karakterler korunur.
powershell -NoProfile -ExecutionPolicy Bypass -Command "$b=[IO.File]::ReadAllBytes('%NSI%'); if($b.Length -lt 3 -or $b[0] -ne 239 -or $b[1] -ne 187 -or $b[2] -ne 191){exit 9}" >>"%SETUPLOG%" 2>&1
if errorlevel 1 (
  echo HATA: NSIS betigi UTF-8 BOM degil. Setup uretilmedi.
  pause
  exit /b 1
)
>>"%SETUPLOG%" echo NSI_ENCODING=UTF8_BOM

echo.
echo [1/3] Self-contained uygulama hazirlaniyor...
mkdir "%APP%" >nul 2>&1
mkdir "%SETUPOUT%" >nul 2>&1

dotnet restore "DogruTemin.Wpf\DogruTemin.Wpf.csproj" -r win-x64 >>"%PUBLISHLOG%" 2>&1
if errorlevel 1 (
  echo HATA: Restore basarisiz. Ayrinti: _YAYIN\Loglar\YAYIN.log
  pause
  exit /b 1
)

dotnet publish "DogruTemin.Wpf\DogruTemin.Wpf.csproj" -c Release -r win-x64 --self-contained true --no-restore -o "%APP%" /p:PublishSingleFile=false /p:PublishReadyToRun=false /p:DebugType=None /p:DebugSymbols=false >>"%PUBLISHLOG%" 2>&1
if errorlevel 1 (
  echo HATA: Uygulama yayini basarisiz. Ayrinti: _YAYIN\Loglar\YAYIN.log
  pause
  exit /b 1
)

if not exist "%APP%\DogruTemin.exe" (
  echo HATA: DogruTemin.exe olusmadi.
  pause
  exit /b 1
)
if not exist "%APP%\coreclr.dll" (
  echo HATA: Self-contained runtime eksik: coreclr.dll
  pause
  exit /b 1
)
if not exist "%APP%\hostfxr.dll" (
  echo HATA: Self-contained runtime eksik: hostfxr.dll
  pause
  exit /b 1
)
if not exist "%APP%\PresentationFramework.dll" (
  echo HATA: WPF runtime eksik: PresentationFramework.dll
  pause
  exit /b 1
)
for /f %%C in ('dir /a-d /s /b "%APP%" 2^>nul ^| find /c /v ""') do set "APP_FILE_COUNT=%%C"
if not defined APP_FILE_COUNT set "APP_FILE_COUNT=0"
>>"%PUBLISHLOG%" echo APP_FILE_COUNT=!APP_FILE_COUNT!
if !APP_FILE_COUNT! LSS 100 (
  echo HATA: Self-contained yayin eksik: !APP_FILE_COUNT! dosya.
  pause
  exit /b 1
)
echo [OK] Uygulama yayini: !APP_FILE_COUNT! dosya.

echo [2/3] Windows setup olusturuluyor...
"!MAKENSIS!" /V4 "%NSI%" >>"%SETUPLOG%" 2>&1
if errorlevel 1 (
  echo HATA: NSIS setup olusturamadi. Ayrinti: _YAYIN\Loglar\SETUP.log
  pause
  exit /b 1
)

set "SETUPEXE=%SETUPOUT%\DogruTemin_Setup_4.1.0_x64.exe"
if not exist "!SETUPEXE!" (
  echo HATA: Setup EXE bulunamadi. Ayrinti: _YAYIN\Loglar\SETUP.log
  pause
  exit /b 1
)
for %%F in ("!SETUPEXE!") do set "SETUP_BYTES=%%~zF"
>>"%SETUPLOG%" echo SETUP_BYTES=!SETUP_BYTES!
if !SETUP_BYTES! LSS 10000000 (
  echo HATA: Setup beklenenden cok kucuk: !SETUP_BYTES! bayt.
  pause
  exit /b 1
)

powershell -NoProfile -ExecutionPolicy Bypass -Command "$p='!SETUPEXE!'; $b=[IO.File]::ReadAllBytes($p); if($b.Length -lt 2 -or $b[0]-ne 77 -or $b[1]-ne 90){exit 8}" >>"%SETUPLOG%" 2>&1
if errorlevel 1 (
  echo HATA: Setup EXE PE/MZ kontrolunu gecemedi.
  pause
  exit /b 1
)

echo [OK] Setup olusturuldu: !SETUP_BYTES! bayt.
echo [3/3] Son kontroller tamamlandi.

mkdir "%DISTDIR%" >nul 2>&1
set "FINALEXE=%DISTDIR%\DogruTemin_Setup_4.1.0_x64.exe"
move /y "!SETUPEXE!" "!FINALEXE!" >nul
if errorlevel 1 (
  echo HATA: Setup DAGITIM klasorune tasinamadi.
  pause
  exit /b 1
)
if exist "%~dp0KURULUM_NOTU.txt" copy /y "%~dp0KURULUM_NOTU.txt" "%DISTDIR%\KURULUM_NOTU.txt" >nul

rem Setup dosyasinin SHA-256 ozeti; dosyanin aktarimda degismedigini dogrulamak icindir.
set "HASHFILE=%DISTDIR%\DogruTemin_Setup_4.1.0_x64_SHA256.txt"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$p='!FINALEXE!'; $h=(Get-FileHash -Algorithm SHA256 -LiteralPath $p).Hash; @('DogruTemin Setup SHA-256','',('Dosya : ' + [IO.Path]::GetFileName($p)),('SHA256: ' + $h)) | Set-Content -LiteralPath '!HASHFILE!' -Encoding UTF8" >nul 2>&1
if errorlevel 1 (
  echo UYARI: SHA-256 dosyasi olusturulamadi. Setup dosyasi yine kullanilabilir.
) else (
  echo [OK] SHA-256 dogrulama dosyasi olusturuldu.
)

if exist "%~dp0_YAYIN" rmdir /s /q "%~dp0_YAYIN"

echo.
echo ============================================================
echo BASARILI - DAGITIM DOSYALARI:
echo ============================================================
echo !FINALEXE!
echo %DISTDIR%\KURULUM_NOTU.txt  ^(istege bagli kullanici bilgilendirmesi^)
echo %DISTDIR%\DogruTemin_Setup_4.1.0_x64_SHA256.txt  ^(dosya dogrulama^)
echo.
echo Bu EXE Windows 10/11 x64 bilgisayarlara kopyalanabilir.
echo Son kullanici bilgisayarinda .NET, SDK, Visual Studio veya Excel gerekmez.
echo.
pause
exit /b 0
