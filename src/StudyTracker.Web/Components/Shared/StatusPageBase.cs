using Microsoft.AspNetCore.Components;
using StudyTracker.Web.Services;

namespace StudyTracker.Web.Components.Shared;

public abstract class StatusPageBase : ComponentBase, IDisposable
{
    [Inject] protected StatusState State { get; set; } = default!;
    [Inject] protected TimeProvider Time { get; set; } = default!;

    protected StatusLoadResult? Result { get; private set; }

    protected DateOnly Today => Time.Today();

    protected override async Task OnInitializedAsync()
    {
        State.Changed += OnStateChanged;
        await LoadAsync();
    }

    protected async Task LoadAsync()
    {
        Result = null;
        Result = await State.GetAsync();
    }

    private void OnStateChanged() => _ = InvokeAsync(async () =>
    {
        Result = await State.GetAsync();
        StateHasChanged();
    });

    public void Dispose()
    {
        State.Changed -= OnStateChanged;
        GC.SuppressFinalize(this);
    }
}
