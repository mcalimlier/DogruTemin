using System.Security.Cryptography;
using System.Text.Json;
using DogruTemin.Core.Models;

namespace DogruTemin.Core.Services;

public static class RecordIntegrity
{
    public static string ContentHash(ProcurementData data)
    {
        var copy = JsonSerializer.Deserialize<ProcurementData>(JsonSerializer.Serialize(data))!;
        copy.Revision = 0;
        copy.LastOutput = null;
        copy.LastSavedAt = null;
        copy.AutoBackup = false;
        foreach (var offer in copy.Firms.SelectMany(f => f.Offers))
        {
            if (offer.ApproximateUnitPrice.HasValue)
                offer.ApproximateUnitPrice = decimal.Parse(offer.ApproximateUnitPrice.Value.ToString("0.############################", System.Globalization.CultureInfo.InvariantCulture), System.Globalization.CultureInfo.InvariantCulture);
            if (offer.FinalUnitPrice.HasValue)
                offer.FinalUnitPrice = decimal.Parse(offer.FinalUnitPrice.Value.ToString("0.############################", System.Globalization.CultureInfo.InvariantCulture), System.Globalization.CultureInfo.InvariantCulture);
        }
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(copy)));
    }

    public static bool HasCurrentOutput(ProcurementData data) => data.LastOutput is { } output
        && output.RecordRevision == data.Revision
        && output.ContentHash == ContentHash(data);
}
