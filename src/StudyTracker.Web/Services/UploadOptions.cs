namespace StudyTracker.Web.Services;

public sealed record UploadOptions(int MaxFileMegabytes, int MaxFilesPerDrop)
{
    public long MaxFileBytes => MaxFileMegabytes * 1024L * 1024L;

    public static UploadOptions From(IConfiguration configuration)
    {
        var megabytes = configuration.GetValue("StudyFolder:MaxUploadMegabytes", 200);
        if (megabytes is < 1 or > 2048)
            throw new InvalidOperationException("StudyFolder:MaxUploadMegabytes must be between 1 and 2048.");
        return new UploadOptions(megabytes, 20);
    }
}
