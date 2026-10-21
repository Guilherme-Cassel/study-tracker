using StudyTracker.UI.Services;

namespace StudyTracker.UI.Tests;

public class AppInfoTests
{
    [Theory]
    [InlineData("1.2.0+3f9c2ab1d", "v1.2.0")]
    [InlineData("1.2.0", "v1.2.0")]
    [InlineData("0.0.0-dev+3f9c2ab", "v0.0.0-dev")]
    [InlineData("v1.0.0-3-g3f9c2ab-local+3f9c2ab", "v1.0.0-3-g3f9c2ab-local")]
    [InlineData("3f9c2ab-local", "3f9c2ab-local")]
    [InlineData(null, "")]
    [InlineData("  ", "")]
    public void Describe_DropsTheCommitHashAndPrefixesNumbers(string? informational, string expected)
    {
        Assert.Equal(expected, AppInfo.Describe(informational));
    }
}
