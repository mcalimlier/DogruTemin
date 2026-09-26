using System.Text.Json;
using DogruTemin.Core.Models;
using DogruTemin.Core.Services;

namespace DogruTemin.Tests;

public sealed class SchemaMigrationServiceTests
{
    [Fact]
    public void OlderSchemaIsAdvancedToCurrentVersion()
    {
        var data = new ProcurementData { SchemaVersion = 1 };

        var migrated = SchemaMigrationService.Migrate(data);

        Assert.Equal(SchemaMigrationService.CurrentSchemaVersion, migrated.SchemaVersion);
        Assert.Same(data, migrated);
    }

    [Fact]
    public void FutureSchemaIsRejected()
    {
        var data = new ProcurementData
        {
            SchemaVersion = SchemaMigrationService.CurrentSchemaVersion + 1
        };

        var exception = Assert.Throws<InvalidDataException>(
            () => SchemaMigrationService.Migrate(data));

        Assert.Contains("daha yeni", exception.Message);
    }

    [Fact]
    public void StorageLoadRejectsFutureSchema()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "DogruTemin_Schema_" + Guid.NewGuid().ToString("N"));

        try
        {
            var storage = new StorageService(root);
            var json = JsonSerializer.Serialize(new ProcurementData
            {
                SchemaVersion = SchemaMigrationService.CurrentSchemaVersion + 1,
                RecordId = "future-schema"
            });

            Directory.CreateDirectory(storage.DataFolder);
            File.WriteAllText(storage.ActiveFile, json);

            var loaded = storage.Load(out var warning);

            Assert.Null(loaded);
            Assert.Contains("daha yeni", warning);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }
}
