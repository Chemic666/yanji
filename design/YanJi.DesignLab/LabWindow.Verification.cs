using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YanJi.App;
using YanJi.Core;
namespace YanJi.DesignLab;
public sealed partial class LabWindow
{
 private IEnumerable<FrameworkElement> Elements(DependencyObject? root=null){root??=_root;for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is FrameworkElement element)yield return element;foreach(var item in Elements(child))yield return item;}}
 private async Task VerifyInteractionsAsync(string directory,Func<string,Task> capture)
 {
  _clock.Stop();var checks=new List<object>();
  void Check(string id,bool condition,string detail){checks.Add(new{Id=id,Status=condition?"PASS":"FAIL",Detail=detail});File.WriteAllText(Path.Combine(directory,"interaction-results.json"),JsonSerializer.Serialize(checks,new JsonSerializerOptions{WriteIndented=true}));if(!condition)throw new InvalidOperationException("样板运行验证失败："+id);}
  async Task Until(Func<bool> condition){for(int i=0;i<100&&!condition();i++)await Task.Delay(25);if(!condition())throw new TimeoutException("样板操作等待超时");}
  async Task Click(string name){UpdateLayout();var button=Elements().OfType<Button>().Last(b=>AutomationProperties.GetAutomationId(b)==name||AutomationProperties.GetName(b)==name);button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Task.Delay(60);await Until(()=>!Model.Busy);}
  void Key(LabChart chart,System.Windows.Input.Key key){chart.Focus();chart.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(chart),0,key){RoutedEvent=Keyboard.KeyDownEvent});}
  _direction="A";_dark=false;_stats=false;Width=1200;Height=800;Build();UpdateLayout();
  var before=TodayAnalysis().DurationMs;Check("demo-isolation",File.Exists(Path.Combine(_directory,"VISUAL-LAB-ONLY.txt")),"仅连接具有样板标记的独立数据库");
  var selected=Elements().OfType<ComboBox>().First(c=>AutomationProperties.GetAutomationId(c)=="TimerSubject");Check("selector-template",selected.Template.FindName("Toggle",selected) is System.Windows.Controls.Primitives.ToggleButton toggle&&toggle.HorizontalAlignment==HorizontalAlignment.Stretch,"实际选择器使用铺满模板和右侧箭头");
  await Click("QuickManual");Check("manual-overlay",_root.Children.Count==2,"快速补录真实侧面板");Check("modal-keyboard-isolation",!_root.Children[0].IsEnabled&&KeyboardNavigation.GetTabNavigation(_root.Children[1])==KeyboardNavigationMode.Cycle,"禁用下层并限制顶层Tab循环");
  var minutes=Elements().OfType<TextBox>().Single(t=>AutomationProperties.GetAutomationId(t)=="ManualMinutes");minutes.Text="0.5";Model.ManualNote="视觉验证：中文补录";await Click("ManualSave");await Until(()=>TodayAnalysis().DurationMs==before+30000);
  var added=Model.Database.Records(Model.LocalToday()).Single(r=>r.Note=="视觉验证：中文补录");Check("manual-duration-only",added.Parts.Single().Kind==PartKind.DurationOnly&&added.Parts.Single().StartUtcMs==null&&added.DurationMs==30000,"补录30秒，无虚构起止时间");Check("manual-binding",TodayAnalysis().DurationMs==before+30000&&_root.Children.Count==1,"表单绑定、保存与统计刷新");
  ShowRecord(added);UpdateLayout();Elements().OfType<TextBox>().Single(t=>AutomationProperties.GetAutomationId(t)=="RecordNote").Text="视觉验证：修订后中文备注";await Click("保存备注修订");var edited=Model.Database.Records().Single(r=>r.Id==added.Id);Check("note-revision",edited.Note=="视觉验证：修订后中文备注"&&Model.Database.History(edited.Id).Count>=2,"记录备注修改与修订历史");
  ShowRecord(edited);await Click("删除整次学习…");Check("delete-confirmation",Model.Database.Records().Any(r=>r.Id==edited.Id),"显示删除范围，确认前不改数据");await capture("A-record-delete-confirmation");await Click("确认软删除");Check("soft-delete-refresh",!Model.Database.Records().Any(r=>r.Id==edited.Id)&&TodayAnalysis().DurationMs==before,"软删除后统计立即回到原值");
  var deleted=Model.Database.Records(null,true).Single(r=>r.Id==edited.Id);ShowRecord(deleted);await Click("RecordRestore");Check("restore-refresh",Model.Database.Records().Any(r=>r.Id==edited.Id)&&TodayAnalysis().DurationMs==before+30000,"恢复入口与统计刷新");var current=Model.Database.Records().Single(r=>r.Id==edited.Id);await Model.RunAsync(()=>Model.Database.DeleteRecord(current.Id,current.Version),"清理本轮样板验证记录");
  var oldReflection=Model.Reflection;Model.Reflection="视觉验证：明天先复习定积分。";await Click("ReflectionSave");Check("chinese-persistence",Model.Database.Reflection(Model.LocalToday())==Model.Reflection,"中文属性绑定与SQLite持久化；非物理IME测试");Model.Reflection=oldReflection;await Model.SaveReflectionAsync();
  Build();await Click("TimerPrimary");await Until(()=>Model.TimerRunning);var timerId=Model.Timer.SessionId;await Task.Delay(1200);await Model.TickAsync();await Click("TimerPrimary");await Until(()=>Model.Timer.State==TimerState.Paused);var paused=Model.Timer.DurationMs;Check("timer-positive",paused>0,"真实Windows连续时间来源计算");await capture("A-timer-paused");await Task.Delay(600);await Model.TickAsync();Check("pause-excluded",Model.Timer.DurationMs==paused,"暂停等待不增加有效时长");
  await Click("TimerPrimary");await Until(()=>Model.TimerRunning);Check("resume-identity",Model.Timer.SessionId==timerId,"继续保留同一次学习身份");await Task.Delay(150);await Model.MarkUncertainAsync("模拟电源/时间事件：仅验证界面联动，未改变系统。");await Task.Delay(100);Check("review-entry-live",Elements().OfType<Button>().Any(b=>AutomationProperties.GetName(b)=="仅保留已确认时间"),"运行中异常即时出现核对入口");var confirmed=Model.Timer.DurationMs;await Click("仅保留已确认时间");Check("review-no-auto-time",Model.Timer.State==TimerState.Paused&&Model.Timer.DurationMs==confirmed,"核对不自动确认未知时间");await Click("TimerFinish");var saved=Model.Database.Records().Single(r=>r.Id==timerId);Check("timer-saved",saved.DurationMs>=paused&&saved.Parts.All(p=>p.Kind==PartKind.Interval),"结束保存与真实片段");await Model.RunAsync(()=>Model.Database.DeleteRecord(saved.Id,saved.Version),"清理本轮计时验证记录");
  _stats=true;_subject="math";Model.StatsSubjectId="math";Build();UpdateLayout();Check("subject-filter",Model.Analysis!.Subjects.All(s=>s.SubjectId=="math"),"日期与科目筛选联动");var chart=_charts.First();Key(chart,System.Windows.Input.Key.Home);Check("keyboard-exact-tooltip",chart.ToolTip is ToolTip tip&&tip.IsOpen&&tip.Content is string label&&label.Contains(Model.StatsFrom.ToString("yyyy-MM-dd")),"键盘主动打开精确数值提示；非物理键盘");Key(chart,System.Windows.Input.Key.Enter);Check("keyboard-drill",_lastDrill.Date==DateOnly.FromDateTime(Model.StatsFrom)&&_lastDrill.Subject=="math","Home/Enter经真实WPF路由事件下钻，保留科目筛选；非物理键盘");
  Build();await capture("A-statistics-math-filter");_subject=null;Model.StatsSubjectId="";_stats=false;Build();Check("source-invariance",TodayAnalysis().DurationMs==before,"两套布局和所有验证结束后原示例聚合值不变");
  var reloaded=new MainViewModel(_directory);await reloaded.InitializeAsync();Check("reopen-persistence",reloaded.Database.Records().Sum(r=>r.DurationMs)==Model.Database.Records().Sum(r=>r.DurationMs)&&reloaded.Database.Reflection(Model.LocalToday())==oldReflection,"新实例重新读取数据库与感想");
  await Click("QuickManual");await capture("A-manual-light");Build();_dark=true;Build();ShowManual();await capture("A-manual-dark");Build();
  var today=Model.LocalToday();var single=new StudyRecord("chart-edge","math",null,SessionSource.Manual,"仅图表边界夹具，不写数据库",[TimeParts.Duration(today,TimeSpan.FromSeconds(27.125),"Asia/Shanghai")]);var one=StudyAnalytics.Build([single],Model.AllSubjects,today,today,today);Overlay("图表边界 · 单点 / 27.125秒",Ui.Stack(Chart(one,"Trend",160),Chart(one,"Donut",160)),500);await capture("chart-single-point");Check("single-point-precision",one.DurationMs==27125,"少于一分钟，数据保持27125毫秒");Build();
  var extreme=single with{Parts=[new("extreme",today,15_432_100_987,"Asia/Shanghai",PartKind.DurationOnly)]};var big=StudyAnalytics.Build([extreme],Model.AllSubjects,today,today,today);Overlay("图表边界 · 极端时长 / 实际刻度",Chart(big,"Daily",240),500);await capture("chart-extreme-duration");Check("extreme-precision",big.DurationMs==15_432_100_987,"只绘图不落库；不归一化实际时长");Build();_dark=false;_includeDeleted=false;
  foreach(var d in new[]{"A","B"})foreach(var dark in new[]{false,true}){_direction=d;_dark=dark;_stats=false;Build();UpdateLayout();var ink=((SolidColorBrush)Ui.Brush("LabInk")).Color;var background=((SolidColorBrush)Ui.Brush("LabCanvas")).Color;double L(Color c){double V(byte v){var s=v/255d;return s<=.04045?s/12.92:Math.Pow((s+.055)/1.055,2.4);}return .2126*V(c.R)+.7152*V(c.G)+.0722*V(c.B);}var ratio=(Math.Max(L(ink),L(background))+.05)/(Math.Min(L(ink),L(background))+.05);Check("contrast-"+d+"-"+(dark?"dark":"light"),ratio>=7,$"主文字与画布对比度 {ratio:0.00}:1");}
 }
}
