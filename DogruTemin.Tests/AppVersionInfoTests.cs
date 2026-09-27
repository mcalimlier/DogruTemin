using DogruTemin.Wpf;

namespace DogruTemin.Tests;

public class AppVersionInfoTests
{
    [Fact]
    public void VersionComesFromCentralBuildProperties()
    {
        Assert.StartsWith("4.3.0", AppVersionInfo.InformationalVersion, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("4.3.0", AppVersionInfo.ProductVersion);
        Assert.StartsWith("v4.3.0", AppVersionInfo.DisplayVersion, StringComparison.OrdinalIgnoreCase);
    }
}
