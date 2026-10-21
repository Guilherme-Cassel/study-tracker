using System.ComponentModel;
using System.Diagnostics;

namespace StudyTracker.Web.Services;

// What the desktop shortcut needs: one running copy, and the browser opened on it.
public static class AppLauncher
{
    public const string InstanceName = @"Local\StudyTracker";
    public const string DefaultUrl = "http://localhost:5280";

    public static string BrowserUrl(IConfiguration configuration)
    {
        var first = (configuration["Urls"] ?? DefaultUrl).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return Uri.TryCreate(first, UriKind.Absolute, out var uri) && uri.IsLoopback ? uri.ToString() : DefaultUrl;
    }

    public static void OpenBrowser(string url, ILogger? logger = null)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true })?.Dispose();
        }
        catch (Win32Exception ex)
        {
            logger?.LogWarning(ex, "Could not open the browser at {Url}", url);
        }
    }
}
