namespace StudyTracker.Core;

internal static class Guard
{
    public static string NotBlank(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} cannot be empty.", name);
        return value;
    }

    public static List<T> NotNull<T>(List<T>? value, string name) =>
        value ?? throw new ArgumentNullException(name);
}
