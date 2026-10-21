namespace StudyTracker.Desktop;

internal static class AppIcon
{
    public static Icon Load() =>
        new(typeof(AppIcon).Assembly.GetManifestResourceStream("StudyTracker.Desktop.app.ico")!);
}
