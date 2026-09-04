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
    raise SystemExit('Hedef PowerShell bloğu bulunamadı')
s = s.replace(old, new, 1)
p.write_text(s, encoding='utf-8', newline='\n')
print('PowerShell kaçış bloğu düzeltildi.')
