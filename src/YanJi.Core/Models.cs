namespace YanJi.Core;

public enum PartKind { DurationOnly, Interval }
public enum SessionSource { Manual, Timer }
public enum TimerState { Idle, Running, Paused, NeedsReview, Completed }
public enum Mastery { Unknown, Weak, Reviewing, Mastered }
public enum WeakPointState { Open, Reviewing, Verified }

public sealed record StudyPart(string Id, DateOnly Date, long DurationMs, string ZoneId,
    PartKind Kind, long? StartUtcMs = null, long? EndUtcMs = null);

public sealed record StudyRecord(string Id, string SubjectId, string? ChapterId, SessionSource Source,
    string Note, List<StudyPart> Parts, long Version = 0, bool Deleted = false,
    DateTimeOffset? CreatedAt = null)
{
    public long DurationMs => Parts.Sum(x => x.DurationMs);
}

public sealed record Subject(string Id, string Name, bool Archived = false);
public sealed record Chapter(string Id, string SubjectId, string Name, int Progress = 0,
    Mastery Mastery = Mastery.Unknown, bool Archived = false);
public sealed record LearningTask(string Id, string Title, DateOnly? DueDate = null,
    bool Done = false, string? SubjectId = null);
public sealed record WeakPoint(string Id, string Title, string Detail = "", string Category = "",
    string? SubjectId = null, string? ChapterId = null, WeakPointState State = WeakPointState.Open,
    DateOnly? ReviewDate = null);
public sealed record Revision(string Id, string EntityKind, string EntityId, long Version,
    DateTimeOffset At, string Reason, string? BeforeJson, string? AfterJson);
public sealed record DailyTotal(DateOnly Date, long DurationMs);
public sealed record SubjectTotal(string SubjectId, string Name, long DurationMs);
public sealed record ClockSample(DateTimeOffset Utc, long AwakeMs, long BootMs, string ProcessToken);
public sealed record TimerSnapshot(string SessionId, string SubjectId, string? ChapterId, string Note,
    string ZoneId, TimerState State, List<StudyPart> Parts, ClockSample? LastSample,
    long? TrustedEndUtcMs, string? ReviewReason = null, long Version = 0)
{
    public long DurationMs => Parts.Sum(x => x.DurationMs);
    public bool IsActive => State is TimerState.Running or TimerState.Paused or TimerState.NeedsReview;
}
