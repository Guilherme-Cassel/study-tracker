using System.Diagnostics;
using System.Runtime.InteropServices;

var directory = AppContext.BaseDirectory;
var server = Path.Combine(directory, "StudyTracker.exe");
if (!File.Exists(server))
{
    ShowError($"Não encontrei {server}.\n\nRode scripts\\install.ps1 de novo.");
    return 1;
}

var start = new ProcessStartInfo(server)
{
    UseShellExecute = false,
    CreateNoWindow = true,
    WorkingDirectory = directory
};
foreach (var arg in args) start.ArgumentList.Add(arg);

using var process = Process.Start(start);
if (process is null)
{
    ShowError("Não consegui iniciar o Study Tracker.");
    return 1;
}

// A second copy opens the browser on the running one and exits with 0; a failed start exits with an error code.
if (process.WaitForExit(TimeSpan.FromSeconds(8)) && process.ExitCode != 0)
{
    ShowError("O Study Tracker não conseguiu iniciar.\n\n" +
        "Confira o caminho da pasta em %LOCALAPPDATA%\\StudyTracker\\appsettings.Local.json, " +
        "se o Google Drive está aberto e se a porta 5280 não está ocupada por outro programa.");
    return 1;
}
return 0;

static void ShowError(string message) => MessageBoxW(IntPtr.Zero, message, "Study Tracker", 0x10);

[DllImport("user32.dll", CharSet = CharSet.Unicode)]
static extern int MessageBoxW(IntPtr owner, string text, string caption, uint type);
