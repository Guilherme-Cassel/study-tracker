using System.Reflection;
using System.Text.RegularExpressions;

namespace StudyTracker.UI.Services;

public static class AppInfo
{
    // Releases set it to 1.0.<run number>; builds from source get 0.0.0-dev.
    public static string Version { get; } = Describe(
        (Assembly.GetEntryAssembly() ?? typeof(AppInfo).Assembly)
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    public static string Describe(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion)) return "";
        var version = informationalVersion.Split('+')[0].Trim();
        return Regex.IsMatch(version, @"^\d+\.\d+") ? "v" + version : version;
    }
}
