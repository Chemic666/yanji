using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YanJi.Core;

namespace YanJi.App;
public sealed partial class MainViewModel
{
    [ObservableProperty] private int focusModeIndex;
    [ObservableProperty] private string focusMinutes="45";
    [ObservableProperty] private string breakMinutes="5";
    [ObservableProperty] private LearningTask? focusTask;
    [ObservableProperty] private string focusTaskLabel="自由开始 · 也可从今日待办开始";
    [ObservableProperty] private string focusRemainingText="自由计时 · 按实际有效时长保存";
    [ObservableProperty] private bool breakActive;
    [ObservableProperty] private double focusProgress;
    [ObservableProperty] private bool canStartFocus=true;
    [ObservableProperty] private bool canPauseFocus;
    [ObservableProperty] private string focusStartLabel="开始学习";
    [ObservableProperty] private bool configuredTimed;
    [ObservableProperty] private bool configuredPomodoro;
    [ObservableProperty] private bool hasLinkedTask;
    [ObservableProperty] private bool hasFocusGoal;
    [ObservableProperty] private string focusSubjectLabel="有效学习时间 · HH:mm:ss";
    [ObservableProperty] private bool focusSetupVisible=true;
    [ObservableProperty] private bool canCompleteLinkedTask;
    private NonStudyCountdown? _break;
    public ObservableCollection<LearningTask> TodayTasks {get;}=[];
    partial void OnFocusModeIndexChanged(int value)
    {if(!TimerActive)FocusMinutes=value==2?"25":"45";UpdateFocus();}
    partial void OnFocusMinutesChanged(string value)=>UpdateFocus();
    partial void OnBusyChanged(bool value)=>UpdateFocus();
    private void LoadFocusPreferences()
    {
        FocusModeIndex=int.TryParse(Database.Setting("focus-mode","0"),out var mode)&&mode is >=0 and <=2?mode:0;
        FocusMinutes=Database.Setting("focus-minutes",mode==2?"25":"45");BreakMinutes=Database.Setting("break-minutes","5");
        if(Timer.Focus is {} focus){FocusModeIndex=(int)focus.Mode;FocusMinutes=(focus.TargetMs/60000).ToString();BreakMinutes=(focus.BreakMs/60000).ToString();FocusTask=Database.Tasks().FirstOrDefault(t=>t.Id==(Timer.Context!=null?Timer.Context.TaskId:focus.TaskId));}
    }
    private FocusContext CreateFocus()
    {
        if(FocusModeIndex is <0 or >2)throw new ArgumentException("请选择计时模式。");
        if(FocusModeIndex!=0&&(!int.TryParse(FocusMinutes,out var n)||n is <1 or >720))throw new ArgumentException("专注时长请输入 1～720 的整数分钟。");
        var rest=5;
        if(FocusModeIndex==2&&(!int.TryParse(BreakMinutes,out rest)||rest is <1 or >60))throw new ArgumentException("休息请输入 1～60 的整数分钟。");
        var task=FocusTask==null?null:Database.Tasks().FirstOrDefault(t=>t.Id==FocusTask.Id);
        if(FocusTask!=null&&(task==null||task.Done))throw new ArgumentException("任务已完成或不存在，请重新选择待办。");
        return new((FocusMode)FocusModeIndex,FocusModeIndex==0?0:long.Parse(FocusMinutes)*60000,rest*60000);
    }
    private void UpdateFocus()
    {
        var s=Timer;var focus=s.IsActive?s.Focus:null;
        FocusSetupVisible=!s.IsActive;
        var subject=s.IsActive?AllSubjects.FirstOrDefault(x=>x.Id==s.SubjectId)?.Name:TimerSubject?.Name;
        TimerText=DurationText.FocusTimer(s);
        FocusSubjectLabel=(subject??"请选择科目")+(focus is {Mode:not FocusMode.Free}?" · 剩余时间 HH:mm:ss":" · 有效学习时间 HH:mm:ss")+(focus?.Mode==FocusMode.Pomodoro?" · 番茄钟":focus?.Mode==FocusMode.Countdown?" · 倒计时":"");
        ConfiguredTimed=FocusModeIndex!=0;ConfiguredPomodoro=FocusModeIndex==2;HasLinkedTask=(s.IsActive?s.Context?.TaskId??(s.Context==null?focus?.TaskId:null):FocusTask?.Id)!=null;HasFocusGoal=focus is {Mode:not FocusMode.Free};
        var linkedId=s.IsActive?(s.Context!=null?s.Context.TaskId:focus?.TaskId):FocusTask?.Id;CanCompleteLinkedTask=!Busy&&linkedId!=null&&Tasks.Any(t=>t.Id==linkedId&&!t.Done);
        FocusTaskLabel=s.IsActive?(linkedId==null?"自由开始 · 未关联任务":"任务 · "+(s.Context?.TaskTitle??focus?.TaskTitle)):FocusTask!=null?"任务 · "+FocusTask.Title:"自由开始 · 也可从今日待办开始";
        FocusRemainingText=focus is {Mode:not FocusMode.Free}?"已学习 "+Duration(s.DurationMs)+" · 目标 "+Duration(focus.TargetMs):!s.IsActive&&FocusModeIndex!=0?"目标 "+FocusMinutes+" 分钟 · 开始后仅扣减有效学习时间":"自由计时 · 按实际有效时长保存";
        FocusProgress=focus is {TargetMs:>0}?Math.Clamp(s.DurationMs/(double)focus.TargetMs*100,0,100):0;
        if(BreakActive&&_break!=null){FocusSubjectLabel="休息剩余时间 · HH:mm:ss";TimerText=DurationText.Timer(_break.RemainingMs);TimerStateText=_break.RemainingMs==0?"休息结束 · 主动开始下一轮":_break.Running?"休息中 · 不计入学习时长":"休息已暂停 · 不计入学习时长";FocusRemainingText="休息剩余 "+Duration(_break.RemainingMs);}
        CanStartFocus=!Busy&&!s.IsActive;CanPauseFocus=!Busy&&!NeedsReview&&(s.IsActive||BreakActive&&_break?.RemainingMs>0);
        FocusStartLabel=BreakActive?"开始下一轮":FocusModeIndex==0?"开始学习":"开始专注";
    }
    private void RefreshFocusTasks()=>Replace(TodayTasks,Tasks.Where(t=>!t.Done&&(t.DueDate==null||t.DueDate<=LocalToday())).OrderBy(t=>t.DueDate??DateOnly.MaxValue));
    private void OnFocusSaved(TimerSnapshot saved)
    {
        if(saved.Focus is {Mode:FocusMode.Pomodoro} focus&&saved.DurationMs>=focus.TargetMs)
        {_break=new();_break.Start(focus.BreakMs,WindowsClock.Sample());BreakActive=true;Status="本轮已保存 · 休息不计入学习，下一轮由你开始。";}
    }
    [RelayCommand] private async Task StartTaskAsync(LearningTask? selected)
    {
        if(Busy||TimerActive||selected==null){Status="请先结束或核对当前计时，再开始任务。";return;}
        var task=Database.Tasks().FirstOrDefault(t=>t.Id==selected.Id);if(task==null||task.Done){Status="请选择未完成任务。";return;}
        if(task.SubjectId!=null){var subject=Subjects.FirstOrDefault(s=>s.Id==task.SubjectId);if(subject==null){Status="任务科目已归档，请先调整任务科目。";return;}TimerSubject=subject;}
        TimerChapter=TimerChapters.FirstOrDefault(c=>c.Id==task.ChapterId)??TimerChapters.FirstOrDefault();if(task.ChapterId!=null&&TimerChapter?.Id!=task.ChapterId){Status="任务知识节点已归档，请先调整任务。";return;}FocusTask=task;Page=0;await StartAsync();if(TimerActive&&task.SubjectId==null)Status="任务未指定科目；本次使用 "+TimerSubject?.Name+"，关联已保存。";
    }
    [RelayCommand] private void ClearFocusTask(){if(!TimerActive){FocusTask=null;UpdateFocus();}}
    [RelayCommand] private void SkipBreak(){_break=null;BreakActive=false;UpdateTimer();}
    [RelayCommand] private async Task CompleteFocusTaskAsync()
    {
        var id=Timer.IsActive?(Timer.Context!=null?Timer.Context.TaskId:Timer.Focus?.TaskId):FocusTask?.Id;if(id==null)return;
        var task=Database.Tasks().FirstOrDefault(t=>t.Id==id);if(task!=null)await RunAsync(()=>Database.SaveTask(task with{Done=true}),"任务已由你标记完成；计时状态不受影响。");
        if(FocusTask?.Id==id)FocusTask=null;UpdateFocus();
    }
    internal async Task<bool> PauseBreakAsync()
    {if(!BreakActive||_break==null)return false;if(_break.Running)_break.Pause(WindowsClock.Sample());else _break.Resume(WindowsClock.Sample());UpdateTimer();await Task.CompletedTask;return true;}
}
