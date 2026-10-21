using Microsoft.AspNetCore.Components;
using StudyTracker.Web.Services;

namespace StudyTracker.Web.Components.Shared;

public abstract class StatusPageBase : ComponentBase
{
    [Inject] protected StatusState State { get; set; } = default!;
    [Inject] protected TimeProvider Time { get; set; } = default!;

    protected StatusLoadResult? Result { get; private set; }

    protected DateOnly Today => Time.Today();

    protected override Task OnInitializedAsync() => LoadAsync();

    protected async Task LoadAsync()
    {
        Result = null;
        Result = await State.GetAsync();
    }
}
