namespace StudyTracker.Infrastructure.Tests;

public class StudyFolderTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsMissingPath(string? rootPath)
    {
        var ex = Assert.Throws<ArgumentException>(() => new StudyFolder(rootPath));
        Assert.Contains("StudyFolder:RootPath", ex.Message);
    }

    [Theory]
    [InlineData("Infnet")]
    [InlineData(@"..\Infnet")]
    public void Constructor_RejectsRelativePath(string rootPath)
    {
        Assert.Throws<ArgumentException>(() => new StudyFolder(rootPath));
    }

    [Fact]
    public void Constructor_NormalizesTrailingSeparator()
    {
        var path = Path.GetTempPath();

        var folder = new StudyFolder(path);

        Assert.Equal(Path.TrimEndingDirectorySeparator(Path.GetFullPath(path)), folder.RootPath);
    }

    [Fact]
    public void IsAvailable_IsTrueForExistingFolder()
    {
        Assert.True(new StudyFolder(Path.GetTempPath()).IsAvailable);
    }

    [Fact]
    public void IsAvailable_IsFalseForMissingFolder()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        Assert.False(new StudyFolder(missing).IsAvailable);
    }
}
