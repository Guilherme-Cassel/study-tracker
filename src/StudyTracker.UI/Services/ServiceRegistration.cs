using Microsoft.Extensions.DependencyInjection;
using StudyTracker.Infrastructure;
using StudyTracker.Infrastructure.Folders;
using StudyTracker.Infrastructure.Status;

namespace StudyTracker.UI.Services;

public static class ServiceRegistration
{
    public static string UserConfigDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StudyTracker");

    // Machine settings, kept outside the installed app so reinstalling does not lose them.
    public static string UserConfigFile => Path.Combine(UserConfigDirectory, "appsettings.Local.json");

    // Throws ArgumentException when StudyFolder:RootPath is missing or not an absolute path.
    public static IServiceCollection AddStudyTracker(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton(new StudyFolder(configuration["StudyFolder:RootPath"], configuration["StudyFolder:DeliverablePrefix"]));
        services.AddSingleton<StatusDataStore>();
        services.AddSingleton<FolderScanner>();
        services.AddSingleton<ExplorerLauncher>();
        services.AddSingleton<AssignmentFileWriter>();
        services.AddSingleton(UploadOptions.From(configuration));
        services.AddSingleton<StatusState>();
        services.AddHostedService<FolderMonitor>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<Notifier>();
        return services;
    }
}
