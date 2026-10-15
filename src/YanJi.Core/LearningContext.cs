namespace YanJi.Core;
public enum ContextCapture { Legacy, Started, Manual, Corrected }
public sealed record StudyWeakPointRef(string Id,string Title);
/// <summary>Session association and label evidence. Node identity remains StudyRecord.ChapterId.</summary>
public sealed record LearningContext(string ProfileId="legacy-default",string? TaskId=null,string TaskTitle="",
    string? NodePath=null,DateTimeOffset? CapturedAt=null,ContextCapture Capture=ContextCapture.Started,
    IReadOnlyList<StudyWeakPointRef>? WeakPoints=null)
{
    public LearningContext Copy()=>this with{WeakPoints=WeakPoints?.ToArray()};
}
public sealed record KnowledgeStudySummary(long OwnDurationMs,long SubtreeDurationMs,int SessionCount,DateOnly? LastStudyDate,
    long UnlinkedDurationMs,IReadOnlyList<StudyRecord> Records,IReadOnlyList<LearningTask> Tasks,IReadOnlyList<WeakPoint> WeakPoints);
