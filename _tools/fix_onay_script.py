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

p.write_text(s, encoding='utf-8', newline='\n')
print('PowerShell integration script fixed.')
