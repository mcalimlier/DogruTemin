using DogruTemin.Core.Models;

namespace DogruTemin.Core.Services;

public static class DraftValidationService
{
    public static IReadOnlyList<ValidationIssue> Validate(ProcurementData data)
    {
        var issues = new List<ValidationIssue>();

        if (data.Products.Count > 8)
            Critical(issues, "Products", "En fazla 8 ürün / mal / hizmet eklenebilir.");

        if (data.Firms.Count > 5)
            Critical(issues, "Firms", "En fazla 5 firma eklenebilir.");

        foreach (var product in data.Products.Take(8))
        {
            if (!string.IsNullOrWhiteSpace(product.Unit) && product.Unit is not ("Adet" or "Miktar"))
                Critical(issues, $"Product:{product.Id}:Unit", $"{product.No}. ürünün birimi Adet veya Miktar olmalıdır.");

            if (!string.IsNullOrWhiteSpace(product.Quantity) && !NumberParser.TryPositiveDecimal(product.Quantity, out _))
                Critical(issues, $"Product:{product.Id}:Quantity", $"{product.No}. ürün miktarı 0'dan büyük geçerli bir sayı olmalıdır.");
        }

        foreach (var firm in data.Firms.Take(5))
        {
            if (!string.IsNullOrWhiteSpace(firm.TaxNo) && !InputValidationService.ValidTaxNo(firm.TaxNo))
                Critical(issues, $"Firm:{firm.Id}:TaxNo", $"{firm.No}. firmanın vergi numarası 10 veya 11 rakam olmalıdır.");

            if (!string.IsNullOrWhiteSpace(firm.Phone) && !InputValidationService.ValidPhone(firm.Phone))
                Critical(issues, $"Firm:{firm.Id}:Phone", $"{firm.No}. firmanın telefonu geçersiz.");

            if (!string.IsNullOrWhiteSpace(firm.Email) && !InputValidationService.ValidEmail(firm.Email))
                Critical(issues, $"Firm:{firm.Id}:Email", $"{firm.No}. firmanın e-posta adresi geçersiz.");

            foreach (var offer in firm.Offers)
            {
                if (offer.ApproximateUnitPrice is <= 0)
                    Critical(issues, $"Offer:{firm.Id}:{offer.ProductId}:Approx", $"{firm.No}. firma için girilen yaklaşık fiyat geçersiz.");

                if (offer.FinalUnitPrice is <= 0)
                    Critical(issues, $"Offer:{firm.Id}:{offer.ProductId}:Final", $"{firm.No}. firma için girilen nihai fiyat geçersiz.");
            }
        }

        var activeFirms = data.Firms
            .Where(f => !string.IsNullOrWhiteSpace(f.Name))
            .Take(5)
            .ToList();

        for (var i = 0; i < activeFirms.Count; i++)
        for (var j = i + 1; j < activeFirms.Count; j++)
        {
            var sameName = TextNormalizer.NormalizeFirmName(activeFirms[i].Name) ==
                           TextNormalizer.NormalizeFirmName(activeFirms[j].Name);

            var aTax = TextNormalizer.DigitsOnly(activeFirms[i].TaxNo);
            var bTax = TextNormalizer.DigitsOnly(activeFirms[j].TaxNo);
            var sameTax = aTax.Length > 0 && aTax == bTax;

            if (sameName || sameTax)
                Critical(issues, "Firms", $"{activeFirms[i].No}. ve {activeFirms[j].No}. firma mükerrer görünüyor.");
        }

        if (!string.IsNullOrWhiteSpace(data.Institution.Phone) &&
            !InputValidationService.ValidPhone(data.Institution.Phone))
            Critical(issues, "Institution.Phone", "Kurum telefonu 10-15 rakam içermelidir.");

        if (!string.IsNullOrWhiteSpace(data.Institution.Email) &&
            !InputValidationService.ValidEmail(data.Institution.Email))
            Critical(issues, "Institution.Email", "Geçerli bir kurum e-posta adresi giriniz.");

        return issues;
    }

    private static void Critical(List<ValidationIssue> issues, string field, string message) =>
        issues.Add(new ValidationIssue
        {
            Severity = ValidationSeverity.Critical,
            Field = field,
            Message = message
        });
}
