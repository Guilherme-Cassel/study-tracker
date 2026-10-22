using Microsoft.JSInterop;

namespace StudyTracker.Desktop;

// Uses the Windows clipboard, which works from any click (the browser clipboard needs a user gesture the
// WebView does not always recognize). Falls back to the browser when the system clipboard is busy.
internal sealed class WindowsClipboard(IJSRuntime js) : StudyTracker.UI.Services.Clipboard(js)
{
    public override async Task<bool> CopyAsync(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var copied = await Task.Run(() =>
        {
            var ok = false;
            var thread = new Thread(() =>
            {
                try
                {
                    System.Windows.Forms.Clipboard.SetText(text);
                    ok = true;
                }
                catch (System.Runtime.InteropServices.ExternalException)
                {
                    // another process holds the clipboard open
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            return ok;
        });
        return copied || await base.CopyAsync(text);
    }
}
