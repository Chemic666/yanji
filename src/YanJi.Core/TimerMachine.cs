namespace YanJi.Core;

public sealed class TimerMachine
{
    private TimerSnapshot _snapshot = new("", "", null, "", "Asia/Shanghai", TimerState.Idle, [], null, null);
    public TimerSnapshot Snapshot => _snapshot with { Parts = [.. _snapshot.Parts] };
    public void Start(string subjectId, string? chapterId, string note, string zoneId, ClockSample now)
    {
        if (_snapshot.IsActive) throw new InvalidOperationException("已有活动计时。");
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectId);
        TimeParts.ResolveZone(zoneId);
        _snapshot = new(Guid.NewGuid().ToString("N"), subjectId, chapterId, note, zoneId,
            TimerState.Running, [], now, now.Utc.ToUnixTimeMilliseconds());
    }
    public void Advance(ClockSample now)
    {
        if (_snapshot.State != TimerState.Running) return;
        var previous = _snapshot.LastSample!;
        var awake = now.AwakeMs - previous.AwakeMs;
        var boot = now.BootMs - previous.BootMs;
        var wall = (now.Utc - previous.Utc).TotalMilliseconds;
        if (now.ProcessToken != previous.ProcessToken || awake < 0 || boot < 0 ||
            Math.Abs(boot - awake) > 1000 || Math.Abs(wall - awake) > 2000)
        { MarkUncertain("系统时间或电源状态变化，未确认的间隙未计入。"); return; }
        if (awake > 0)
        {
            var start = _snapshot.TrustedEndUtcMs!.Value;
            foreach (var part in TimeParts.Interval(DateTimeOffset.FromUnixTimeMilliseconds(start),
                         DateTimeOffset.FromUnixTimeMilliseconds(checked(start + awake)), _snapshot.ZoneId))
            {
                var last = _snapshot.Parts.LastOrDefault();
                if (last?.Kind == PartKind.Interval && last.Date == part.Date && last.EndUtcMs == part.StartUtcMs)
                    _snapshot.Parts[^1] = last with { DurationMs = last.DurationMs + part.DurationMs, EndUtcMs = part.EndUtcMs };
                else _snapshot.Parts.Add(part);
            }
            _snapshot = _snapshot with { TrustedEndUtcMs = checked(start + awake) };
        }
        _snapshot = _snapshot with { LastSample = now };
    }
    public void Pause(ClockSample now)
    {
        Advance(now);
        if (_snapshot.State == TimerState.Running) _snapshot = _snapshot with { State = TimerState.Paused, LastSample = now };
    }
    public void Resume(ClockSample now)
    {
        if (_snapshot.State != TimerState.Paused) return;
        if (_snapshot.Parts.Any(p => p.Kind == PartKind.Interval && p.EndUtcMs > now.Utc.ToUnixTimeMilliseconds()))
        { MarkUncertain("当前时间早于已有学习区间，请核对后继续。"); return; }
        _snapshot = _snapshot with { State = TimerState.Running, LastSample = now, TrustedEndUtcMs = now.Utc.ToUnixTimeMilliseconds() };
    }
    public void Finish(ClockSample now)
    {
        Advance(now);
        if (_snapshot.State == TimerState.NeedsReview) throw new InvalidOperationException("请先核对未知时间。");
        if (_snapshot.IsActive) _snapshot = _snapshot with { State = TimerState.Completed, LastSample = now };
    }
    public void MarkUncertain(string reason)
    {
        if (_snapshot.IsActive) _snapshot = _snapshot with { State = TimerState.NeedsReview, ReviewReason = reason };
    }
    public void ResolveReview(IEnumerable<StudyPart> extra, ClockSample now)
    {
        if (_snapshot.State != TimerState.NeedsReview) throw new InvalidOperationException("当前不需要核对。");
        var parts = extra.ToList();
        if (parts.Any(p => p.Kind != PartKind.DurationOnly || p.DurationMs <= 0 || p.StartUtcMs != null || p.EndUtcMs != null))
            throw new ArgumentException("核对补记必须是明确日期的纯时长。");
        _snapshot = _snapshot with { Parts = [.. _snapshot.Parts, .. parts], State = TimerState.Paused,
            LastSample = now, TrustedEndUtcMs = now.Utc.ToUnixTimeMilliseconds(), ReviewReason = null };
    }
    public void Restore(TimerSnapshot checkpoint, ClockSample now)
    {
        if (_snapshot.IsActive) throw new InvalidOperationException("已有活动计时。");
        _snapshot = checkpoint with { Parts = [.. checkpoint.Parts], LastSample = now };
        if (checkpoint.State == TimerState.Running) MarkUncertain("上次运行未正常结束；仅恢复已保存的学习部分，请核对未保存时间。");
    }
}
