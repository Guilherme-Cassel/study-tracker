using Microsoft.JSInterop;

namespace StudyTracker.UI.Services;

public enum ThemeChoice
{
    System,
    Light,
    Dark
}

// One per browser connection (scoped): the light/dark choice, kept in the browser's localStorage.
public sealed class Theme(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _module;

    public ThemeChoice Current { get; private set; }

    public event Action? Changed;

    public static string Key(ThemeChoice choice) => choice switch
    {
        ThemeChoice.Light => "light",
        ThemeChoice.Dark => "dark",
        _ => "system"
    };

    public static ThemeChoice Parse(string? key) => key switch
    {
        "light" => ThemeChoice.Light,
        "dark" => ThemeChoice.Dark,
        _ => ThemeChoice.System
    };

    public static string Label(ThemeChoice choice) => choice switch
    {
        ThemeChoice.Light => "Claro",
        ThemeChoice.Dark => "Escuro",
        _ => "Sistema"
    };

    public async Task LoadAsync()
    {
        var module = await ModuleAsync();
        Current = Parse(await module.InvokeAsync<string?>("get"));
        Changed?.Invoke();
    }

    public async Task SetAsync(ThemeChoice choice)
    {
        var module = await ModuleAsync();
        await module.InvokeVoidAsync("set", Key(choice));
        Current = choice;
        Changed?.Invoke();
    }

    private async Task<IJSObjectReference> ModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./_content/StudyTracker.UI/theme.js");

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
