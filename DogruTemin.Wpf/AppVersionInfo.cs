using System.Reflection;

namespace DogruTemin.Wpf;

public static class AppVersionInfo
{
    public static string InformationalVersion =>
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion?
            .Split('+')[0]
        ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
        ?? "4.2.0";

    public static string Version => InformationalVersion;

    public static string ProductVersion => InformationalVersion.Split('-')[0];

    public static string DisplayVersion
    {
        get
        {
            var value = InformationalVersion;
            if (value.EndsWith("-rc1", StringComparison.OrdinalIgnoreCase))
                return $"v{value[..^4]} RC1";
            return "v" + value;
        }
    }
}
