using YanJi.Core;

namespace YanJi.Tests;

public class TimerMachineTests
{
    private static ClockSample Sample(long milliseconds, string token = "process-a") =>
        new(DateTimeOffset.Parse("2026-10-13T08:00:00Z").AddMilliseconds(milliseconds),
            milliseconds, milliseconds, token);
    private static TimerMachine Started()
    {
        var timer = new TimerMachine();
        timer.Start("math", null, "", "Asia/Shanghai", Sample(0));
        return timer;
    }

    [Fact]
    public void PauseDoesNotCountAndResumeAddsOnlyRunningTime()
    {
        var timer = Started();
        timer.Pause(Sample(30_000));
        timer.Pause(Sample(60_000));
        timer.Resume(Sample(90_000));
        timer.Finish(Sample(105_000));
        Assert.Equal(45_000, timer.Snapshot.DurationMs);
        Assert.Equal(TimerState.Completed, timer.Snapshot.State);
        timer.Finish(Sample(200_000));
        Assert.Equal(45_000, timer.Snapshot.DurationMs);
    }

    [Fact]
    public void PreventsAnotherActiveTimer()
    {
        var timer = Started();
        Assert.Throws<InvalidOperationException>(() =>
            timer.Start("english", null, "", "UTC", Sample(1000)));
    }

    [Theory]
    [InlineData(3_600_000)]
    [InlineData(-3_600_000)]
    public void ClockJumpDoesNotCreditUncertainGap(long jump)
    {
        var timer = Started();
        timer.Advance(Sample(10_000));
        timer.Advance(new(Sample(20_000).Utc.AddMilliseconds(jump), 20_000, 20_000, "process-a"));
        Assert.Equal(TimerState.NeedsReview, timer.Snapshot.State);
        Assert.Equal(10_000, timer.Snapshot.DurationMs);
    }

    [Fact]
    public void ExcludedSleepGapRequiresReviewAndAddsNothing()
    {
        var timer = Started();
        timer.Advance(Sample(10_000));
        timer.Advance(new(Sample(70_000).Utc, 15_000, 70_000, "process-a"));
        Assert.Equal(TimerState.NeedsReview, timer.Snapshot.State);
        Assert.Equal(10_000, timer.Snapshot.DurationMs);
    }

    [Fact]
    public void RunningCheckpointRestartsWithReviewAndNoAddedTime()
    {
        var original = Started();
        original.Advance(Sample(15_000));
        var restored = new TimerMachine();
        restored.Restore(original.Snapshot, Sample(3_600_000, "process-b"));
        Assert.Equal(TimerState.NeedsReview, restored.Snapshot.State);
        Assert.Equal(15_000, restored.Snapshot.DurationMs);
    }

    [Fact]
    public void PausedCheckpointRemainsPausedAcrossProcessRestart()
    {
        var original = Started();
        original.Pause(Sample(15_000));
        var restored = new TimerMachine();
        restored.Restore(original.Snapshot, Sample(3_600_000, "process-b"));
        Assert.Equal(TimerState.Paused, restored.Snapshot.State);
        Assert.Equal(15_000, restored.Snapshot.DurationMs);
    }

    [Fact]
    public void ExplicitReviewResolutionCanAddDurationWithoutInventingInterval()
    {
        var timer = Started();
        timer.Advance(Sample(10_000));
        timer.MarkUncertain("missing evidence");
        timer.ResolveReview([TimeParts.Duration(new DateOnly(2026, 10, 13),
            TimeSpan.FromMinutes(2), "Asia/Shanghai")], Sample(90_000));
        timer.Finish(Sample(100_000));
        Assert.Equal(130_000, timer.Snapshot.DurationMs);
        Assert.Equal(TimerState.Completed, timer.Snapshot.State);
        Assert.Single(timer.Snapshot.Parts, x => x.Kind == PartKind.DurationOnly);
    }

    [Fact]
    public void PauseAcrossMidnightCreditsFiveAndFifteenMinutes()
    {
        var epoch = DateTimeOffset.Parse("2026-10-13T15:50:00Z");
        ClockSample At(long ms) => new(epoch.AddMilliseconds(ms), ms, ms, "process-a");
        var timer = new TimerMachine();
        timer.Start("math", null, "", "Asia/Shanghai", At(0));
        var identity = timer.Snapshot.SessionId;
        timer.Pause(At(300_000));
        timer.Resume(At(900_000));
        timer.Finish(At(1_800_000));
        Assert.Equal(identity, timer.Snapshot.SessionId);
        Assert.Equal(300_000, timer.Snapshot.Parts.Where(x => x.Date == new DateOnly(2026, 10, 13)).Sum(x => x.DurationMs));
        Assert.Equal(900_000, timer.Snapshot.Parts.Where(x => x.Date == new DateOnly(2026, 10, 14)).Sum(x => x.DurationMs));
        Assert.Equal(1_200_000, timer.Snapshot.DurationMs);
    }
    [Theory]
    [InlineData(-1, 20000, "process-a")]
    [InlineData(20000, -1, "process-a")]
    [InlineData(20000, 20000, "process-b")]
    public void CounterResetOrChangedProcessDoesNotCreditGap(long awake,long boot,string token)
    {var timer=Started();timer.Advance(Sample(10000));timer.Advance(new(Sample(20000).Utc,awake,boot,token));Assert.Equal(TimerState.NeedsReview,timer.Snapshot.State);Assert.Equal(10000,timer.Snapshot.DurationMs);}
}
