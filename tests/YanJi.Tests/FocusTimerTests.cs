using System.Text.Json;
using YanJi.Core;
using YanJi.Data;

namespace YanJi.Tests;
public class FocusTimerTests
{
    private static ClockSample At(long ms)=>new(DateTimeOffset.Parse("2026-10-13T15:59:50Z").AddMilliseconds(ms),ms,ms,"test");
    private static FocusContext Plan(FocusMode mode=FocusMode.Countdown,long target=60000)=>new(mode,target,300000,"task","开始时任务");
    [Fact] public void DelayedTickCapsAtTargetAndSplitsMidnight()
    {
        var timer=new TimerMachine();timer.Start("math",null,"","Asia/Shanghai",At(0),Plan());timer.Advance(At(90000));
        Assert.Equal(TimerState.Completed,timer.Snapshot.State);Assert.Equal(60000,timer.Snapshot.DurationMs);
        Assert.Equal(new long[]{10000,50000},timer.Snapshot.Parts.Select(p=>p.DurationMs));
        Assert.Equal(At(60000).Utc.ToUnixTimeMilliseconds(),timer.Snapshot.Parts[^1].EndUtcMs);
        timer.Advance(At(120000));Assert.Equal(60000,timer.Snapshot.DurationMs);
    }
    [Theory] [InlineData(FocusMode.Countdown)] [InlineData(FocusMode.Pomodoro)]
    public void PauseIsExcludedFromTarget(FocusMode mode)
    {
        var t=new TimerMachine();t.Start("math",null,"","UTC",At(0),Plan(mode));t.Pause(At(10000));t.Resume(At(100000));t.Advance(At(155000));
        Assert.Equal(60000,t.Snapshot.DurationMs);Assert.Equal(TimerState.Completed,t.Snapshot.State);Assert.Equal(2,t.Snapshot.Parts.Count);
    }
    [Fact] public void SleepGapCannotCompleteGoalAndRestartRetainsPlan()
    {
        var t=new TimerMachine();t.Start("math",null,"","UTC",At(0),Plan());t.Advance(At(10000));
        t.Advance(new(At(90000).Utc,11000,90000,"test"));Assert.Equal(TimerState.NeedsReview,t.Snapshot.State);Assert.Equal(10000,t.Snapshot.DurationMs);
        var copy=new TimerMachine();copy.Restore(JsonSerializer.Deserialize<TimerSnapshot>(JsonSerializer.Serialize(t.Snapshot))!,At(90000));
        Assert.Equal(Plan(),copy.Snapshot.Focus);copy.ResolveReview([],At(90000));copy.Resume(At(90000));copy.Advance(At(150000));Assert.Equal(60000,copy.Snapshot.DurationMs);
    }
    [Fact] public void LegacySnapshotRemainsFreeTimer()
    {
        var t=new TimerMachine();t.Start("math",null,"","UTC",At(0));t.Advance(At(100000));Assert.Null(t.Snapshot.Focus);Assert.Equal(TimerState.Running,t.Snapshot.State);
    }
    [Fact] public void InvalidPlanDoesNotStart()
    {
        var t=new TimerMachine();Assert.Throws<ArgumentException>(()=>t.Start("math",null,"","UTC",At(0),Plan(target:0)));Assert.False(t.Snapshot.IsActive);
    }
    [Fact] public void BreakClockExcludesPauseAndStopsAtDiscontinuity()
    {
        var rest=new NonStudyCountdown();rest.Start(60000,At(0));rest.Pause(At(10000));rest.Advance(At(90000));Assert.Equal(50000,rest.RemainingMs);
        rest.Resume(At(90000));rest.Advance(new(At(150000).Utc,91000,150000,"test"));Assert.False(rest.Running);Assert.Equal(50000,rest.RemainingMs);
        rest.Resume(At(150000));rest.Advance(At(220000));Assert.Equal(0,rest.RemainingMs);Assert.False(rest.Running);
    }
    [Fact] public void InvalidTaskRollsBackAndActivePlanCannotBeReplaced()
    {
        var dir=Path.Combine(Path.GetTempPath(),"yanji-focus-"+Guid.NewGuid().ToString("N"));using var db=new YanJiDatabase(dir);var t=new TimerMachine();t.Start("math",null,"","UTC",At(0),Plan());
        Assert.Throws<ArgumentException>(()=>db.SaveTimer(t.Snapshot));Assert.Null(db.LoadTimer());Assert.Empty(db.Records());
        db.SaveTask(new("task","标题",SubjectId:"math"));var saved=db.SaveTimer(t.Snapshot);var before=db.LoadTimer();
        Assert.Throws<ArgumentException>(()=>db.SaveTimer(saved with{Focus=Plan(FocusMode.Pomodoro)}));Assert.Equal(JsonSerializer.Serialize(before),JsonSerializer.Serialize(db.LoadTimer()));Assert.Equal(Plan(),db.Records().Single().Focus);
    }
    [Fact] public void EarlyFinishKeepsActualDurationAndTaskIsNotCompleted()
    {
        var dir=Path.Combine(Path.GetTempPath(),"yanji-focus-"+Guid.NewGuid().ToString("N"));using var db=new YanJiDatabase(dir);
        db.SaveTask(new("task","任务",SubjectId:"math"));var t=new TimerMachine();t.Start("math",null,"","UTC",At(0),Plan());var saved=db.SaveTimer(t.Snapshot,"开始");t.ConfirmSaved(saved.Version);
        t.Finish(At(23000));saved=db.SaveTimer(t.Snapshot,"提前结束");Assert.Null(db.LoadTimer());Assert.Equal(23000,db.Records().Single().DurationMs);Assert.Equal(Plan(),db.Records().Single().Focus);Assert.False(db.Tasks().Single().Done);
        Assert.Equal(saved.Version,db.SaveTimer(saved,"重复完成").Version);
        var record=db.Records().Single();record=db.SaveRecord(record with{Note="修正备注"},"修正");Assert.Equal(Plan(),record.Focus);
        db.DeleteRecord(record.Id,record.Version);var deleted=db.Records(null,true).Single();db.RestoreRecord(deleted.Id,deleted.Version);Assert.Equal(Plan(),db.Records().Single().Focus);
        var backup=Path.Combine(dir,"test.yanjibak");db.Backup(backup);db.ValidateBackup(backup);db.RestoreBackup(backup);Assert.Equal(Plan(),db.Records().Single().Focus);Assert.Equal("ok",db.Integrity());
    }
}
