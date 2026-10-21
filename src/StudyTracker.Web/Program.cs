using StudyTracker.Infrastructure;
using StudyTracker.Infrastructure.Folders;
using StudyTracker.Infrastructure.Status;
using StudyTracker.UI;
using StudyTracker.UI.Services;
using StudyTracker.Web.Components;
using StudyTracker.Web.Services;

var isDevelopment = string.Equals(
    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), Environments.Development, StringComparison.OrdinalIgnoreCase);

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    // The installed app starts from a shortcut, so its files are next to the exe, not in the working directory.
    ContentRootPath = isDevelopment ? null : AppContext.BaseDirectory
});

builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
    .AddJsonFile(AppLauncher.UserConfigFile, optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

var browserUrl = AppLauncher.BrowserUrl(builder.Configuration);
var openBrowser = builder.Configuration.GetValue<bool>("OpenBrowser");

using var instance = new Mutex(initiallyOwned: true, AppLauncher.InstanceName, out var isFirstInstance);
if (!isFirstInstance)
{
    if (openBrowser) AppLauncher.OpenBrowser(browserUrl);
    return;
}

builder.Services.AddSingleton(new StudyFolder(
    builder.Configuration["StudyFolder:RootPath"],
    builder.Configuration["StudyFolder:DeliverablePrefix"]));
builder.Services.AddSingleton<StatusDataStore>();
builder.Services.AddSingleton<FolderScanner>();
builder.Services.AddSingleton<ExplorerLauncher>();
builder.Services.AddSingleton<AssignmentFileWriter>();
builder.Services.AddSingleton(UploadOptions.From(builder.Configuration));
builder.Services.AddSingleton<StatusState>();
builder.Services.AddHostedService<FolderMonitor>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<Notifier>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Routes).Assembly);

if (openBrowser)
    app.Lifetime.ApplicationStarted.Register(() => AppLauncher.OpenBrowser(browserUrl, app.Logger));

app.Run();
