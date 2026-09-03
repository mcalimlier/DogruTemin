param(
    [switch]$AllowPrerelease,
    [string]$OutputRoot = '',
    [string]$CertificateThumbprint = $env:DOGRUTEMIN_SIGN_CERT_SHA1,
    [string]$TimestampUrl = 'http://timestamp.digicert.com'
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
function Fail([string]$Message) { Write-Host "HATA: $Message" -ForegroundColor Red; exit 1 }

function Find-SignTool {
    $command = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    $kitsRoot = "${env:ProgramFiles(x86)}\Windows Kits\10\bin"
    if (-not (Test-Path $kitsRoot)) { return $null }
    return Get-ChildItem $kitsRoot -Filter signtool.exe -File -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match '\\x64\\signtool\.exe$' } |
        Sort-Object FullName -Descending | Select-Object -ExpandProperty FullName -First 1
}

function Sign-File([string]$FilePath) {
    if ([string]::IsNullOrWhiteSpace($CertificateThumbprint)) { return }
    $signTool = Find-SignTool
    if (-not $signTool) { Fail 'Kod imzası istendi ancak signtool.exe bulunamadı.' }
    & $signTool sign /sha1 $CertificateThumbprint /fd SHA256 /tr $TimestampUrl /td SHA256 $FilePath
    if ($LASTEXITCODE -ne 0) { Fail "Kod imzası başarısız: $FilePath" }
    & $signTool verify /pa /v $FilePath
    if ($LASTEXITCODE -ne 0) { Fail "Kod imzası doğrulanamadı: $FilePath" }
}

[xml]$props = Get-Content (Join-Path $PSScriptRoot 'Directory.Build.props') -Raw -Encoding UTF8
$group = $props.Project.PropertyGroup | Select-Object -First 1
$version = [string]$group.VersionPrefix
$suffix = [string]$group.VersionSuffix
$assemblyVersion = [string]$group.AssemblyVersion
if ([string]::IsNullOrWhiteSpace($version)) { Fail 'VersionPrefix okunamadı.' }
$packageVersion = if ([string]::IsNullOrWhiteSpace($suffix)) { $version } else { "$version-$suffix" }

if (-not [string]::IsNullOrWhiteSpace($suffix) -and -not $AllowPrerelease) {
    Write-Host "DoğruTemin $packageVersion bir RC/test sürümüdür." -ForegroundColor Yellow
    Write-Host 'Final Setup güvenlik amacıyla üretilmedi. RC testleri bitince VersionSuffix boşaltılmalıdır.'
    exit 2
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { Fail '.NET SDK bulunamadı.' }
$makensis = @("${env:ProgramFiles(x86)}\NSIS\makensis.exe", "$env:ProgramFiles\NSIS\makensis.exe", "$env:LOCALAPPDATA\Programs\NSIS\makensis.exe") |
    Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $makensis) { $cmd = Get-Command makensis.exe -ErrorAction SilentlyContinue; if ($cmd) { $makensis = $cmd.Source } }
if (-not $makensis) { Fail 'NSIS makensis.exe bulunamadı.' }

$publish = Join-Path $PSScriptRoot '_YAYIN'
$appOut = Join-Path $publish 'Uygulama'
$setupOut = Join-Path $publish 'Setup'
$dist = if ([string]::IsNullOrWhiteSpace($OutputRoot)) { Join-Path $PSScriptRoot 'DAGITIM' } else { [IO.Path]::GetFullPath($OutputRoot) }
if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
New-Item -ItemType Directory -Force -Path $appOut,$setupOut,$dist | Out-Null

Write-Host 'Kalite kapısı: restore + Release build + otomatik testler' -ForegroundColor Cyan
& dotnet restore (Join-Path $PSScriptRoot 'DogruTemin.Wpf\DogruTemin.Wpf.csproj')
if ($LASTEXITCODE -ne 0) { Fail 'WPF restore başarısız.' }
& dotnet restore (Join-Path $PSScriptRoot 'DogruTemin.Tests\DogruTemin.Tests.csproj')
if ($LASTEXITCODE -ne 0) { Fail 'Test restore başarısız.' }
& dotnet build (Join-Path $PSScriptRoot 'DogruTemin.Wpf\DogruTemin.Wpf.csproj') -c Release --no-restore
if ($LASTEXITCODE -ne 0) { Fail 'Release derlemesi başarısız.' }
& dotnet test (Join-Path $PSScriptRoot 'DogruTemin.Tests\DogruTemin.Tests.csproj') -c Release --no-restore
if ($LASTEXITCODE -ne 0) { Fail 'Otomatik testler başarısız. Setup üretilmedi.' }

$project = Join-Path $PSScriptRoot 'DogruTemin.Wpf\DogruTemin.Wpf.csproj'
& dotnet restore $project -r win-x64
if ($LASTEXITCODE -ne 0) { Fail 'win-x64 restore başarısız.' }
& dotnet publish $project -c Release -r win-x64 --self-contained true --no-restore -o $appOut /p:PublishSingleFile=false /p:PublishReadyToRun=false /p:DebugType=None /p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { Fail 'Publish başarısız.' }
foreach ($file in 'DogruTemin.exe','coreclr.dll','hostfxr.dll','PresentationFramework.dll') {
    if (-not (Test-Path (Join-Path $appOut $file))) { Fail "Self-contained yayın eksik: $file" }
}

if ([string]::IsNullOrWhiteSpace($CertificateThumbprint)) {
    Write-Host 'Kod imzalama sertifikası tanımlı değil; paket imzasız üretilecek.' -ForegroundColor Yellow
} else { Sign-File (Join-Path $appOut 'DogruTemin.exe') }

$template = Join-Path $PSScriptRoot 'Kurulum\DogruTemin_NSIS.nsi'
$nsi = Get-Content $template -Raw -Encoding UTF8
$nsi = $nsi.Replace('!define APPVERSION "4.2.0"', "!define APPVERSION `"$version`"")
$nsi = $nsi.Replace('OutFile "..\_YAYIN\Setup\DogruTemin_Setup_4.2.0_x64.exe"', "OutFile `"..\_YAYIN\Setup\DogruTemin_Setup_${packageVersion}_x64.exe`"")
$nsi = $nsi.Replace('VIProductVersion "4.2.0.0"', "VIProductVersion `"$assemblyVersion`"")
$nsi = $nsi.Replace('VIAddVersionKey /LANG=1055 "FileVersion" "4.2.0.0"', "VIAddVersionKey /LANG=1055 `"FileVersion`" `"$assemblyVersion`"")
$nsi = $nsi.Replace('VIAddVersionKey /LANG=1055 "ProductVersion" "4.2.0"', "VIAddVersionKey /LANG=1055 `"ProductVersion`" `"$packageVersion`"")
$tempNsi = Join-Path $publish 'DogruTemin_NSIS.generated.nsi'
[IO.File]::WriteAllText($tempNsi, $nsi, (New-Object System.Text.UTF8Encoding($true)))

& $makensis /V4 $tempNsi
if ($LASTEXITCODE -ne 0) { Fail 'NSIS Setup oluşturamadı.' }
$setupExe = Join-Path $setupOut "DogruTemin_Setup_${packageVersion}_x64.exe"
if (-not (Test-Path $setupExe)) { Fail 'Setup EXE oluşmadı.' }
if ((Get-Item $setupExe).Length -lt 10000000) { Fail 'Setup beklenenden küçük.' }
if (-not [string]::IsNullOrWhiteSpace($CertificateThumbprint)) { Sign-File $setupExe }

$finalExe = Join-Path $dist "DogruTemin_Setup_${packageVersion}_x64.exe"
Move-Item $setupExe $finalExe -Force
foreach ($file in 'KURULUM_NOTU.txt','KULLANICI_KILAVUZU.md','RC1_DEGISIKLIK_NOTU.md') {
    if (Test-Path $file) { Copy-Item $file (Join-Path $dist $file) -Force }
}
$hash = (Get-FileHash $finalExe -Algorithm SHA256).Hash
$hashFile = Join-Path $dist "DogruTemin_Setup_${packageVersion}_x64_SHA256.txt"
@('DogruTemin Setup SHA-256','',"Dosya : $(Split-Path $finalExe -Leaf)","SHA256: $hash") | Set-Content $hashFile -Encoding UTF8
$notePath = Join-Path $dist 'KURULUM_NOTU.txt'
if (Test-Path $notePath) {
    @('', 'BU DAGITIM PAKETINE OZEL SHA-256', '--------------------------------', "Dosya : $(Split-Path $finalExe -Leaf)", "SHA256: $hash") | Add-Content $notePath -Encoding UTF8
}
Remove-Item $publish -Recurse -Force
Write-Host "BAŞARILI: $finalExe" -ForegroundColor Green
Write-Host "SHA-256: $hash"
