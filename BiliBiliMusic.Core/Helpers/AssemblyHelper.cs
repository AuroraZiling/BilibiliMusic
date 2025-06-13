namespace BiliBiliMusic.Core.Helpers;

public static class AssemblyHelper
{
    public static string? GetAssemblyVersion()
    {
        var assembly = typeof(AssemblyHelper).Assembly;
        var version = assembly.GetName().Version;
        return version?.ToString(3) ?? "Unknown Version";
    }
}