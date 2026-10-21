namespace StudyTracker.Infrastructure.Status;

public sealed class StatusDataFormatException : Exception
{
    public StatusDataFormatException(string message) : base(message) { }
    public StatusDataFormatException(string message, Exception inner) : base(message, inner) { }
}

public sealed class StatusDataConflictException : Exception
{
    public StatusDataConflictException()
        : base("status-dados.js changed on disk since it was loaded. Reload it and apply the change again.") { }
}
