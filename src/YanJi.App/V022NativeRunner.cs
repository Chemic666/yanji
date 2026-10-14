using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using YanJi.Core;
namespace YanJi.App;
internal static class V022NativeRunner
{
    public static void Empty(MainWindow w,Action<bool,string> check)
    {
        w.Model.Page=0;w.UpdateLayout();
        check(w.Model.TimerChapters.First().Name.Contains("暂无章节"),"V022 initial chapter selector teaches empty state");
        check(w.Model.TimerText=="00:00","V022 timer uses total HH:mm");
        check(VisualUpgradeRunner.Descendants(w).OfType<Button>().Any(b=>Equals(b.Content,"管理章节")),"V022 chapter management shortcut exists");
    }
    public static async Task RunAsync(MainWindow w,Action<bool,string> check)
    {
        var vm=w.Model;var day=vm.LocalToday();
        vm.Database.SaveChapter(new("v022-archived","math","归档章节",Archived:true));vm.Refresh();
        vm.Database.SaveChapter(new("v022-drill","math","下钻章节"));vm.Database.SaveRecord(new("v022-drill-record","math","v022-drill",SessionSource.Manual,"",[new("v022-drill-part",day,1200,"Asia/Shanghai",PartKind.DurationOnly)]),"synthetic drill");vm.Refresh();
        vm.Database.SaveRecord(new("v022-one-ms","math",null,SessionSource.Manual,"极短学习",[new("v022-one-ms-part",day,1,"Asia/Shanghai",PartKind.DurationOnly)]),"synthetic precision");vm.Refresh();
        vm.ManualSubject=vm.Subjects.First(s=>s.Id=="english");
        check(vm.ManualChapters.All(c=>c.Id==""||c.SubjectId=="english"),"V022 subject change cannot retain foreign chapter");
        vm.ManualSubject=vm.Subjects.First(s=>s.Id=="math");check(vm.ManualChapters.All(c=>!c.Archived),"V022 archived chapters cannot be newly selected");
        var editor=new EditorDialog(w,"章节依赖验收",[new("subject","科目","math",[new("math","数学"),new("english","英语")]),new("chapter","章节","v022-archived",[new("","不指定")])]);editor.BindChapters(vm.AllChapters.ToList(),"v022-archived");
        Exception? editorFailure=null;editor.Loaded+=(_,_)=>editor.Dispatcher.BeginInvoke(new Action(()=>
        {try{var chapterBox=VisualUpgradeRunner.Descendants(editor).OfType<ComboBox>().First(c=>AutomationProperties.GetName(c)=="章节");check(chapterBox.Items.OfType<Choice>().Any(c=>c.Value=="v022-archived"),"V022 edit dialog retains existing archived chapter reference");editor.SetField("subject","english");check(chapterBox.SelectedValue?.ToString()==""&&chapterBox.Items.OfType<Choice>().All(c=>c.Value==""||vm.AllChapters.Any(ch=>ch.Id==c.Value&&ch.SubjectId=="english"&&!ch.Archived)),"V022 edit dialog subject switch refreshes only eligible chapters");}catch(Exception e){editorFailure=e;}finally{editor.Close();}}));editor.ShowDialog();if(editorFailure!=null)throw editorFailure;
        vm.StatsFrom=day.AddDays(-500).ToDateTime(TimeOnly.MinValue);vm.StatsTo=day.ToDateTime(TimeOnly.MinValue);vm.StatsSubjectId="math";
        check(vm.DailyPlot.Count<100&&vm.DailyPlot.All(p=>p.From!=null&&p.To!=null),"V022 long main chart uses explicit bounded buckets");
        var chapter=vm.ChapterPlot.First(p=>p.Key!="__unspecified");vm.OpenRecords(DateOnly.FromDateTime(vm.StatsFrom),day,chapter.SubjectId,chapter.Key);
        check(vm.RecordSubjectId==chapter.SubjectId&&vm.RecordChapterId==chapter.Key&&vm.FilteredRecords.All(r=>r.Record.ChapterId==chapter.Key),"V022 chapter drill retains owning subject chapter and range");
        vm.OpenRecords(DateOnly.FromDateTime(vm.StatsFrom),day,"math","__unspecified");check(vm.FilteredRecords.All(r=>r.Record.ChapterId==null),"V022 unspecified chapter has independent detail filter");
        vm.StatsFrom=day.AddDays(-29).ToDateTime(TimeOnly.MinValue);vm.StatsSubjectId="";vm.Page=4;w.UpdateLayout();
        check(VisualUpgradeRunner.Descendants(w).OfType<Button>().Count(b=>b.Content is string s&&new[]{"数据概览","科目与章节","学习复盘"}.Contains(s))==3,"V022 three statistics destinations share filters");
        vm.NewPeriodReview();vm.ReviewBody="实际阶段复盘输入";await vm.SavePeriodReviewAsync();var review=vm.PeriodReviews.First(r=>r.Body=="实际阶段复盘输入");
        vm.EditPeriodReview(review);vm.ReviewBody="修正阶段复盘输入";await vm.SavePeriodReviewAsync();check(vm.PeriodReviews.Any(r=>r.Body=="修正阶段复盘输入"&&r.Review.Version==2),"V022 review editor persists new and edited independent text");
        review=vm.PeriodReviews.First(r=>r.Body=="修正阶段复盘输入");await vm.RunAsync(()=>vm.Database.SetPeriodReviewDeleted(review.Review.Id,review.Review.Version,true),"synthetic delete");check(vm.PeriodReviews.All(r=>r.Review.Id!=review.Review.Id),"V022 review delete refresh hides deleted entity");
        vm.ShowDeletedReviews=true;review=vm.PeriodReviews.First(r=>r.Body=="修正阶段复盘输入");await vm.RunAsync(()=>vm.Database.SetPeriodReviewDeleted(review.Review.Id,review.Review.Version,false),"synthetic restore");check(vm.PeriodReviews.Any(r=>r.Review.Id==review.Review.Id&&!r.Review.Deleted),"V022 review restore refresh retains original entity");vm.ShowDeletedReviews=false;
        vm.Database.SaveSetting("rest-day:"+day.ToString("yyyy-MM-dd"),"true");await vm.StartCommand.ExecuteAsync(null);await Task.Delay(1100);await vm.MarkUncertainAsync("synthetic pending review");
        var prefix=vm.Timer.DurationMs;var heat=vm.StatsHeatmap.First(p=>p.From==day);check(heat.Status.Contains("待核对")&&heat.Status.Contains("休息标记")&&heat.Value>=prefix,"V022 pending confirmed prefix counts while rest flag remains independent");await Task.Delay(100);check(vm.Timer.DurationMs==prefix,"V022 unknown gap does not grow trusted duration");await vm.ResolveAsync([]);await vm.FinishCommand.ExecuteAsync(null);
        vm.StatsView=0;w.UpdateLayout();var center=w.SubjectDonut.DonutCenterBounds;check(Math.Abs(center.Left+center.Width/2-w.SubjectDonut.ActualWidth/2)<.1||w.SubjectDonut.ActualWidth>=420&&Math.Abs(center.Left+center.Width/2-86)<.1,"V022 donut text bounds are mathematically centered");
        foreach(var ms in new long[]{0,1,999,1555,3661001,1000000000000})
        {var display=new NaturalDuration{Milliseconds=ms};var host=new Window{Owner=w,Content=display,Width=360,Height=100,ShowActivated=false};host.Show();await Task.Delay(10);check(AutomationProperties.GetName(display)==DurationText.Format(ms),"V022 accessible duration floors seconds without mutating "+ms);host.Close();}
    }
}
