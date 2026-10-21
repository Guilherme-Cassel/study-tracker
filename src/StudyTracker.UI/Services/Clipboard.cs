using Microsoft.JSInterop;

namespace StudyTracker.UI.Services;

// One per browser connection (scoped): copies text to the clipboard through the browser.
public sealed class Clipboard(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;

    public async Task<bool> CopyAsync(string text)
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
    }
}
