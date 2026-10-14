using YanJi.Core;
namespace YanJi.Tests;
public class TimerDisplayTests
{
    private static ClockSample At(long ms)=>new(DateTimeOffset.Parse("2026-10-14T00:00:00Z").AddMilliseconds(ms),ms,ms,"display-test");
    [Theory]
    [InlineData(0,"00:00:00")][InlineData(1000,"00:00:01")][InlineData(2000,"00:00:02")][InlineData(3000,"00:00:03")]
    [InlineData(65999,"00:01:05")][InlineData(3661000,"01:01:01")][InlineData(86400000,"24:00:00")]
    public void DigitsIncludeSeconds(long ms,string expected)=>Assert.Equal(expected,DurationText.Timer(ms));
    [Theory][InlineData(FocusMode.Free)][InlineData(FocusMode.Countdown)][InlineData(FocusMode.Pomodoro)]
    public void ControlledClockExcludesPauseAndDelayedDisplayDoesNotDrift(FocusMode mode)
    {
        var t=new TimerMachine();t.Start("math",null,"","UTC",At(0),new(mode,mode==FocusMode.Free?0:120000));
        for(var second=1;second<=3;second++){t.Advance(At(second*1000));Assert.Equal("00:00:0"+second,DurationText.Timer(t.Snapshot.DurationMs));}
        t.Pause(At(3000));t.Advance(At(6000));Assert.Equal(3000,t.Snapshot.DurationMs);
        t.Resume(At(6000));t.Advance(At(68000));Assert.Equal(65000,t.Snapshot.DurationMs);Assert.Equal("00:01:05",DurationText.Timer(t.Snapshot.DurationMs));
        if(mode!=FocusMode.Free)Assert.Equal("00:00:55",DurationText.Timer(t.Snapshot.Focus!.TargetMs-t.Snapshot.DurationMs));
        t.Finish(At(69125));Assert.Equal(66125,t.Snapshot.DurationMs);Assert.Equal("00:01:06",DurationText.Timer(t.Snapshot.DurationMs));
    }
    [Theory][InlineData(FocusMode.Free)][InlineData(FocusMode.Countdown)][InlineData(FocusMode.Pomodoro)]
    public void SharedProjectionUsesRemainingOnlyWhileFiniteTimerIsActive(FocusMode mode)
    {
        var t=new TimerMachine();t.Start("math",null,"","UTC",At(0),new(mode,mode==FocusMode.Free?0:120000));
        t.Advance(At(3000));Assert.Equal(mode==FocusMode.Free?"00:00:03":"00:01:57",DurationText.FocusTimer(t.Snapshot));
        t.Pause(At(3000));t.Advance(At(6000));Assert.Equal(mode==FocusMode.Free?"00:00:03":"00:01:57",DurationText.FocusTimer(t.Snapshot));
        t.Resume(At(6000));t.Advance(At(68000));Assert.Equal(mode==FocusMode.Free?"00:01:05":"00:00:55",DurationText.FocusTimer(t.Snapshot));
        t.Finish(At(69125));Assert.Equal("00:01:06",DurationText.FocusTimer(t.Snapshot));
    }
    [Fact] public void TargetCompletionShowsSavedEffectiveTimeWithoutTickCounting()
    {
        var t=new TimerMachine();t.Start("math",null,"","UTC",At(0),new(FocusMode.Countdown,60000));t.Advance(At(65125));
        Assert.Equal(60000,t.Snapshot.DurationMs);Assert.Equal(TimerState.Completed,t.Snapshot.State);Assert.Equal("00:01:00",DurationText.FocusTimer(t.Snapshot));
    }}
