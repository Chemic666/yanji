namespace YanJi.Core;
/// <summary>Ephemeral break clock; it never creates study parts or persistence facts.</summary>
public sealed class NonStudyCountdown
{
    public long RemainingMs { get; private set; }
    public bool Running { get; private set; }
    private ClockSample? _last;
    public void Start(long duration, ClockSample now)
    { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(duration); RemainingMs=duration; Running=true; _last=now; }
    public void Advance(ClockSample now)
    {
        if(!Running||_last==null)return;
        var awake=now.AwakeMs-_last.AwakeMs;var boot=now.BootMs-_last.BootMs;var wall=(now.Utc-_last.Utc).TotalMilliseconds;
        if(now.ProcessToken!=_last.ProcessToken||awake<0||boot<0||Math.Abs(boot-awake)>1000||Math.Abs(wall-awake)>2000){Running=false;_last=now;return;}
        RemainingMs=Math.Max(0,RemainingMs-awake);_last=now;if(RemainingMs==0)Running=false;
    }
    public void Pause(ClockSample now){Advance(now);Running=false;}
    public void Resume(ClockSample now){if(RemainingMs>0){_last=now;Running=true;}}
}
