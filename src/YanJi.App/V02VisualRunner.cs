using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using YanJi.Core;
namespace YanJi.App;
internal static class V02VisualRunner
{
 private static readonly string[] Names=["today","calendar","plan","weak","statistics","settings","records"];
 private static async Task Shot(Window w,string output,string name)
 {w.UpdateLayout();await w.Dispatcher.InvokeAsync(()=>{},DispatcherPriority.ApplicationIdle);await Task.Delay(180);NativeCapture.Save(w,Path.Combine(output,name+"-native.png"));SmokeRunner.Capture(w,Path.Combine(output,name+"-render.png"));}
 public static async Task EmptyAsync(MainWindow w,string output)
 {var vm=w.Model;foreach(var dark in new[]{false,true}){ThemeService.Apply(dark);for(var page=0;page<7;page++){vm.Page=page;w.MainScroll.ScrollToTop();await Shot(w,output,$"empty-{Names[page]}-{(dark?"dark":"light")}");}}ThemeService.Apply(vm.DarkTheme);vm.Page=0;}
 public static async Task RunAsync(MainWindow w,string output,Action<bool,string> check)
 {
  var vm=w.Model;var today=vm.LocalToday();vm.StatsSubjectId="math";vm.Refresh();
  vm.Page=0;w.UpdateLayout();
  var selector=VisualUpgradeRunner.Descendants(w).OfType<ComboBox>().First(c=>AutomationProperties.GetName(c)=="计时科目");
  var toggle=(ToggleButton)selector.Template.FindName("Toggle",selector);
  check(toggle.ActualWidth>=selector.ActualWidth-1&&toggle.ActualHeight>=selector.ActualHeight-1,"V02 selector full input surface uses actual template hit bounds");
  var selected=selector.SelectedItem;
  ((IToggleProvider)new ToggleButtonAutomationPeer(toggle).GetPattern(PatternInterface.Toggle)!).Toggle();await Task.Delay(80);
  check(selector.IsDropDownOpen&&selector.ItemContainerGenerator.ContainerFromIndex(0) is ComboBoxItem{IsVisible:true},"V02 real selector toggle opens Chinese options");
  selector.IsDropDownOpen=false;selector.SelectedItem=selected;
  vm.Page=4;w.MainScroll.ScrollToTop();w.UpdateLayout();var date=VisualUpgradeRunner.Descendants(w).OfType<DatePicker>().First(d=>d.IsVisible);
  var calendarButton=(Button)date.Template.FindName("PART_Button",date);((IInvokeProvider)new ButtonAutomationPeer(calendarButton).GetPattern(PatternInterface.Invoke)!).Invoke();await Task.Delay(80);
  var datePopup=(Popup)date.Template.FindName("PART_Popup",date);
  // DatePicker installs its internally wired Calendar as Popup.Child; it replaces template content.
  var nativeCalendar=datePopup.Child as Calendar??VisualUpgradeRunner.Descendants(datePopup.Child).OfType<Calendar>().FirstOrDefault();
  check(date.IsDropDownOpen&&datePopup.IsOpen&&nativeCalendar is {IsVisible:true},"V02 real date-picker button opens native calendar");date.IsDropDownOpen=false;
  check(vm.MiniTrend.All(p=>p.SubjectId==null),"V02 recent all-subject trend never inherits statistics subject filter");
  check(vm.MiniTrend.Sum(p=>p.Value)==vm.Database.DailyTotals(today.AddDays(-6),today).Sum(d=>d.DurationMs),"V02 recent trend uses canonical raw milliseconds");
  vm.Page=0;w.UpdateLayout();w.RecentTrend.InvokePoint(w.RecentTrend.Data!.Count-1);await Task.Delay(120);
  check(vm.Page==6&&string.IsNullOrEmpty(vm.RecordSubjectId)&&vm.RecordFrom==vm.RecordTo,"V02 recent trend actual drill preserves all subjects and one date");
  vm.OpenRecords(today.AddDays(-29),today);w.UpdateLayout();var row=vm.FilteredRecords.First();
  var view=VisualUpgradeRunner.Descendants(w.RecordList).OfType<Button>().First(b=>Equals(b.Content,"查看"));
  ((IInvokeProvider)new ButtonAutomationPeer(view).GetPattern(PatternInterface.Invoke)!).Invoke();await Task.Delay(100);
  var focus=Keyboard.FocusedElement as FrameworkElement;
  check(vm.DrawerOpen&&!w.NavigationList.IsEnabled&&!w.MainScroll.IsEnabled,"V02 record drawer disables underlying navigation and page operations");
  check(focus!=null&&w.DetailDrawer.IsAncestorOf(focus),"V02 drawer receives keyboard focus");
  for(var i=0;i<30;i++){(Keyboard.FocusedElement as UIElement)?.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));await Task.Delay(5);}
  check(Keyboard.FocusedElement is DependencyObject inside&&w.DetailDrawer.IsAncestorOf(inside),"V02 drawer cycles Tab within visible detail controls");
  await Shot(w,output,"record-detail-light");
  w.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(w),0,Key.Escape){RoutedEvent=Keyboard.PreviewKeyDownEvent});await Task.Delay(40);
  check(!vm.DrawerOpen&&w.NavigationList.IsEnabled&&w.MainScroll.IsEnabled,"V02 Escape closes drawer and reenables workspace");
  vm.StatsSubjectId="";vm.RecordSubjectId="";vm.RecordChapterId="";vm.RecordSource="";vm.RecordState="active";
  foreach(var dark in new[]{false,true})
  {
   await vm.RunAsync(()=>vm.Database.SaveSetting("theme",dark?"dark":"light"),"视觉验收主题");
   foreach(var size in new[]{(1200d,800d),(900d,600d)})
   {
    w.Width=size.Item1;w.Height=size.Item2;
    for(var page=0;page<7;page++){vm.Page=page;await vm.ChangeDayAsync((page==1?today.AddDays(-1):today).ToDateTime(TimeOnly.MinValue));w.MainScroll.ScrollToTop();await Shot(w,output,$"{Names[page]}-{(dark?"dark":"light")}-{size.Item1}x{size.Item2}");}
    check(w.ActualWidth==size.Item1&&w.ActualHeight==size.Item2,"V02 actual window size "+size.Item1+"x"+size.Item2+" "+(dark?"dark":"light"));
   }
  }
  w.Width=1600;w.Height=1000;vm.Page=4;w.MainScroll.ScrollToTop();await Shot(w,output,"statistics-large-dark");
  check(w.ActualWidth>=1200&&w.ActualHeight>=800,"V02 large window expands workspace within OS tracking limit");
  w.Width=1200;w.Height=800;vm.Page=4;w.MainScroll.ScrollToBottom();await Shot(w,output,"statistics-secondary-dark");
  vm.StatsView=1;w.UpdateLayout();w.MainScroll.ScrollToTop();Keyboard.Focus(w.TrendChart);w.TrendChart.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(w),0,Key.Home){RoutedEvent=Keyboard.KeyDownEvent});
  check(!AutomationProperties.GetHelpText(w.TrendChart).Contains("毫秒")&&AutomationProperties.GetHelpText(w.TrendChart).Contains(DurationText.Format(w.TrendChart.Data![0].Value))&&Application.Current.Windows.Count>0,"V02 chart keyboard provides integer-second duration description");
  w.TrendChart.BringIntoView();w.UpdateLayout();w.Activate();Keyboard.Focus(w.ExitButton);await Task.Delay(80);ToolTipService.SetInitialShowDelay(w.TrendChart,100);
  var pointer=NativeCapture.MoveToOwned(w.TrendChart,w.TrendChart.FirstHitCenter);
  try
  {
   await Task.Delay(700);
   var expected=DurationText.Format(w.TrendChart.Data![0].Value);
   check(w.TrendChart.IsMouseOver&&w.TrendChart.ToolTip is string hover&&hover.Contains(expected),"V02 actual owned-window pointer hover displays integer-second presentation");
   var tooltip=w.TrendChart.HoverTooltip;
   check(tooltip.IsOpen&&tooltip.IsVisible&&tooltip.Content is string text&&text.Contains(expected),"V02 actual chart hover opens its native tooltip popup");NativeCapture.SavePopup(tooltip,Path.Combine(output,"chart-hover-exact-native.png"));
  }
  finally{NativeCapture.RestoreCursor(pointer);ToolTipService.SetInitialShowDelay(w.TrendChart,400);}
  vm.StatsSubjectId="math";w.UpdateLayout();check(w.TrendChart.ToolTip==null,"V02 changing chart filter clears stale precision tooltip");vm.StatsSubjectId="";
  vm.Page=5;w.MainScroll.ScrollToTop();w.UpdateLayout();var theme=VisualUpgradeRunner.Descendants(w).OfType<CheckBox>().First(c=>Equals(c.Content,"深色主题"));Keyboard.Focus(theme);theme.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
  check(Keyboard.FocusedElement!=null,"V02 themed settings retain keyboard navigation");
  var converter=new DurationDisplayConverter();check((string)converter.Convert(1L,typeof(string),null!,System.Globalization.CultureInfo.InvariantCulture)=="0 秒"&&vm.Database.Records().Any(r=>r.Parts.Any(p=>p.DurationMs==1)),"V022 compact display floors seconds while one millisecond facts persist");
  await vm.RunAsync(()=>vm.Database.SaveSetting("theme","light"),"验收完成");vm.Page=0;await vm.ChangeDayAsync(today.ToDateTime(TimeOnly.MinValue));w.MainScroll.ScrollToTop();
  var editor=new EditorDialog(w,"快速补录 · 输入验证",[new("minutes","时长（分钟）*","invalid")],"保留输入，错误直接显示在当前表单。",v=>double.TryParse(v["minutes"],out _)?null:"请输入有效分钟数");
  Exception? editorFailure=null;
  editor.Loaded+=(_,_)=>editor.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(async()=>{try{var save=VisualUpgradeRunner.Descendants(editor).OfType<Button>().First(b=>b.IsDefault);((IInvokeProvider)new ButtonAutomationPeer(save).GetPattern(PatternInterface.Invoke)!).Invoke();await Task.Delay(80);editor.UpdateLayout();check(editor.Values==null&&VisualUpgradeRunner.Descendants(editor).OfType<TextBlock>().Any(t=>t.Text=="请输入有效分钟数"&&t.IsVisible),"V02 editor inline validation keeps the original input");await Shot(editor,output,"editor-inline-error-light");editor.SetField("minutes","30");((IInvokeProvider)new ButtonAutomationPeer(save).GetPattern(PatternInterface.Invoke)!).Invoke();}catch(Exception error){editorFailure=error;editor.Close();}}));
  check(editor.ShowDialog()==true&&editor.Values?["minutes"]=="30","V02 validated editor can correct and save without a second dialog");
  if(editorFailure!=null)throw editorFailure;
  check(vm.Database.Integrity()=="ok","V02 presentation and interaction checks preserve database integrity");
  vm.Page=1;await vm.ChangeDayAsync(today.ToDateTime(TimeOnly.MinValue));w.MainScroll.ScrollToTop();w.UpdateLayout();
  var dayButton=VisualUpgradeRunner.Descendants(w).OfType<Button>().First(b=>b.DataContext is CalendarDay{Selected:true});
  check(dayButton.BorderThickness.Left>=1&&((SolidColorBrush)dayButton.Background).Color==((SolidColorBrush)ThemeService.Brush("AccentSoft")).Color,"V02 selected calendar date has effective fill and visible border");
  var original=(CalendarDay)dayButton.DataContext;var at=vm.Days.IndexOf(original);vm.Days[at]=original with{Status="待核对"};w.UpdateLayout();
  dayButton=VisualUpgradeRunner.Descendants(w).OfType<Button>().First(b=>b.DataContext is CalendarDay{Selected:true});
  check(dayButton.BorderThickness.Left>=1&&((SolidColorBrush)dayButton.BorderBrush).Color==((SolidColorBrush)ThemeService.Brush("Warning")).Color,"V02 pending-review calendar fixture has an effective warning border");
  await Shot(w,output,"calendar-review-state-light");vm.Days[at]=original;
  ThemeService.ApplyPalette(false,true);w.UpdateLayout();var selectedNav=(ListBoxItem)w.NavigationList.Items[1];
  check(((SolidColorBrush)selectedNav.Foreground).Color==SystemColors.HighlightTextColor&&((SolidColorBrush)ThemeService.Brush("AccentSoft")).Color==SystemColors.HighlightColor,"V02 simulated high-contrast selection separates text and background");
  await Shot(w,output,"high-contrast-palette-simulated");ThemeService.Apply(vm.DarkTheme);
 }
}
