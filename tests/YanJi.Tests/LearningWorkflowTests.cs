using System.Text.Json;
using YanJi.Core;
using YanJi.Data;
namespace YanJi.Tests;
public sealed class LearningWorkflowTests
{
    private static ClockSample At(long ms)=>new(DateTimeOffset.Parse("2026-10-14T15:59:58Z").AddMilliseconds(ms),ms,ms,"m3");
    private static YanJiDatabase Db()
    {var d=new YanJiDatabase(M1TestDirectories.NewDirectory());d.SaveKnowledgeNodes([new("root","math","根章节"),new("leaf","math","子节点","root"),new("other","math","另一节点")]);d.SaveTask(new("task","原任务",SubjectId:"math",ChapterId:"leaf"));return d;}
    private static void Save(YanJiDatabase d,TimerMachine t){var s=d.SaveTimer(t.Snapshot,"M3 workflow");t.ConfirmSaved(s.Version,s.Context);}
    [Theory][InlineData(FocusMode.Free)][InlineData(FocusMode.Countdown)][InlineData(FocusMode.Pomodoro)]
    public void ContextSurvivesPauseMidnightRecoveryAndRepeatedFinish(FocusMode mode)
    {
        using var d=Db();var t=new TimerMachine();t.Start("math","leaf","","Asia/Shanghai",At(0),new(mode,mode==FocusMode.Free?0:60000,60000),new(TaskId:"task"));Save(d,t);
        Assert.Equal("原任务",t.Snapshot.Context!.TaskTitle);t.Advance(At(1000));t.Advance(At(2000));t.Pause(At(3000));Save(d,t);var raw=t.Snapshot.DurationMs;
        t.Advance(At(6000));Assert.Equal(raw,t.Snapshot.DurationMs);t.Resume(At(6000));t.Advance(At(8000));Save(d,t);
        d.SaveTask(d.Tasks().Single() with{Title="后来任务",ChapterId="other"});
        var recovery=new TimerMachine();recovery.Restore(d.LoadTimer()!,At(10000));Assert.Equal(TimerState.NeedsReview,recovery.Snapshot.State);Assert.Equal(5000,recovery.Snapshot.DurationMs);
        recovery.ResolveReview([],At(10000));recovery.Resume(At(10000));recovery.Advance(At(70000));if(mode==FocusMode.Free)recovery.Finish(At(70000));Save(d,recovery);
        var s=recovery.Snapshot;Assert.Equal(mode==FocusMode.Free?65000:60000,s.DurationMs);Assert.Equal("原任务",s.Context!.TaskTitle);Assert.Equal("根章节 / 子节点",s.Context.NodePath);
        Assert.Equal(2,s.Parts.Select(p=>p.Date).Distinct().Count());Assert.Null(d.LoadTimer());var saved=d.Records().Single();Assert.Equal(s.DurationMs,saved.DurationMs);Assert.Equal(s.Version,d.SaveTimer(s,"retry").Version);
        Assert.False(d.Tasks().Single().Done);Assert.Equal(LearningState.NotStarted,d.KnowledgeNodes().Single(n=>n.Id=="leaf").State);
        d.ExcludePart(saved.Id,saved.Parts[0].Id,saved.Version);var reduced=d.Records().Single();Assert.Equal(saved.DurationMs-saved.Parts[0].DurationMs,d.KnowledgeStudy("root",DateOnly.MinValue,DateOnly.MaxValue).SubtreeDurationMs);
        d.RestorePart(saved.Id,saved.Parts[0].Id,reduced.Version);Assert.Equal(saved.DurationMs,d.KnowledgeStudy("root",DateOnly.MinValue,DateOnly.MaxValue).SubtreeDurationMs);
        var backup=Path.Combine(Path.GetDirectoryName(d.DatabasePath)!,"workflow.yanjibak");d.Backup(backup);d.ValidateBackup(backup);d.RestoreBackup(backup);Assert.Equal("原任务",d.Records().Single().Context!.TaskTitle);
    }
    [Theory][InlineData("after-weak")][InlineData("after-context")]
    public void LinkedWeakPointWriteFailureIsAtomic(string stage)
    {
        using var d=Db();var r=d.SaveRecord(new("manual","math","leaf",SessionSource.Manual,"",[TimeParts.Duration(new(2026,10,15),TimeSpan.FromMilliseconds(12345),"UTC")]),"synthetic");
        var before=JsonSerializer.Serialize(r);var history=d.History(r.Id).Count;
        d.LearningCheckpoint=s=>{if(s==stage)throw new IOException("synthetic failure");};Assert.Throws<IOException>(()=>d.AddWeakPointToStudy(r.Id,r.Version,"新薄弱点",""));
        Assert.Empty(d.WeakPoints());Assert.Equal(before,JsonSerializer.Serialize(d.Records().Single()));Assert.Equal(history,d.History(r.Id).Count);
        d.LearningCheckpoint=null;d.AddWeakPointToStudy(r.Id,r.Version,"新薄弱点","");var after=d.Records().Single();Assert.Single(d.WeakPoints());Assert.Single(after.Context!.WeakPoints!);
        Assert.Throws<InvalidOperationException>(()=>d.AddWeakPointToStudy(r.Id,r.Version,"重复", ""));Assert.Single(d.WeakPoints());
        M1TestDirectories.Report(Path.GetDirectoryName(d.DatabasePath)!,"atomic-weak",new{stage,Before=before,After=JsonSerializer.Serialize(after),Duration=after.DurationMs});
    }
    [Fact] public void ActiveCheckpointAndContextFailureRollBackTogether()
    {
        using var d=Db();var t=new TimerMachine();t.Start("math","leaf","","UTC",At(0),context:new(TaskId:"task"));Save(d,t);var before=JsonSerializer.Serialize(d.LoadTimer());
        t.Advance(At(3000));d.LearningCheckpoint=s=>{if(s=="timer-before-commit")throw new IOException("synthetic failure");};Assert.Throws<IOException>(()=>d.SaveTimer(t.Snapshot));
        Assert.Equal(before,JsonSerializer.Serialize(d.LoadTimer()));Assert.Equal(0,d.Records().Single().DurationMs);
        d.LearningCheckpoint=null;Save(d,t);Assert.Equal(3000,d.Records().Single().DurationMs);
    }
    [Fact] public void TenThousandNodeSummaryIsUniqueAndBounded()
    {
        using var d=Db();var nodes=Enumerable.Range(0,10000).Select(i=>new KnowledgeNode("scale"+i,"math","合成节点"+i,i==0?null:"scale"+((i-1)/20))).ToList();d.SaveKnowledgeNodes(nodes,"synthetic scale");
        d.SaveRecord(new("scale-study","math","scale9999",SessionSource.Manual,"",[TimeParts.Duration(new(2026,10,15),TimeSpan.FromMilliseconds(98765),"UTC")]),"synthetic");
        var watch=System.Diagnostics.Stopwatch.StartNew();var result=d.KnowledgeStudy("scale0",DateOnly.MinValue,DateOnly.MaxValue);watch.Stop();Assert.Equal(98765,result.SubtreeDurationMs);Assert.Equal(1,result.SessionCount);Assert.True(watch.ElapsedMilliseconds<5000);
        M1TestDirectories.Report(Path.GetDirectoryName(d.DatabasePath)!,"knowledge-study-performance",new{Nodes=10000,ElapsedMs=watch.Elapsed.TotalMilliseconds,Result=result.SubtreeDurationMs});
    }
    [Fact] public void ContextSurvivesActualDatabaseCloseReopenWithoutConfirmingGap()
    {
        string path;string id;
        using(var d=Db()){path=d.DirectoryPath;var t=new TimerMachine();t.Start("math","leaf","","UTC",At(0),context:new(TaskId:"task"));Save(d,t);t.Advance(At(3000));Save(d,t);id=t.Snapshot.SessionId;}
        using var reopened=new YanJiDatabase(path);var machine=new TimerMachine();machine.Restore(reopened.LoadTimer()!,At(90000));Assert.Equal(TimerState.NeedsReview,machine.Snapshot.State);Assert.Equal(3000,machine.Snapshot.DurationMs);Assert.Equal("原任务",machine.Snapshot.Context!.TaskTitle);
        machine.ResolveReview([],At(90000));machine.Finish(At(90000));Save(reopened,machine);Assert.Equal(id,reopened.Records().Single().Id);Assert.Equal(3000,reopened.Records().Single().DurationMs);
    }
    [Fact] public void TaskBoundTemplateNodeIsProtectedDuringPackageRollback()
    {
        using var d=Db();var p=KnowledgePackageFormatTests.Example();var result=d.ApplyPackage(d.PreviewPackage(p,"legacy-default",KnowledgePackageLifecycleTests.Targets));var node=d.KnowledgeNodes().Single(n=>n.Name=="知识点");
        d.SaveTask(new("package-task","模板后建立的任务",SubjectId:"408",ChapterId:node.Id));var preview=d.PreviewPackageRollback(result.InstallId);Assert.NotEmpty(preview.Conflicts);Assert.DoesNotContain(preview.Changes,c=>c.After.Id==node.Id&&c.After.Archived);d.ApplyPackage(preview);
        Assert.False(d.KnowledgeNodes().Single(n=>n.Id==node.Id).Archived);Assert.Equal(node.Id,d.Tasks().Single(t=>t.Id=="package-task").ChapterId);
    }
}
