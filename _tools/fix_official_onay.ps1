$ErrorActionPreference = 'Stop'
$path = 'DogruTemin.Wpf/Services/OfficialDocumentService.cs'
$text = [System.IO.File]::ReadAllText($path).Replace("`r`n", "`n")

# Onay Belgesi sayfası BuildDocument içinde zaten vardır. Burada destek listesine de kesin olarak ekle.
$implementedPattern = '(?s)public static bool IsImplemented\(OutputDocumentType type\)\s*=>\s*type is OutputDocumentType\.ApproximateRequest.*?OutputDocumentType\.InspectionAcceptanceReport;'
$implementedMatch = [regex]::Match($text, $implementedPattern)
if (-not $implementedMatch.Success) { throw 'IsImplemented bloğu bulunamadı.' }
$implementedBlock = $implementedMatch.Value
if ($implementedBlock -notmatch 'OutputDocumentType\.ApprovalDocument') {
    $implementedBlock = $implementedBlock.Replace(
        'or OutputDocumentType.ApproximateCostSheet',
        "or OutputDocumentType.ApproximateCostSheet`n            or OutputDocumentType.ApprovalDocument")
    $text = $text.Substring(0, $implementedMatch.Index) + $implementedBlock + $text.Substring($implementedMatch.Index + $implementedMatch.Length)
}

# ActiveProducts zaten List<ProductItem> döndürdüğü için analyzer'ın istediği somut tipi kullan.
$oldQuantity = 'private static string ApprovalQuantitySummary(IReadOnlyCollection<ProductItem> products)'
$newQuantity = 'private static string ApprovalQuantitySummary(List<ProductItem> products)'
if ($text.Contains($oldQuantity)) {
    $text = $text.Replace($oldQuantity, $newQuantity)
} elseif (-not $text.Contains($newQuantity)) {
    throw 'ApprovalQuantitySummary imzası bulunamadı.'
}

[System.IO.File]::WriteAllText($path, $text, [System.Text.UTF8Encoding]::new($false))

& dotnet restore 'DogruTemin.Wpf/DogruTemin.Wpf.csproj' | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'WPF restore başarısız' }
& dotnet restore 'DogruTemin.Tests/DogruTemin.Tests.csproj' | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Test restore başarısız' }
& dotnet build 'DogruTemin.Wpf/DogruTemin.Wpf.csproj' -c Release --no-restore | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Release build başarısız' }
& dotnet test 'DogruTemin.Tests/DogruTemin.Tests.csproj' -c Release --no-restore | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Testler başarısız' }

git config user.name 'mcalimlier'
git config user.email '123293878+mcalimlier@users.noreply.github.com'
git add $path
if (-not (git diff --cached --quiet)) {
    git commit -m 'Onay Belgesini resmî belge motorunda etkinleştir'
    git push
}
