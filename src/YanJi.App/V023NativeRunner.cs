using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using YanJi.Core;
namespace YanJi.App;
internal static class V023NativeRunner
{
    internal static async Task RunAsync(MainWindow w,Action<bool,string> check)
    {
        var day=w.Model.LocalToday();
        var heat=new StudyChart{Mode="Heatmap",Width=600,Data=new[]{new PlotPoint("day",day.ToString("MM-dd"),600,"#456653",From:day,To:day,Status:"学习日")}};
        var host=new Window{Owner=w,Content=heat,Width=650,SizeToContent=SizeToContent.Height,ShowActivated=false};
        try
        {
            host.Show();host.UpdateLayout();await Task.Delay(50);
            check(heat.ActualHeight<=110&&heat.HitCount==1&&heat.FirstHitCenter.X>250,"V023 single-day heatmap fills a compact date strip");
            var chart=new StudyChart{Mode="Trend",Width=600,Data=new[]{new PlotPoint("axis","日期",7681000,"#456653",From:day,To:day)}};
            host.Content=chart;host.UpdateLayout();await Task.Delay(50);
            check(AutomationProperties.GetHelpText(chart).Contains("2 小时 8 分 1 秒"),"V023 rendered chart axis uses natural duration units");
        }
        finally{host.Close();}
        var vm=w.Model;var from=vm.StatsFrom;var to=vm.StatsTo;var subject=vm.StatsSubjectId;var date=day.AddDays(-900);
        foreach(var x in new[]{("english",37209872L),("math",33911873L),("politics",31808638L),("408",31268638L)})
            vm.Database.SaveRecord(new("v023-carry-"+x.Item1,x.Item1,null,SessionSource.Manual,"显示核对",[new("v023-p-"+x.Item1,date,x.Item2,"UTC",PartKind.DurationOnly)]),"isolated display fixture");
        vm.Refresh();vm.StatsFrom=date.ToDateTime(TimeOnly.MinValue);vm.StatsTo=vm.StatsFrom;vm.StatsSubjectId="";
        check(vm.Analysis?.DurationMs==134199021&&vm.SubjectDisplayNote.Contains("3 秒"),"V023 real view explains carry without changing canonical total");
        vm.Database.SaveSubject(new("v023-sub","摘要隔离科目"));
        var saved=vm.Database.SaveRecord(new("v023-summary","v023-sub",null,SessionSource.Manual,"",[new("v023-summary-p",date,1,"UTC",PartKind.DurationOnly)]),"isolated summary");
        vm.Database.SaveTask(new("v023-task","真实待办",day,false,"v023-sub"));
        vm.Database.SaveWeakPoint(new("v023-weak","真实薄弱点","需复习","概念","v023-sub",null,WeakPointState.Open,day));vm.Refresh();vm.StatsSubjectId="v023-sub";
        check(vm.ReviewFactSummary.Contains("不足 1 秒")&&vm.ReviewFactSummary.Contains("1 个学习日")&&vm.ReviewFactSummary.Contains("1 次记录"),"V023 review facts retain tiny saved study and selected scope");
        vm.StatsTo=day.ToDateTime(TimeOnly.MinValue);
        check(vm.Analysis?.CalendarAverageMs is >0 and <1&&vm.NaturalAverage=="不足 1 秒","V023 fractional-millisecond calendar average stays nonzero in presentation");
        check(vm.ReviewSnapshotSummary.Contains("1 项到期")&&vm.ReviewSnapshotSummary.Contains("1 项未完成")&&vm.ReviewSnapshotSummary.Contains("不表示本期变化"),"V023 current task and weakness snapshot is distinct from period facts");
        vm.Database.DeleteRecord(saved.Id,saved.Version);vm.Refresh();
        check(vm.ReviewFactSummary.Contains("0 秒")&&vm.ReviewFactSummary.Contains("0 次记录"),"V023 deleting a record refreshes review facts");
        var deleted=vm.Database.Records(null,true).Single(r=>r.Id==saved.Id);vm.Database.RestoreRecord(deleted.Id,deleted.Version);vm.Refresh();
        check(vm.ReviewFactSummary.Contains("不足 1 秒")&&vm.ReviewFactSummary.Contains("1 次记录"),"V023 restoring a record refreshes review facts");
        vm.StatsTo=to;vm.StatsFrom=from;vm.StatsSubjectId=subject;
    }
}
