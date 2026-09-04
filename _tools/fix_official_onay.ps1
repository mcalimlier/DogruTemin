$ErrorActionPreference = 'Stop'
$path = 'DogruTemin.Wpf/Services/OfficialDocumentService.cs'
$text = [System.IO.File]::ReadAllText($path).Replace("`r`n", "`n")

$oldImplemented = @'
    public static bool IsImplemented(OutputDocumentType type) =>
        type is OutputDocumentType.ApproximateRequest
            or OutputDocumentType.ApproximateCostSheet
            or OutputDocumentType.FirmOfferForm
            or OutputDocumentType.MarketResearchReport
            or OutputDocumentType.InspectionAcceptanceReport;
'@
$newImplemented = @'
    public static bool IsImplemented(OutputDocumentType type) =>
        type is OutputDocumentType.ApproximateRequest
            or OutputDocumentType.ApproximateCostSheet
            or OutputDocumentType.ApprovalDocument
            or OutputDocumentType.FirmOfferForm
            or OutputDocumentType.MarketResearchReport
            or OutputDocumentType.InspectionAcceptanceReport;
'@
if (-not $text.Contains($oldImplemented)) { throw 'IsImplemented hedefi bulunamadı.' }
$text = $text.Replace($oldImplemented, $newImplemented)

$oldQuantity = 'private static string ApprovalQuantitySummary(IReadOnlyCollection<ProductItem> products)'
$newQuantity = 'private static string ApprovalQuantitySummary(List<ProductItem> products)'
if (-not $text.Contains($oldQuantity)) { throw 'ApprovalQuantitySummary hedefi bulunamadı.' }
$text = $text.Replace($oldQuantity, $newQuantity)

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
git commit -m 'Onay Belgesini resmî belge motorunda etkinleştir'
git push
