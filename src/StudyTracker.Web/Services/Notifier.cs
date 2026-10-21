namespace StudyTracker.Web.Services;

public sealed record Notice(string Text, bool IsError);

// One per browser connection (scoped): short messages shown at the bottom of the screen.
public sealed class Notifier
{
    public event Action<Notice>? Posted;

    public void Info(string text) => Posted?.Invoke(new Notice(text, false));

    public void Error(string text) => Posted?.Invoke(new Notice(text, true));
}
