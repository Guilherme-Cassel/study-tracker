using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;
using StudyTracker.UI;
using StudyTracker.UI.Services;

namespace StudyTracker.Desktop;

internal sealed class MainForm : Form
{
    private const string Title = "Study Tracker";

    private readonly StatusState _state;

    public MainForm(IServiceProvider services)
    {
        _state = services.GetRequiredService<StatusState>();

        Text = Title;
        Icon = AppIcon.Load();
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1280, 860);
        MinimumSize = new Size(420, 560);

        var view = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            HostPage = Path.Combine("wwwroot", "index.html"),
            Services = services
        };
        view.RootComponents.Add<Routes>("#app");
        view.RootComponents.Add<HeadOutlet>("head::after");
        view.BlazorWebViewInitializing += (_, e) =>
            e.UserDataFolder = Path.Combine(ServiceRegistration.UserConfigDirectory, "WebView2");
        view.BlazorWebViewInitialized += (_, e) =>
            e.WebView.CoreWebView2.DocumentTitleChanged += (_, _) => Text = e.WebView.CoreWebView2.DocumentTitle is { Length: > 0 } title ? title : Title;
        Controls.Add(view);

        // Coming back to the window is when changes made elsewhere (Explorer, Claude) matter most.
        Activated += async (_, _) =>
        {
            try { await _state.RefreshAsync(); }
            catch (Exception) { /* FolderMonitor reads the folder again on its own schedule */ }
        };
    }
}
