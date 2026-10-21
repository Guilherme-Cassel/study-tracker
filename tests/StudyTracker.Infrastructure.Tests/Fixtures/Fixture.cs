namespace StudyTracker.Infrastructure.Tests.Fixtures;

internal static class Fixture
{
    // Generated with fmtJson from the static panel (status.html), so it is the exact layout the panel writes.
    public static string SampleText => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "status-dados.sample.js"));
}
