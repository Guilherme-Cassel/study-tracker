using StudyTracker.Infrastructure;
using StudyTracker.Infrastructure.Folders;
using StudyTracker.Infrastructure.Status;
using StudyTracker.Web.Components;
using StudyTracker.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Services.AddSingleton(new StudyFolder(
    builder.Configuration["StudyFolder:RootPath"],
    builder.Configuration["StudyFolder:DeliverablePrefix"]));
builder.Services.AddSingleton<StatusDataStore>();
builder.Services.AddSingleton<FolderScanner>();
builder.Services.AddSingleton<ExplorerLauncher>();
builder.Services.AddSingleton<StatusState>();
builder.Services.AddHostedService<FolderMonitor>();
builder.Services.AddSingleton(TimeProvider.System);

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
    .AddInteractiveServerRenderMode();

app.Run();
