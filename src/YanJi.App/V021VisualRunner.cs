using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using YanJi.Core;
namespace YanJi.App;
internal static class V021VisualRunner
{
    private static void Click(Button b)=>((IInvokeProvider)new ButtonAutomationPeer(b).GetPattern(PatternInterface.Invoke)!).Invoke();
    private static void PressKey(UIElement element,Key key)=>element.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(element),0,key){RoutedEvent=Keyboard.KeyDownEvent});
    private static bool VisibleIn(MainWindow w,FrameworkElement e)
    {var point=e.TranslatePoint(new Point(),w.MainScroll);return point.Y>=0&&point.Y+e.ActualHeight<=w.MainScroll.ActualHeight-w.MainScroll.Padding.Bottom&&point.X>=0&&point.X+e.ActualWidth<=w.MainScroll.ActualWidth;}
    public static async Task RunAsync(MainWindow w,string output,Action<bool,string> check)
    {
        var vm=w.Model;vm.DrawerOpen=false;vm.StatsSubjectId="";var day=vm.LocalToday();
        vm.Page=3;w.UpdateLayout();var search=VisualUpgradeRunner.Descendants(w).OfType<TextBox>().First(t=>AutomationProperties.GetName(t)=="搜索名称、困难或错误分类");
        check(search.ActualHeight==40,"V021 weak search matches 40 DIP filter height");
        foreach(var size in new[]{(900d,600d),(1200d,800d)})
        {
            w.Width=size.Item1;w.Height=size.Item2;vm.Page=0;w.MainScroll.ScrollToTop();w.UpdateLayout();await Task.Delay(80);
            var actions=VisualUpgradeRunner.Descendants(w).OfType<Button>().Where(b=>b.IsVisible&&b.Content is string s&&(s=="开始"||s=="暂停 / 继续"||s=="结束")).ToList();
            check(actions.Count==3&&actions.All(b=>VisibleIn(w,b)),$"V021 timer three actions inside {size.Item1}x{size.Item2} initial viewport");
            check(Grid.GetColumn(w.TodayReflection)==2,$"V021 today keeps two usable columns at {size.Item1}");
            vm.Page=4;w.MainScroll.ScrollToTop();w.UpdateLayout();await Task.Delay(60);
            check(VisibleIn(w,w.DailyStack),$"V021 entire daily plot and axes inside {size.Item1}x{size.Item2} initial viewport");
            var caption=VisualUpgradeRunner.Descendants(w).OfType<TextBlock>().First(t=>t.IsVisible&&t.Text=="纵轴为实际时长，非归一化百分比；颜色对应科目。");
            check(VisibleIn(w,caption),$"V021 daily plot scope explanation inside {size.Item1}x{size.Item2} initial viewport");
            check(w.MainScroll.ExtentWidth<=w.MainScroll.ViewportWidth+1,$"V021 no main horizontal overflow at {size.Item1}");
        }
        vm.Page=0;w.MainScroll.ScrollToTop();w.UpdateLayout();
        var view=VisualUpgradeRunner.Descendants(w.TodayRecordList).OfType<Button>().First(b=>Equals(b.Content,"查看"));view.Focus();Click(view);await Task.Delay(80);
        check(w.DrawerCloseButton.IsKeyboardFocused,"V021 drawer uses stable named close focus");
        w.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(w),0,Key.Escape){RoutedEvent=Keyboard.PreviewKeyDownEvent});await Task.Delay(80);
        check(view.IsKeyboardFocused,"V021 compact detail Escape returns to original visible action");
        vm.Page=4;w.MainScroll.ScrollToTop();w.SubjectDonut.BringIntoView();w.UpdateLayout();w.Activate();await Task.Delay(100);
        var chart=w.SubjectDonut;var points=chart.Data!;
        check(chart.Mode=="Ranking"&&chart.HitCount==points.Count,"V021 ranking has one hit per canonical subject");
        var first=points[0];Keyboard.Focus(chart);PressKey(chart,Key.Home);PressKey(chart,Key.Enter);await Task.Delay(100);
        check(vm.Page==6&&vm.RecordSubjectId==first.SubjectId&&DateOnly.FromDateTime(vm.RecordFrom)==DateOnly.FromDateTime(vm.StatsFrom)&&DateOnly.FromDateTime(vm.RecordTo)==DateOnly.FromDateTime(vm.StatsTo),"V021 ranking Enter preserves original subject and range");
        check(vm.FilteredRecords.Sum(r=>r.Contribution)==first.Value,"V021 ranking drill record contributions equal raw point milliseconds");
        vm.Page=4;chart.BringIntoView();w.UpdateLayout();var pointer=NativeCapture.MoveToOwned(chart,chart.FirstHitCenter);
        try
        {
            await Task.Delay(350);
            check(chart.HoverTooltip.IsOpen&&chart.HoverTooltip.Content is string s&&s.Contains(DurationText.Exact(first.Value)),"V021 native ranking hover exposes exact original milliseconds");
            NativeCapture.SavePopup(chart.HoverTooltip,Path.Combine(output,"v021-ranking-tooltip-native.png"));
            chart.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice,0,MouseButton.Left){RoutedEvent=Mouse.MouseDownEvent});await Task.Delay(80);
            check(vm.Page==6&&vm.RecordSubjectId==first.SubjectId,"V021 routed mouse at owned pointer ranking hit preserves subject");
        }
        finally{NativeCapture.RestoreCursor(pointer);}
        vm.Page=4;vm.StatsSubjectId="english";w.UpdateLayout();check(!chart.HoverTooltip.IsOpen&&chart.ToolTip==null,"V021 filter change clears native ranking precision popup");
        chart.Data=[new("zero","zero",0,"#456653")];w.UpdateLayout();await Task.Delay(30);check(chart.HitCount==0&&chart.DesiredSize.Height==80,"V021 all-zero ranking has no fictitious hit and compact height");
        chart.Data=[new("tiny","long label for precision boundary",1,"#456653","math"),new("large","large",long.MaxValue/2,"#456653","english")];w.UpdateLayout();await Task.Delay(30);check(chart.HitCount==2,"V021 ranking tiny and extreme values remain finite and independently reachable");
        chart.ClearValue(StudyChart.DataProperty);chart.SetBinding(StudyChart.DataProperty,"SubjectPlot");vm.StatsSubjectId="";
        w.Width=900;w.Height=600;
        foreach(var dark in new[]{false,true})
        {
            ThemeService.Apply(dark);var longText=string.Concat(Enumerable.Repeat("保留条件、错因和下一次复习的边界。",12));
            var editor=new EditorDialog(w,"长文编辑验收",[new("name","章节与记录名称",longText),new("note","多行备注",longText+"\n第二行保留",Multiline:true),new("extra","补充说明",longText)],longText,v=>v["name"]=="有效"?null:"名称需要修正；已有多行输入保留。");
            Exception? failure=null;
            editor.Loaded+=(_,_)=>editor.Dispatcher.BeginInvoke(new Action(async()=>
            {
                try
                {
                    var buttons=VisualUpgradeRunner.Descendants(editor).OfType<Button>().ToList();var save=buttons.First(b=>b.IsDefault);var cancel=buttons.First(b=>b.IsCancel);
                    Click(save);await Task.Delay(50);editor.UpdateLayout();
                    bool Inside(FrameworkElement e){var point=e.TranslatePoint(new Point(),editor);return point.Y>=0&&point.Y+e.ActualHeight<=editor.ActualHeight&&point.X>=0&&point.X+e.ActualWidth<=editor.ActualWidth;}
                    check(editor.Values==null&&Inside(save)&&Inside(cancel)&&save.ActualHeight>=40&&cancel.ActualHeight>=40,"V021 long editor invalid input retains visible save/cancel "+(dark?"dark":"light"));
                    NativeCapture.Save(editor,Path.Combine(output,"v021-long-editor-"+(dark?"dark":"light")+"-native.png"));
                    editor.SetField("name","有效");Click(save);
                }
                catch(Exception e){failure=e;editor.Close();}
            }));
            check(editor.ShowDialog()==true&&editor.Values?["note"]==longText+"\n第二行保留","V021 long editor correction preserves multiline content "+(dark?"dark":"light"));
            if(failure!=null)throw failure;
        }
        ThemeService.Apply(vm.DarkTheme);
        foreach(var ms in new long[]{0,1,1555,59000,60000,61001,3600001})
        {
            var display=new NaturalDuration{Milliseconds=ms,FontSize=28};var host=new Window{Owner=w,Content=display,Width=320,Height=100,ShowActivated=false};host.Show();await Task.Delay(20);
            check(display.Milliseconds==ms&&AutomationProperties.GetName(display).Contains(DurationText.Exact(ms))&&display.Inlines.OfType<Run>().Any(r=>r.FontSize<display.FontSize),"V021 natural duration hierarchy retains "+ms+"ms precision");host.Close();
        }
        vm.Page=0;await vm.ChangeDayAsync(day.ToDateTime(TimeOnly.MinValue));
    }
}

