using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YanJi.Core;
namespace YanJi.App;
public sealed record RecentLearning(string SubjectId,string? ChapterId,string? TaskId,string Label);
public sealed partial class MainViewModel
{
    [ObservableProperty] private LearningTask? manualTask;
    [ObservableProperty] private StudyRecord? completedStudy;
    public ObservableCollection<RecentLearning> RecentLearningItems {get;}=[];
    public bool HasCompletedStudy=>CompletedStudy!=null;
    internal void OpenKnowledgeRecord(StudyRecord record){Page=6;OpenDetail(Row(record));}
    partial void OnCompletedStudyChanged(StudyRecord? value)=>OnPropertyChanged(nameof(HasCompletedStudy));
    internal LearningContext ContextFor(string? node,LearningTask? task=null)
        =>new(node==null?task?.ProfileId??"legacy-default":Database.KnowledgeNodes().Single(n=>n.Id==node).ProfileId,task?.Id);
    internal void RefreshLearning()
    {
        var records=Database.Records();if(CompletedStudy!=null&&!records.Any(r=>r.Id==CompletedStudy.Id))CompletedStudy=null;
        var recent=records.Where(r=>(!Timer.IsActive||r.Id!=Timer.SessionId)&&r.DurationMs>0).OrderByDescending(r=>r.CreatedAt)
            .DistinctBy(r=>(r.SubjectId,r.ChapterId,r.Context?.TaskId)).Take(4).Select(r=>new RecentLearning(r.SubjectId,r.ChapterId,r.Context?.TaskId,
                (AllSubjects.FirstOrDefault(s=>s.Id==r.SubjectId)?.Name??r.SubjectId)+" · "+(r.Context?.NodePath??AllChapters.FirstOrDefault(c=>c.Id==r.ChapterId)?.Name??"未指定知识节点")));
        Replace(RecentLearningItems,recent);
    }
    public async Task StartKnowledgeAsync(string nodeId)
    {
        if(Busy||TimerActive){Status="请先结束或核对当前计时。";return;}
        var node=Database.KnowledgeNodes().Single(n=>n.Id==nodeId);
        TimerSubject=Subjects.FirstOrDefault(s=>s.Id==node.SubjectId);
        TimerChapter=TimerChapters.FirstOrDefault(c=>c.Id==nodeId);FocusTask=null;
        if(TimerSubject==null||TimerChapter==null){Status="目录或科目已归档，请先重新启用。";return;}
        Page=0;await StartAsync();
    }
    [RelayCommand] private async Task ContinueRecentAsync(RecentLearning? recent)
    {
        if(recent==null||Busy||TimerActive)return;
        if(recent.TaskId is {} id&&Database.Tasks().FirstOrDefault(t=>t.Id==id&&!t.Done) is {} task&&task.SubjectId==recent.SubjectId&&task.ChapterId==recent.ChapterId)
        {await StartTaskAsync(task);return;}
        if(recent.ChapterId!=null){await StartKnowledgeAsync(recent.ChapterId);return;}
        TimerSubject=Subjects.FirstOrDefault(s=>s.Id==recent.SubjectId);TimerChapter=TimerChapters.FirstOrDefault();FocusTask=null;Page=0;await StartAsync();
    }
    [RelayCommand] private void DismissCompletedStudy()=>CompletedStudy=null;
}
