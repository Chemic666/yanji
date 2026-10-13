using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using YanJi.Core;
namespace YanJi.App;

/// <summary>Deterministic synthetic fixture; independent of smoke timer measurements.</summary>
internal static class V021CaptureRunner
{
    internal sealed record Fixture(int Schema, string Anchor, string Zone, bool Synthetic);
    public static async Task RunAsync(MainWindow w,string output,string manifest)
    {
        Directory.CreateDirectory(output);
        try
        {
            if(Directory.EnumerateFiles(output).Any(p=>Path.GetFileName(p) is not ("provenance.json" or "pid.txt")))throw new InvalidOperationException("Fresh capture output required; earlier evidence preserved.");
            var f=JsonSerializer.Deserialize<Fixture>(File.ReadAllText(manifest))!;
            var vm=w.Model;var day=DateOnly.Parse(f.Anchor);
            if(f.Schema!=1||!f.Synthetic||day!=vm.LocalToday()||f.Zone!="Asia/Shanghai")throw new InvalidOperationException("Invalid synthetic fixture anchor/schema/zone");
            File.WriteAllText(Path.Combine(output,"owned-synthetic.txt"),"V021 synthetic capture only");
            var shots=new List<object>();
            async Task Capture(string state)
            {
                foreach(var dark in new[]{false,true})foreach(var size in new[]{(1200d,800d),(900d,600d)})
                {
                    ThemeService.Apply(dark);w.Width=size.Item1;w.Height=size.Item2;
                    var names=new[]{"today","calendar","plan","weak","statistics","settings","records"};
                    for(var page=0;page<7;page++)
                    {
                        vm.Page=page;await vm.ChangeDayAsync(day.ToDateTime(TimeOnly.MinValue));w.MainScroll.ScrollToTop();w.UpdateLayout();
                        await w.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);await Task.Delay(100);
                        var name=$"{state}-{names[page]}-{(dark?"dark":"light")}-{size.Item1}x{size.Item2}-native.png";
                        NativeCapture.Save(w,Path.Combine(output,name));var dpi=VisualTreeHelper.GetDpi(w);
                        shots.Add(new{File=name,State=state,Page=page,Dark=dark,Width=w.ActualWidth,Height=w.ActualHeight,DpiX=dpi.PixelsPerInchX,DpiY=dpi.PixelsPerInchY,Scroll=w.MainScroll.VerticalOffset,OwnerProcessId=Environment.ProcessId,OwnedHwnd=new System.Windows.Interop.WindowInteropHelper(w).Handle.ToInt64()});
                    }
                    if(state=="filled")
                    {
                        vm.OpenRecords(day,day);vm.OpenDetail(vm.FilteredRecords.First());w.UpdateLayout();await Task.Delay(100);
                        NativeCapture.Save(w,Path.Combine(output,$"detail-{(dark?"dark":"light")}-{size.Item1}x{size.Item2}-native.png"));vm.DrawerOpen=false;
                    }
                }
            }
            await Capture("empty");
            var db=vm.Database;var created=new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue),TimeSpan.FromHours(8));
            var longText=string.Concat(Enumerable.Repeat("理清概念与适用条件，保留推导与错因，下一次复习先回顾边界。",5));
            db.SaveChapter(new("fixture-chapter","math","函数与极限 · "+longText,60,Mastery.Weak));
            db.SaveTask(new("fixture-task","复习极限 · "+longText,day,false,"math"));
            db.SaveWeakPoint(new("fixture-weak","等价无穷小的使用条件 · "+longText,longText,"概念理解","math","fixture-chapter",WeakPointState.Reviewing,day.AddDays(2)));
            db.SaveReflection(day,longText);
            var subjects=new[]{"math","english","408","politics"};
            for(var i=0;i<30;i++)db.SaveRecord(new("fixture-"+i,subjects[i%4],i%4==0?"fixture-chapter":null,SessionSource.Manual,i==0?longText:"章节练习与回顾",[new("part-"+i,day.AddDays(-i),(25+(i*17)%110)*60000L,f.Zone,PartKind.DurationOnly)],CreatedAt:created),"synthetic fixture");
            var raw=new long[]{1,1555,59000,60000,61001};
            for(var i=0;i<raw.Length;i++)db.SaveRecord(new("precision-"+i,"math",null,SessionSource.Manual,"精度样本 "+raw[i]+"ms",[new("precision-part-"+i,day,raw[i],f.Zone,PartKind.DurationOnly)],CreatedAt:created),"synthetic precision");
            var start=new DateTimeOffset(day.AddDays(-1).ToDateTime(new TimeOnly(23,50)),TimeSpan.FromHours(8));
            var parts=TimeParts.Interval(start,start.AddMinutes(30),f.Zone).Select((p,i)=>p with{Id="cross-part-"+i}).ToList();
            db.SaveRecord(new("fixture-cross","math",null,SessionSource.Timer,"跨日：本日20分钟，整次30分钟",parts,CreatedAt:created),"synthetic cross-day");
            var excluded=db.SaveRecord(new("fixture-excluded","english",null,SessionSource.Manual,"有排除历史",[new("excluded-a",day,10000,f.Zone,PartKind.DurationOnly),new("excluded-b",day,20000,f.Zone,PartKind.DurationOnly)],CreatedAt:created),"synthetic exclusion");db.ExcludePart(excluded.Id,"excluded-a",excluded.Version);
            var deleted=db.SaveRecord(new("fixture-deleted","408",null,SessionSource.Manual,"软删除样本",[new("deleted-part",day,12345,f.Zone,PartKind.DurationOnly)],CreatedAt:created),"synthetic deleted");db.DeleteRecord(deleted.Id,deleted.Version);
            vm.Refresh();vm.StatsFrom=day.AddDays(-29).ToDateTime(TimeOnly.MinValue);vm.StatsTo=day.ToDateTime(TimeOnly.MinValue);vm.OpenRecords(day.AddDays(-29),day);
            File.WriteAllText(Path.Combine(output,"canonical.json"),JsonSerializer.Serialize(db.Records(null,true),new JsonSerializerOptions{WriteIndented=true}));
            await Capture("filled");
            File.WriteAllText(Path.Combine(output,"screenshots.json"),JsonSerializer.Serialize(shots,new JsonSerializerOptions{WriteIndented=true}));
            File.WriteAllText(Path.Combine(output,"result.json"),JsonSerializer.Serialize(new{Status="PASS",Screenshots=shots.Count+4,Integrity=db.Integrity(),Fixture=f}));
        }
        catch(Exception e){((App)Application.Current).ValidationFailed=true;File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());}
        finally{w.SmokeClose();}
    }
}
