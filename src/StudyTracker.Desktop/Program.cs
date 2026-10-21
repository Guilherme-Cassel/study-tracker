using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });
        builder.Configuration
            .AddJsonFile(ServiceRegistration.UserConfigFile, optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        try
        {
            builder.Services.AddStudyTracker(builder.Configuration);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            MessageBox.Show(
                "A configuração do Study Tracker está incompleta.\n\n" +
                $"Confira StudyFolder:RootPath em:\n{ServiceRegistration.UserConfigFile}\n\n{ex.Message}",
                "Study Tracker", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }

        builder.Services.AddWindowsFormsBlazorWebView();
        if (builder.Environment.IsDevelopment())
            builder.Services.AddBlazorWebViewDeveloperTools();

        using var host = builder.Build();
        host.Start();
        Application.Run(new MainForm(host.Services));
        host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        return 0;
    }
}
