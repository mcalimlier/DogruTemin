from pathlib import Path

p = Path('_tools/apply_onay_belgesi.ps1')
s = p.read_text(encoding='utf-8')

old = '''$validation = $validation.Replace('RequireDate(data.Dates.CostCalculationDate, issues, \"Yaklaşık Maliyet Hesap Cetveli tarihi\");`r`n        RequireDate(data.Dates.MarketReportDate', 'RequireDate(data.Dates.CostCalculationDate, issues, \"Yaklaşık Maliyet Hesap Cetveli tarihi\");`r`n        RequireDate(data.Dates.ApprovalDocumentDate, issues, \"Onay Belgesi tarihi\");`r`n        RequireDate(data.Dates.MarketReportDate')
$validation = $validation.Replace(\"RequireDate(data.Dates.CostCalculationDate, issues, \\\"Yaklaşık Maliyet Hesap Cetveli tarihi\\\");`n        RequireDate(data.Dates.MarketReportDate\", \"RequireDate(data.Dates.CostCalculationDate, issues, \\\"Yaklaşık Maliyet Hesap Cetveli tarihi\\\");`n        RequireDate(data.Dates.ApprovalDocumentDate, issues, \\\"Onay Belgesi tarihi\\\");`n        RequireDate(data.Dates.MarketReportDate\")'''
new = '''$oldWorkflowDates = @'
        RequireDate(data.Dates.CostCalculationDate, issues, "Yaklaşık Maliyet Hesap Cetveli tarihi");
        RequireDate(data.Dates.MarketReportDate, issues, "Piyasa Fiyat Araştırması Tutanağı tarihi");
'@
$newWorkflowDates = @'
        RequireDate(data.Dates.CostCalculationDate, issues, "Yaklaşık Maliyet Hesap Cetveli tarihi");
        RequireDate(data.Dates.ApprovalDocumentDate, issues, "Onay Belgesi tarihi");
        RequireDate(data.Dates.MarketReportDate, issues, "Piyasa Fiyat Araştırması Tutanağı tarihi");
'@
$validation = Replace-Once $validation $oldWorkflowDates $newWorkflowDates 'workflow Onay tarihi zorunluluğu' '''
if old not in s:
    raise SystemExit('Target PowerShell block not found')
s = s.replace(old, new, 1)

old_func = '''function Replace-Once([string]$Text, [string]$Old, [string]$New, [string]$Label) {
    $i = $Text.IndexOf($Old, [System.StringComparison]::Ordinal)
    if ($i -lt 0) { throw "Anchor bulunamadı: $Label" }
    if ($Text.IndexOf($Old, $i + $Old.Length, [System.StringComparison]::Ordinal) -ge 0) { throw "Anchor birden fazla: $Label" }
    return $Text.Substring(0, $i) + $New + $Text.Substring($i + $Old.Length)
}'''
new_func = '''function Replace-Once([string]$Text, [string]$Old, [string]$New, [string]$Label) {
    $Text = $Text.Replace("`r`n", "`n")
    $Old = $Old.Replace("`r`n", "`n")
    $New = $New.Replace("`r`n", "`n")
    $i = $Text.IndexOf($Old, [System.StringComparison]::Ordinal)
    if ($i -lt 0) { throw "Anchor bulunamadı: $Label" }
    if ($Text.IndexOf($Old, $i + $Old.Length, [System.StringComparison]::Ordinal) -ge 0) { throw "Anchor birden fazla: $Label" }
    return $Text.Substring(0, $i) + $New + $Text.Substring($i + $Old.Length)
}'''
if old_func not in s:
    raise SystemExit('Replace-Once function not found')
s = s.replace(old_func, new_func, 1)

old_clear_anchor = '''$clearAnchor = @'
            case "Dates":
'@'''
new_clear_anchor = '''$clearAnchor = @'
                LoadOfferFirm(_offerEditingFirm);
                RefreshSaveStatusVisual();
                break;
            case "Dates":
'@'''
if old_clear_anchor not in s:
    raise SystemExit('Clear anchor block not found')
s = s.replace(old_clear_anchor, new_clear_anchor, 1)

old_clear_new = '''$clearNew = @'
            case "ApprovalDocument":'''
new_clear_new = '''$clearNew = @'
                LoadOfferFirm(_offerEditingFirm);
                RefreshSaveStatusVisual();
                break;
            case "ApprovalDocument":'''
if old_clear_new not in s:
    raise SystemExit('Clear new block not found')
s = s.replace(old_clear_new, new_clear_new, 1)

old_build = '''& dotnet restore | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'dotnet restore başarısız' }
& dotnet build -c Release --no-restore | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Release build başarısız' }
& dotnet test -c Release --no-build | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Testler başarısız' }'''
new_build = '''& dotnet restore 'DogruTemin.Wpf/DogruTemin.Wpf.csproj' | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'WPF restore başarısız' }
& dotnet restore 'DogruTemin.Tests/DogruTemin.Tests.csproj' | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Test restore başarısız' }
& dotnet build 'DogruTemin.Wpf/DogruTemin.Wpf.csproj' -c Release --no-restore | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Release build başarısız' }
& dotnet test 'DogruTemin.Tests/DogruTemin.Tests.csproj' -c Release --no-restore | Out-Host
if ($LASTEXITCODE -ne 0) { throw 'Testler başarısız' }'''
if old_build not in s:
    raise SystemExit('Build block not found')
s = s.replace(old_build, new_build, 1)

p.write_text(s, encoding='utf-8', newline='\n')
print('PowerShell integration script fixed.')
