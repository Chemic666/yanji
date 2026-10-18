using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YanJi.Core;

namespace YanJi.App;

/// <summary>Actual HWND copy checks; only reached through explicit isolated runtime-check mode.</summary>
internal static class UiCopyValidation
{
    internal static async Task RunAsync(MainWindow w,string output)
    {
        var m=w.Model;var d=m.Database;var app=(App)Application.Current;
        if(!app.TestMode)throw new InvalidOperationException("UI copy validation requires explicit isolated test mode");
        var checks=new List<string>();var shots=new List<object>();
        void Check(bool condition,string label){if(!condition)throw new InvalidOperationException(label);checks.Add(label);}
        void Write(string name,object value)=>File.WriteAllText(Path.Combine(output,name),JsonSerializer.Serialize(value,new JsonSerializerOptions{WriteIndented=true}));
        async Task Shot(Window window,string name)
        {
            window.UpdateLayout();await Task.Delay(150);var path=Path.Combine(output,"copy-"+name+".png");NativeCapture.Save(window,path);
            shots.Add(new{File=Path.GetFileName(path),Sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),Width=window.ActualWidth,Height=window.ActualHeight,Dark=m.DarkTheme,Dpi=VisualTreeHelper.GetDpi(window).PixelsPerInchX});
        }
        async Task Theme(bool dark){m.DarkTheme=dark;while(m.Busy)await Task.Delay(25);await Task.Delay(50);}
        static bool Has(byte[] bytes,string text)=>bytes.AsSpan().IndexOf(Encoding.Unicode.GetBytes(text))>=0||bytes.AsSpan().IndexOf(Encoding.UTF8.GetBytes(text))>=0;
        var raw=d.Records().Sum(r=>r.DurationMs);
        var assembly=typeof(App).Assembly;var binary=File.ReadAllBytes(assembly.Location);
        foreach(var text in new[]{"备份已完成并通过校验","恢复备份前请确认","学习记录已导出为 CSV",ProductCopy.RestoreWarning})
            Check(Has(binary,text),"actual compiled IL contains new copy: "+text[..Math.Min(16,text.Length)]);
        using(var stream=assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(n=>n.EndsWith(".g.resources",StringComparison.Ordinal)))!)
        using(var resources=new ResourceReader(stream))
        {
            var iterator=resources.GetEnumerator();byte[]? main=null;
            while(iterator.MoveNext())if(iterator.Key.ToString()=="mainwindow.baml")
            {using var buffer=new MemoryStream();((Stream)iterator.Value!).CopyTo(buffer);main=buffer.ToArray();}
            Check(main!=null,"actual compiled WPF BAML found");
            foreach(var text in new[]{"备份数据","恢复备份","检查数据完整性","日历与统计会同步更新"})Check(Has(main!,text),"compiled BAML contains: "+text);
            foreach(var text in new[]{"隔离候选","关闭窗口进入托盘","整次软删除","QUIET FOCUS 2.0 · RC2"})Check(!Has(main!,text),"compiled main-window BAML removes obsolete copy: "+text);
        }
        Check(ProductCopy.Footer(false,ProductCopy.FullVersion)=="v0.4.0 · 预发布","formal version label derives from this prerelease assembly, without claiming Stable");
        Check(w.Title.Contains("隔离测试")&&w.VersionHint.Text=="隔离测试"&&w.VersionHint.ToolTip!.ToString()!.Contains(ProductCopy.FullVersion),"test label retained and version details match actual assembly");
        d.SaveChapter(new("copy-node","math","函数、极限与连续"));
        d.SaveTask(new("copy-task","整理极限运算的常见方法",m.LocalToday(),false,"math","copy-node"));
        d.SaveWeakPoint(new("copy-weak","等价无穷小的使用条件","区分适用条件，复习后自行验收。","概念理解","math","copy-node",WeakPointState.Reviewing));m.Refresh();
        foreach(var dark in new[]{false,true})
        {
            await Theme(dark);
            foreach(var size in new[]{(1200,800),(900,600)})
            {
                w.Width=size.Item1;w.Height=size.Item2;
                foreach(var page in new[]{0,2,4})
                {
                    m.Page=page;if(page==2)await w.SimpleProgressView.RefreshAsync();w.MainScroll.ScrollToTop();
                    await Shot(w,$"{(page==0?"today":page==2?"progress":"data")}-{(dark?"dark":"light")}-{size.Item1}");
                    Check(w.ActualWidth==size.Item1&&w.ActualHeight==size.Item2,"native layout size "+page+"/"+dark+"/"+size.Item1);
                }
            }
            w.Width=1200;w.Height=800;m.Page=5;w.MainScroll.ScrollToTop();await Shot(w,"settings-"+(dark?"dark":"light"));
            Check(AtelierValidation.Children(w).OfType<Button>().Any(b=>b.IsVisible&&b.Content?.ToString()=="备份数据"),"actual settings backup action discoverable "+dark);
            w.ShowMiniTimer();Check(ReferenceEquals(w.MiniTimer!.DataContext,m)&&w.MiniTimer.Title.Contains("隔离测试"),"mini context and environment label "+dark);await Shot(w.MiniTimer,"mini-"+(dark?"dark":"light"));w.MiniTimer.Hide();
        }
        await Theme(false);m.Page=0;w.MainScroll.ScrollToTop();
        async Task Dialog(Func<Task> show,string name)
        {
            var completed=new TaskCompletionSource<bool>();
            w.TestDialogShown=dialog=>dialog.Loaded+=async(_,_)=>{try{await Shot(dialog,name);completed.TrySetResult(true);}catch(Exception e){completed.TrySetException(e);}finally{dialog.DialogResult=false;}};
            try{await show();await completed.Task.WaitAsync(TimeSpan.FromSeconds(5));Check(true,"actual editor copy captured: "+name);}finally{w.TestDialogShown=null;}
        }
        await Dialog(()=>w.ConfigureExamProfileAsync(),"onboarding");
        await Dialog(()=>{AtelierValidation.Children(w).OfType<Button>().Single(b=>b.IsVisible&&b.Content?.ToString()=="＋ 手动补录").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));return Task.CompletedTask;},"quick-add");
        var package=new KnowledgePackageWindow(w);package.Show();await package.Initialization;await Shot(package,"knowledge-packages");package.Close();
        Check(d.Records().Sum(r=>r.DurationMs)==raw&&d.Integrity()=="ok","copy review keeps original raw learning milliseconds and database intact");
        Write("copy-result.json",new{Status="PASS",Checks=checks,ShortVersion=ProductCopy.ShortVersion(ProductCopy.FullVersion),FullVersion=ProductCopy.FullVersion,Title=w.Title,RawMs=raw,PhysicalIME="NOT TESTED",FormalUserDatabaseOpened=false});
        Write("copy-screenshots.json",shots);
    }
}
