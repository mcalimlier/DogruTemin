using DogruTemin.Core.Models;

namespace DogruTemin.Core.Services;

/// <summary>
/// Controls persisted ProcurementData schema evolution.
/// Version 5 adds record revision and output identity; older applications must not drop them.
/// </summary>
public static class SchemaMigrationService
{
    public const int CurrentSchemaVersion = 5;

    public static ProcurementData Migrate(ProcurementData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.SchemaVersion > CurrentSchemaVersion)
        {
            throw new InvalidDataException(
                $"Kayıt schema sürümü ({data.SchemaVersion}) bu uygulamanın desteklediği sürümden ({CurrentSchemaVersion}) daha yeni.");
        }

        // Versions 1-4 are additive/older representations of the same model.
        // Missing JSON properties are populated by model defaults during deserialization.
        // Keep this explicit migration step so future version-specific transforms have
        // a single, testable place to live.
        while (data.SchemaVersion < CurrentSchemaVersion)
        {
            data.SchemaVersion++;
        }

        return data;
    }
}
