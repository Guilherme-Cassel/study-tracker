using StudyTracker.UI.Services;

namespace StudyTracker.UI.Tests;

public class AppInfoTests
{
    [Theory]
    [InlineData("1.2.0+3f9c2ab1d", "v1.2.0")]
    [InlineData("1.2.0", "v1.2.0")]
    [InlineData("0.0.0-dev+3f9c2ab", "v0.0.0-dev")]
    [InlineData("1.0.12+3f9c2ab", "v1.0.12")]
    [InlineData("3f9c2ab", "3f9c2ab")]
    [InlineData(null, "")]
    [InlineData("  ", "")]
    public void Describe_DropsTheCommitHashAndPrefixesNumbers(string? informational, string expected)
    {
        Assert.Equal(expected, AppInfo.Describe(informational));
    }
}
