using Microsoft.JSInterop;

namespace StudyTracker.UI.Services;

// Copies text to the clipboard. The base implementation goes through the browser, which only works from a
// user gesture; a desktop host can register a subclass that uses the operating system clipboard instead.
public class Clipboard(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;

    public virtual async Task<bool> CopyAsync(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        try
        {
            _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./_content/StudyTracker.UI/clipboard.js");
            return await _module.InvokeAsync<bool>("copy", text);
        }
        catch (JSException)
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_module is not null) await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
        GC.SuppressFinalize(this);
    }
}
