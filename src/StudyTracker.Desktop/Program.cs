using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StudyTracker.Infrastructure;
using StudyTracker.UI.Services;

namespace StudyTracker.Desktop;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        using var instance = new Mutex(initiallyOwned: true, @"Local\StudyTracker", out var isFirstInstance);
        if (!isFirstInstance)
        {
            SingleInstance.BringExistingToFront();
            return 0;
        }

        if (CreateBuilder(args) is not { } builder) return 0;

        builder.Services.AddWindowsFormsBlazorWebView();
        builder.Services.AddScoped<StudyTracker.UI.Services.Clipboard, WindowsClipboard>();
        if (builder.Environment.IsDevelopment())
            builder.Services.AddBlazorWebViewDeveloperTools();

        using var host = builder.Build();
        host.Start();
        Application.Run(new MainForm(host.Services));
        host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        return 0;
    }

    // Asks for the study folder while the settings are missing or point to a folder without the data file.
    // Returns null when the user cancels.
    private static HostApplicationBuilder? CreateBuilder(string[] args)
    {
        while (true)
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                Args = args,
                ContentRootPath = AppContext.BaseDirectory
            });
            builder.Configuration
                .AddJsonFile(ServiceRegistration.UserConfigFile, optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .AddCommandLine(args);

            var rootPath = builder.Configuration["StudyFolder:RootPath"];
            var prefix = builder.Configuration["StudyFolder:DeliverablePrefix"];
            if (LocalSettings.CheckStudyFolder(rootPath) is StudyFolderProblem.Missing or StudyFolderProblem.NotAbsolute or StudyFolderProblem.NoDataFile)
            {
                using var setup = new SetupForm(AppIcon.Load(), rootPath, prefix);
                if (setup.ShowDialog() != DialogResult.OK) return null;
                try
                {
                    LocalSettings.Save(ServiceRegistration.UserConfigFile, setup.FolderPath, setup.Prefix);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    MessageBox.Show($"Não consegui salvar as configurações em:\n{ServiceRegistration.UserConfigFile}\n\n{ex.Message}",
                        "Study Tracker", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return null;
                }
                continue;
            }

            try
            {
                builder.Services.AddStudyTracker(builder.Configuration);
                return builder;
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                MessageBox.Show(
                    $"Não consegui iniciar o Study Tracker.\n\n{ex.Message}\n\nConfira as configurações em:\n{ServiceRegistration.UserConfigFile}",
                    "Study Tracker", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
    }
}
