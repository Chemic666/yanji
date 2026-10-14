using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using YanJi.Core;
namespace YanJi.App;
public sealed record ReviewRow(PeriodReview Review,string Subject)
{public string Label=>$"{Review.From:yyyy-MM-dd} — {Review.To:yyyy-MM-dd} · {Subject} · v{Review.Version}"+(Review.Deleted?" · 已删除":"");public string Body=>Review.Body;}
public sealed record ReflectionRow(DateOnly Date,string Body){public string Label=>Date.ToString("yyyy-MM-dd");}
public sealed record ReviewTaskRow(LearningTask Task,string Subject)
{public string Title=>Task.Title;public string State=>Task.Done?"当前状态：已完成":"当前状态：待完成";public string Due=>Task.DueDate is DateOnly d?$"计划日期：{d:yyyy-MM-dd}":"未安排计划日期";}
public sealed partial class MainViewModel
{
    [ObservableProperty] private int statsView;
    [ObservableProperty] private string chartGranularity="每日科目投入";
    [ObservableProperty] private string reviewBody="";
    [ObservableProperty] private string reviewEditorLabel="新阶段复盘 · 保存时使用上方范围与科目";
    [ObservableProperty] private bool showDeletedReviews;
    [ObservableProperty] private string reviewTaskSummary="";
    [ObservableProperty] private string reviewFactSummary="";
    [ObservableProperty] private string reviewSnapshotSummary="";
    private PeriodReview? _editingReview;
    public ObservableCollection<ReviewRow> PeriodReviews {get;}=[];
    public ObservableCollection<WeakRow> ReviewWeakPoints {get;}=[];
    public ObservableCollection<ReviewTaskRow> ReviewTasks {get;}=[];
    public ObservableCollection<ReflectionRow> ReviewReflections {get;}=[];
    partial void OnShowDeletedReviewsChanged(bool value)=>RefreshReviewMaterials();
    public void NewPeriodReview(){_editingReview=null;ReviewBody="";ReviewEditorLabel="新阶段复盘 · 保存时使用上方范围与科目";}
    public void EditPeriodReview(ReviewRow row)
    {_editingReview=row.Review;ReviewBody=row.Body;ReviewEditorLabel="编辑阶段复盘 · "+row.Label+"（保留原范围与科目）";}
    public async System.Threading.Tasks.Task SavePeriodReviewAsync()
    {
        var draft=_editingReview??new PeriodReview(Guid.NewGuid().ToString("N"),DateOnly.FromDateTime(StatsFrom),DateOnly.FromDateTime(StatsTo),string.IsNullOrEmpty(StatsSubjectId)?null:StatsSubjectId,ReviewBody);
        var saved=false;await RunAsync(()=>{Database.SavePeriodReview(draft with{Body=ReviewBody});saved=true;},"阶段复盘已保存，独立于每日感想");if(saved)NewPeriodReview();
    }
    private void RefreshReviewMaterials()
    {
        var from=DateOnly.FromDateTime(StatsFrom);var to=DateOnly.FromDateTime(StatsTo);var subject=StatsSubjectId;
        Replace(PeriodReviews,Database.PeriodReviews(ShowDeletedReviews).Where(r=>r.To>=from&&r.From<=to&&(string.IsNullOrEmpty(subject)||r.SubjectId==subject)).Select(r=>new ReviewRow(r,r.SubjectId==null?"全部科目":AllSubjects.FirstOrDefault(s=>s.Id==r.SubjectId)?.Name??r.SubjectId)));
        var weak=WeakPoints.Where(w=>string.IsNullOrEmpty(subject)||w.SubjectId==subject).ToList();
        Replace(ReviewWeakPoints,weak.Select(w=>new WeakRow(w,AllSubjects.FirstOrDefault(s=>s.Id==w.SubjectId)?.Name??"未指定科目",AllChapters.FirstOrDefault(c=>c.Id==w.ChapterId)?.Name??"未指定章节",WeakName(w.State),w.ReviewDate is DateOnly d?$"计划复习 {d:yyyy-MM-dd}"+(d<=LocalToday()&&w.State!=WeakPointState.Verified?" · 待复习":""):"未安排复习","")));
        var tasks=Tasks.Where(t=>string.IsNullOrEmpty(subject)||t.SubjectId==subject).ToList();Replace(ReviewTasks,tasks.Select(t=>new ReviewTaskRow(t,AllSubjects.FirstOrDefault(s=>s.Id==t.SubjectId)?.Name??"未指定科目")));
        ReviewTaskSummary=$"当前任务 {tasks.Count} 项 · 已完成状态 {tasks.Count(t=>t.Done)} 项；无完成日期，不表示本期完成量";
        ReviewFactSummary=Analysis is null?"请先选择有效日期范围":$"本期已保存学习 {Duration(Analysis.DurationMs)} · {Analysis.LearningDays} 个学习日 · {Analysis.Sessions} 次记录";
        ReviewSnapshotSummary=$"当前快照：{weak.Count(w=>w.ReviewDate is DateOnly d&&d<=LocalToday()&&w.State!=WeakPointState.Verified)} 项到期待复习薄弱点 · {tasks.Count(t=>!t.Done)} 项未完成任务；不表示本期变化。";
        Replace(ReviewReflections,Database.Reflections().Where(r=>r.Key>=from&&r.Key<=to&&r.Value.Length>0).OrderBy(r=>r.Key).Select(r=>new ReflectionRow(r.Key,r.Value)));
    }
}
