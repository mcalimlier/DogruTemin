param(
    [switch]$AllowPrerelease,
    [switch]$AllowUnsignedFinal,
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
    Write-Host "Kod imzalanıyor: $(Split-Path $FilePath -Leaf)" -ForegroundColor Cyan
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
$isPrerelease = -not [string]::IsNullOrWhiteSpace($suffix)

if ($isPrerelease -and -not $AllowPrerelease) {
    Write-Host "DoğruTemin $packageVersion bir RC/test sürümüdür." -ForegroundColor Yellow
    Write-Host 'Final Setup güvenlik amacıyla üretilmedi. RC testleri bitince VersionSuffix boşaltılmalıdır.'
    exit 2
}
if ($isPrerelease) {
    Write-Host "UYARI: $packageVersion yalnız RC/CI kurulum testi için üretilecek." -ForegroundColor Yellow
}
if (-not $isPrerelease -and [string]::IsNullOrWhiteSpace($CertificateThumbprint) -and -not $AllowUnsignedFinal) {
    Fail 'Final Setup için kod imzalama sertifikası zorunludur. 4.3.0 imzasız istisnası için exe_olustur\EXE_OLUSTUR.bat kullanın.'
}
if (-not $isPrerelease -and [string]::IsNullOrWhiteSpace($CertificateThumbprint) -and $AllowUnsignedFinal) {
    if ($version -ne '4.3.0') { Fail 'Sertifikasız final dağıtım yalnızca onaylanmış 4.3.0 sürümü için açıktır.' }
    Write-Host 'UYARI: Onaylanmış 4.3.0 dağıtımı kod imzası olmadan üretilecek. Windows SmartScreen bilinmeyen yayıncı uyarısı gösterebilir.' -ForegroundColor Yellow
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) { Fail '.NET SDK bulunamadı.' }
if ([int]((& dotnet --version).Split('.')[0]) -lt 8) { Fail '.NET 8 veya daha yeni SDK gerekir.' }

$makensis = @(
    "${env:ProgramFiles(x86)}\NSIS\makensis.exe",
    "$env:ProgramFiles\NSIS\makensis.exe",
    "$env:LOCALAPPDATA\Programs\NSIS\makensis.exe"
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
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
    Write-Host 'Kod imzalama sertifikası tanımlı değil; onaylanmış 4.3.0 Setup imzasız üretilecek.' -ForegroundColor Yellow
} else {
    Sign-File (Join-Path $appOut 'DogruTemin.exe')
}

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
$releaseNotesFile = "RELEASE_NOTES_${packageVersion}.md"
foreach ($file in 'KURULUM_NOTU.txt','KULLANICI_KILAVUZU.md',$releaseNotesFile) {
    if (Test-Path $file) { Copy-Item $file (Join-Path $dist $file) -Force }
}
$smartScreenNote = Join-Path $PSScriptRoot 'Kurulum\SMARTSCREEN_BILGILENDIRME.txt'
if (Test-Path $smartScreenNote) {
    Copy-Item $smartScreenNote (Join-Path $dist 'SMARTSCREEN_BILGILENDIRME.txt') -Force
}
$sha256 = [System.Security.Cryptography.SHA256]::Create()
try {
    $hash = ([BitConverter]::ToString($sha256.ComputeHash([IO.File]::ReadAllBytes($finalExe)))).Replace('-', '')
} finally {
    $sha256.Dispose()
}
$hashFile = Join-Path $dist "DogruTemin_Setup_${packageVersion}_x64_SHA256.txt"
@('DogruTemin Setup SHA-256','',"Dosya : $(Split-Path $finalExe -Leaf)","SHA256: $hash") | Set-Content $hashFile -Encoding UTF8
$notePath = Join-Path $dist 'KURULUM_NOTU.txt'
if (Test-Path $notePath) {
    @('', 'BU DAGITIM PAKETINE OZEL SHA-256', '--------------------------------', "Dosya : $(Split-Path $finalExe -Leaf)", "SHA256: $hash") | Add-Content $notePath -Encoding UTF8
}
Remove-Item $publish -Recurse -Force
Write-Host "BAŞARILI: $finalExe" -ForegroundColor Green
Write-Host "SHA-256: $hash"
