using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using YanJi.App;
using YanJi.Core;
namespace YanJi.DesignLab;
public sealed partial class LabWindow : Window
{
 public MainViewModel Model {get;}
 private readonly string _directory;private readonly string? _captureDirectory;private readonly bool _empty;
 private readonly Grid _root=new();private readonly DispatcherTimer _clock=new(){Interval=TimeSpan.FromSeconds(1)};
 private string _direction="A";private bool _dark;private bool _stats;private bool _ready;private bool _closed;private bool _closing;private bool _small;
 private TextBlock? _status;private readonly List<LabChart> _charts=[];private string _range="month";private string? _subject;private bool _includeDeleted;private bool _reviewPending;private readonly Dictionary<UIElement,IInputElement?> _focusOrigins=[];private (DateOnly? Date,string? Subject) _lastDrill;
 public LabWindow(string data,string? captures,bool empty)
 {
  _directory=data;_captureDirectory=captures;_empty=empty;Model=new(data);DemoFixture.Seed(Model.Database,empty);
  Title="研迹 · V0.2 视觉样板";Width=1200;Height=800;MinWidth=900;MinHeight=600;WindowStartupLocation=WindowStartupLocation.CenterScreen;Content=_root;
  FontFamily=new FontFamily("Segoe UI Variable Text, Microsoft YaHei UI");FontSize=13;UseLayoutRounding=true;SnapsToDevicePixels=true;
  Model.PropertyChanged+=(_,eventArgs)=>{if(eventArgs.PropertyName==nameof(MainViewModel.NeedsReview)&&Model.NeedsReview&&_ready)_=Dispatcher.InvokeAsync(()=>{if(_root.Children.Count>1){_reviewPending=true;if(_status!=null)_status.Text="计时待核对 · 关闭当前面板后核对；未知时间未计入";}else Build();});};
  Model.Error+=message=>{if(_status!=null)_status.Text=message;};
  Loaded+=async(_,_)=>{await Model.InitializeAsync();Model.StatsRange("month");Model.RefreshStats();_ready=true;_small=ActualWidth<1050;Build();File.WriteAllLines(Path.Combine(data,"window.id"),[new WindowInteropHelper(this).Handle.ToInt64().ToString(),Environment.ProcessId.ToString()]);SystemEvents.PowerModeChanged+=PowerChanged;SystemEvents.TimeChanged+=TimeChanged;_clock.Tick+=async(_,_)=>{await Model.TickAsync();if(_status!=null)_status.Text=Model.Status;};_clock.Start();if(captures!=null)await RunVisualChecksAsync(captures);};
  SizeChanged+=(_,_)=>{if(!_ready)return;var small=ActualWidth<1050;if(small!=_small){_small=small;Build();}};
  Closing+=async(_,e)=>{if(_closed)return;e.Cancel=true;if(_closing)return;_closing=true;try{if(await Model.SaveReflectionAsync()&&await Model.PrepareExitAsync(false)){_closed=true;_clock.Stop();SystemEvents.PowerModeChanged-=PowerChanged;SystemEvents.TimeChanged-=TimeChanged;_=Dispatcher.BeginInvoke(new Action(Close));}}finally{_closing=false;}};
  PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Escape&&_root.Children.Count>1){CloseOverlay();e.Handled=true;}if(e.Key==Key.D&&Keyboard.Modifiers==ModifierKeys.Control){if(_root.Children.Count==1)SwitchTheme();e.Handled=true;}};
 }
 private void PowerChanged(object sender,PowerModeChangedEventArgs e){if(e.Mode is PowerModes.Suspend or PowerModes.Resume)Dispatcher.BeginInvoke(async()=>await Model.MarkUncertainAsync("视觉样板收到电源事件；未知时间待核对。"));}
 private void TimeChanged(object? sender,EventArgs e)=>Dispatcher.BeginInvoke(async()=>await Model.MarkUncertainAsync("系统时间变化；请核对计时。"));
 private async void SwitchTheme(){if(!await Model.SaveReflectionAsync())return;_dark=!_dark;await Model.RunAsync(()=>Model.Database.SaveSetting("theme",_dark?"dark":"light"),"样板主题已切换");Build();}
 private async void Navigate(bool stats){if(!await Model.SaveReflectionAsync())return;_stats=stats;Build();}
 private async void SwitchDirection(string direction){if(!await Model.SaveReflectionAsync())return;_direction=direction;Build();}
 private void Build()
 {
  if(!_ready)return;ThemeService.Apply(_dark);_focusOrigins.Clear();_reviewPending=false;LabTheme.Apply(_direction,_dark);Ui.Color(this,BackgroundProperty,"LabCanvas");Ui.Color(_root,Panel.BackgroundProperty,"LabCanvas");SetNativeTheme();_charts.Clear();_root.Children.Clear();
  var shell=new Grid();shell.RowDefinitions.Add(new(){Height=new GridLength(43)});shell.RowDefinitions.Add(new(){Height=new GridLength(1,GridUnitType.Star)});shell.RowDefinitions.Add(new(){Height=new GridLength(28)});
  var comparison=Ui.Header((_direction=="A"?Ui.Text("视觉探索  /  仅使用独立示例数据",11,"LabMuted"):(UIElement)Ui.Row(Ui.Text("YANJI · 示例数据",11,"LabMuted"),Ui.Button("今日学习",()=>Navigate(false),!_stats?"LabPill":"LabButton"),Ui.Button("统计与复盘",()=>Navigate(true),_stats?"LabPill":"LabButton"))),Ui.Row(Ui.Button("A  静桌",()=>SwitchDirection("A"),_direction=="A"?"LabPill":"LabButton","DirectionA"),Ui.Button("B  工作台",()=>SwitchDirection("B"),_direction=="B"?"LabPill":"LabButton","DirectionB"),Ui.Button(_dark?"浅色":"深色",SwitchTheme,"LabButton","ThemeToggle")));comparison.Margin=new Thickness(20,0,16,0);shell.Children.Add(comparison);
  var main=new Grid();main.ColumnDefinitions.Add(new(){Width=new GridLength(_direction=="B"||_small?64:176)});main.ColumnDefinitions.Add(new(){Width=new GridLength(1,GridUnitType.Star)});main.Children.Add(Navigation());
  var scroll=new ScrollViewer{VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled,Padding=new Thickness(_small?24:32,24,_small?24:32,24)};AutomationProperties.SetAutomationId(scroll,"LabContent");var page=(FrameworkElement)(_stats?Statistics():Today());page.MaxWidth=1300;page.HorizontalAlignment=HorizontalAlignment.Stretch;scroll.Content=page;Grid.SetColumn(scroll,1);main.Children.Add(scroll);Grid.SetRow(main,1);shell.Children.Add(main);
  _status=Ui.Text("设计样板 · 本地示例数据 · 不连接正式数据库",10,"LabMuted");_status.Margin=new Thickness(20,0,0,0);Grid.SetRow(_status,2);shell.Children.Add(_status);_root.Children.Add(shell);if(_captureDirectory==null&&SystemParameters.ClientAreaAnimation)shell.BeginAnimation(OpacityProperty,new System.Windows.Media.Animation.DoubleAnimation(.92,1,TimeSpan.FromMilliseconds(120)));
 }
 private UIElement Navigation()
 {
  var compact=_direction=="B"||_small;var dock=new DockPanel{LastChildFill=true,Margin=new Thickness(0,0,0,0)};Ui.Color(dock,Panel.BackgroundProperty,"LabRail");var brand=Ui.Stack(Ui.Text(compact?"迹":"研迹",compact?25:27,"LabAccent",true));brand.Margin=new Thickness(compact?18:24,27,0,30);if(!compact)brand.Children.Add(Ui.Text("YANJI  /  2028",10,"LabMuted"));DockPanel.SetDock(brand,Dock.Top);dock.Children.Add(brand);
  var footer=Ui.Stack(Ui.Text(compact?"V.02":"离线 · 2028 年入学",10,"LabMuted"));footer.Margin=new Thickness(compact?15:24,0,0,20);DockPanel.SetDock(footer,Dock.Bottom);dock.Children.Add(footer);
  var nav=new StackPanel();string[] names={"今日学习","备考历程","计划与进度","薄弱知识点","统计与复盘","设置与数据","学习记录"};string[] icons={"today","calendar","plan","weak","stats","settings","records"};
  for(int i=0;i<names.Length;i++){var index=i;var active=i==(_stats?4:0);var content=Ui.Row(Ui.Icon(icons[i]));if(!compact){var t=Ui.Text(names[i],13,active?"LabAccent":"LabMuted",active);t.Margin=new Thickness(12,0,0,0);content.Children.Add(t);}var b=Ui.Button(names[i],()=>{if(index==0)Navigate(false);else if(index==4)Navigate(true);else if(index==6)ShowRecords(null,null);},active?"LabPill":"LabButton");b.Content=content;b.HorizontalContentAlignment=HorizontalAlignment.Left;b.Margin=new Thickness(compact?10:12,0,compact?10:12,7);b.Padding=new Thickness(compact?12:13,10,compact?12:13,10);b.MinHeight=44;b.ToolTip=index==0||index==4||index==6?names[i]:names[i]+" · 选定视觉方向后推广";b.IsEnabled=index==0||index==4||index==6;nav.Children.Add(b);}dock.Children.Add(nav);return dock;
 }
 private TextBlock ExactMetric(long ms,double size=38){var t=Ui.Number(Ui.Short(ms),size);t.ToolTip=DurationText.Format(ms)+" · "+DurationText.Exact(ms);return t;}
 private AnalyticsSnapshot TodayAnalysis()=>StudyAnalytics.Build(Model.Database.Records(),Model.AllSubjects,Model.LocalToday(),Model.LocalToday(),Model.LocalToday());
 private StackPanel PageHeader(string title,string subtitle){var s=Ui.Stack(Ui.Text(title,_small?22:_direction=="A"?27:25,"LabInk",true),Ui.Text(subtitle,12,"LabMuted"));s.Children[1].SetValue(MarginProperty,new Thickness(0,8,0,0));return s;}
 private UIElement Today()
 {
  var a=_direction=="A";var data=TodayAnalysis();var page=new StackPanel();var title=PageHeader(a?"留一点时间，专注当下。":"今日工作台",Model.LocalToday().ToDateTime(TimeOnly.MinValue).ToString("M月d日 · dddd",CultureInfo.GetCultureInfo("zh-CN")));var actions=Ui.Row(Ui.Button("查看记录",()=>ShowRecords(Model.LocalToday(),null)),Ui.Button("＋ 补录",ShowManual,"LabPill","QuickManual"));page.Children.Add(Ui.Header(title,actions));
  var context=Ui.Header(Ui.Row(Ui.Text("今日已记录",12,"LabMuted"),new TextBlock{Width=12},ExactMetric(data.DurationMs,a?30:28)),Ui.Text(data.Sessions+" 次学习 · 暂停与未知时间不计入",11,"LabMuted"));context.Margin=new Thickness(0,_small?16:24,0,_small?16:a?24:20);page.Children.Add(context);
  var focus=FocusPanel();var ledger=Ledger(data);
  if(a){var left=Ui.Stack(focus,ReflectionPanel());var right=Ui.Stack(ledger,WeekPanel());page.Children.Add(Ui.Columns(left,right,1.3,1,28));}
  else{var left=Ui.Stack(focus,WeekPanel());var right=Ui.Stack(ledger,ReflectionPanel());page.Children.Add(Ui.Columns(left,right,1,1.65,32));}
  return page;
 }
 private UIElement FocusPanel()
 {
  var a=_direction=="A";var panel=new StackPanel();panel.Children.Add(Ui.Header(Ui.Text(a?"这一段学习":"专注计时",16,"LabInk",true),Ui.Text("本地保存",10,"LabMuted")));
  var clock=Ui.Number(Model.TimerText,_small?40:a?50:44,"LabAccent");Ui.Bind(clock,TextBlock.TextProperty,"TimerText",Model);clock.Margin=new Thickness(0,_small?10:18,0,4);AutomationProperties.SetAutomationId(clock,"TimerValue");panel.Children.Add(clock);
  var state=Ui.Text(Model.TimerStateText,11,"LabMuted");Ui.Bind(state,TextBlock.TextProperty,"TimerStateText",Model);state.TextWrapping=TextWrapping.Wrap;panel.Children.Add(state);
  var subject=new ComboBox{Style=(Style)Application.Current.Resources["LabCombo"],ItemsSource=Model.TimerSubjects,DisplayMemberPath="Name",Margin=new Thickness(0,22,0,8),IsEnabled=!Model.TimerActive};Ui.Bind(subject,ComboBox.SelectedItemProperty,"TimerSubject",Model,BindingMode.TwoWay);AutomationProperties.SetName(subject,"计时科目");AutomationProperties.SetAutomationId(subject,"TimerSubject");panel.Children.Add(subject);
  var chapter=new ComboBox{Style=(Style)Application.Current.Resources["LabCombo"],ItemsSource=Model.TimerChapters,DisplayMemberPath="Name",Margin=new Thickness(0,0,0,16),IsEnabled=!Model.TimerActive};Ui.Bind(chapter,ComboBox.SelectedItemProperty,"TimerChapter",Model,BindingMode.TwoWay);AutomationProperties.SetName(chapter,"计时章节");panel.Children.Add(chapter);
  var controls=Ui.Row();var primary=Ui.Button(Model.TimerActive?(Model.TimerRunning?"暂停":"继续学习"):"开始学习",async()=>{if(Model.TimerActive)await Model.PauseResumeCommand.ExecuteAsync(null);else await Model.StartCommand.ExecuteAsync(null);Build();},"LabPrimary","TimerPrimary");primary.IsEnabled=!Model.NeedsReview;primary.MinWidth=108;controls.Children.Add(primary);
  if(Model.TimerActive){controls.Children.Add(Ui.Button("结束保存",async()=>{await Model.FinishCommand.ExecuteAsync(null);Build();},"LabButton","TimerFinish"));}else{var helper=Ui.Text("随时开始，随时休息",11,"LabMuted");helper.Margin=new Thickness(14,0,0,0);controls.Children.Add(helper);}panel.Children.Add(controls);
  if(Model.NeedsReview){var review=Ui.Text(Model.ReviewReason+"\n"+Model.ReviewEvidence,11,"LabDanger");review.Margin=new Thickness(0,12,0,0);review.TextWrapping=TextWrapping.Wrap;panel.Children.Add(review);panel.Children.Add(Ui.Button("仅保留已确认时间",async()=>{await Model.ResolveAsync([]);Build();},"LabPill"));}
  return a?Ui.Surface(panel,padding:_small?16:24):panel;
 }
 private UIElement Ledger(AnalyticsSnapshot today)
 {
  var a=_direction=="A";var list=new StackPanel();list.Children.Add(Ui.Header(Ui.Text(a?"今日的学习痕迹":"学习日志",16,"LabInk",true),Ui.Text(today.Sessions+" 条",11,"LabMuted")));list.Children.Add(Ui.Line(14));
  var records=Model.Database.Records(Model.LocalToday()).OrderByDescending(r=>r.Parts.Any(p=>p.Kind==PartKind.Interval)).ThenBy(r=>r.CreatedAt).Take(a?4:5).ToList();
  if(records.Count==0){var empty=Ui.Text("还没有记录。\n完成一段学习后，回顾会出现在这里。",13,"LabMuted");empty.TextWrapping=TextWrapping.Wrap;empty.Margin=new Thickness(0,12,0,18);list.Children.Add(empty);}
  foreach(var r in records){var name=Model.AllSubjects.FirstOrDefault(s=>s.Id==r.SubjectId)?.Name??r.SubjectId;var chapter=Model.AllChapters.FirstOrDefault(c=>c.Id==r.ChapterId)?.Name??"未指定章节";var amount=r.Parts.Where(p=>p.Date==Model.LocalToday()).Sum(p=>p.DurationMs);var stripe=new Border{Width=3,CornerRadius=new CornerRadius(2),Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(Ui.SubjectColor(r.SubjectId,_dark))),Margin=new Thickness(0,4,12,4)};
   var text=Ui.Stack(Ui.Header(Ui.Text(name,14,"LabInk",true),ExactMetric(amount,15)),Ui.Text(chapter,11,"LabMuted"));text.Children[1].SetValue(MarginProperty,new Thickness(0,6,0,0));var note=Ui.Text(r.Note,12,"LabMuted");note.TextTrimming=TextTrimming.CharacterEllipsis;note.Margin=new Thickness(0,5,0,0);text.Children.Add(note);
   var dock=new DockPanel{LastChildFill=true};DockPanel.SetDock(stripe,Dock.Left);dock.Children.Add(stripe);dock.Children.Add(text);var b=Ui.Button(name,()=>ShowRecord(r));b.Content=dock;b.HorizontalContentAlignment=HorizontalAlignment.Stretch;b.Padding=new Thickness(0,9,0,12);b.ToolTip=name+" · "+DurationText.Format(amount)+"\n"+(r.Parts.Any(p=>p.Kind==PartKind.Interval)?"含真实时间区间":"手动补录 · 未提供时间位置")+"\n点击查看片段与修订历史";AutomationProperties.SetName(b,name+" 学习记录详情");list.Children.Add(b);list.Children.Add(Ui.Line(0));}
  var all=Ui.Button("查看全部记录  →",()=>ShowRecords(Model.LocalToday(),null));all.HorizontalAlignment=HorizontalAlignment.Left;all.Padding=new Thickness(0,10,0,0);list.Children.Add(all);return list;
 }
 private UIElement ReflectionPanel()
 {
  var panel=new StackPanel{Margin=new Thickness(0,24,0,0)};panel.Children.Add(Ui.Header(Ui.Text("留给明天的一句话",15,"LabInk",true),Ui.Button("保存",async()=>{await Model.SaveReflectionAsync();if(_status!=null)_status.Text=Model.Status;},"LabButton","ReflectionSave")));
  var text=new TextBox{AcceptsReturn=true,TextWrapping=TextWrapping.Wrap,Height=_direction=="A"?80:76,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,Margin=new Thickness(0,9,0,0)};Ui.Bind(text,TextBox.TextProperty,"Reflection",Model,BindingMode.TwoWay);AutomationProperties.SetName(text,"每日感想");AutomationProperties.SetAutomationId(text,"ReflectionEditor");panel.Children.Add(text);return panel;
 }
 private UIElement WeekPanel()
 {
  var today=Model.LocalToday();var data=StudyAnalytics.Build(Model.Database.Records(),Model.AllSubjects,today.AddDays(-6),today,today);var panel=new StackPanel{Margin=new Thickness(0,22,0,0)};panel.Children.Add(Ui.Header(Ui.Text("最近 7 天",12,"LabMuted"),ExactMetric(data.DurationMs,16)));panel.Children.Add(Chart(data,"Trend",94));panel.Children.Add(Ui.Text("每一天都可以重新开始",10,"LabMuted"));return panel;
 }
 private LabChart Chart(AnalyticsSnapshot data,string mode,double height){var c=new LabChart{Data=data,Mode=mode,Dark=_dark,PreviewMode=_captureDirectory!=null,Height=height,Margin=new Thickness(0,14,0,0),Drill=(date,subject)=>ShowRecords(date,subject??(_stats?_subject:null))};AutomationProperties.SetName(c,mode=="Donut"?"科目投入图":"每日实际学习时长图");_charts.Add(c);return c;}
 private UIElement Statistics()
 {
  var a=_direction=="A";var data=Model.Analysis!;var page=new StackPanel();var range=Ui.Row();foreach(var item in new[]{("day","今天"),("week","本周"),("month","本月"),("30","近30日")}){var value=item.Item1;var b=Ui.Button(item.Item2,()=>{_range=value;Model.StatsRange(value);Model.RefreshStats();Build();},_range==value?"LabPill":"LabButton");range.Children.Add(b);}page.Children.Add(Ui.Header(PageHeader(a?"看见投入，也看见节奏。":"投入分析",$"{Model.StatsFrom:yyyy.MM.dd} — {Model.StatsTo:MM.dd} · 有效学习片段"),range));
  var totals=Ui.Row();var primary=Ui.Stack(Ui.Text("范围总投入",11,"LabMuted"),ExactMetric(data.DurationMs,_small?30:38));primary.ToolTip=DurationText.Exact(data.DurationMs);totals.Children.Add(primary);
  foreach(var item in new[]{("自然日日均",Ui.Short((long)data.CalendarAverageMs),$"截至今天的 {data.CalendarDays} 个自然日"),("学习日日均",Ui.Short((long)data.LearningAverageMs),$"{data.LearningDays} 个有记录的学习日"),("学习次数",data.Sessions.ToString(),"按整次学习去重")}){var cell=Ui.Stack(Ui.Text(item.Item1,11,"LabMuted"),Ui.Text(item.Item2,_small?16:19,"LabInk",true));cell.Margin=new Thickness(_small?22:38,0,0,0);cell.ToolTip=item.Item3;totals.Children.Add(cell);}totals.Margin=new Thickness(0,20,0,20);page.Children.Add(totals);
  var subjects=new ComboBox{Style=(Style)Application.Current.Resources["LabCombo"],ItemsSource=Model.SubjectFilters,DisplayMemberPath="Label",SelectedValuePath="Value",SelectedValue=_subject??"",Width=144};subjects.SelectionChanged+=(_,_)=>{var id=subjects.SelectedValue as string;if(id==(_subject??""))return;_subject=string.IsNullOrEmpty(id)?null:id;Model.StatsSubjectId=_subject??"";Build();};AutomationProperties.SetName(subjects,"统计科目筛选");
  var graph=Ui.Stack(Ui.Header(Ui.Text(a?"每日科目投入":"学习节奏",16,"LabInk",true),subjects),Chart(data,a?"Daily":"Trend",_small?164:a?194:180));
  if(a){var breakdown=Ui.Stack(Ui.Text("科目分布",16,"LabInk",true),Chart(data,"Donut",154),SubjectList(data,false));page.Children.Add(Ui.Columns(Ui.Surface(graph,padding:22),breakdown,2.15,1,28));}
  else{page.Children.Add(Ui.Surface(graph,padding:22,radius:14));var lower=Ui.Columns(Ui.Stack(Ui.Text("科目投入",15,"LabInk",true),SubjectList(data,true)),Ui.Stack(Ui.Header(Ui.Text("原始记录",15,"LabInk",true),Ui.Button("全部 →",()=>ShowRecords(null,_subject))),RecordPreview()),1,1.3,32);lower.Margin=new Thickness(0,22,0,0);page.Children.Add(lower);}
  if(a){var heat=Ui.Stack(Ui.Header(Ui.Text("学习日历",12,"LabMuted"),Ui.Button("查看原始记录 →",()=>ShowRecords(null,_subject))),Chart(data,"Heat",76));heat.Margin=new Thickness(0,20,0,0);page.Children.Add(heat);}var caption=Ui.Text("纵轴为实际时长 · 悬停查看精确值 · 点击或 Enter 查看对应记录",10,"LabMuted");caption.Margin=new Thickness(0,12,0,0);page.Children.Add(caption);return page;
 }
 private UIElement SubjectList(AnalyticsSnapshot data,bool bars)
 {
  var list=new StackPanel{Margin=new Thickness(0,12,0,0)};foreach(var s in data.Subjects){var dot=new Border{Width=7,Height=7,CornerRadius=new CornerRadius(4),Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(Ui.SubjectColor(s.SubjectId,_dark))),Margin=new Thickness(0,0,9,0)};var name=Ui.Row(dot,Ui.Text(s.Name,12));var content=Ui.Stack(Ui.Header(name,Ui.Text(Ui.Short(s.DurationMs),12,"LabInk",true)));if(bars){var g=new Grid{Height=4,Margin=new Thickness(0,8,0,0)};g.Children.Add(Ui.Surface(new Grid(),"LabLine",0,2));var bar=new Border{Background=dot.Background,HorizontalAlignment=HorizontalAlignment.Left,CornerRadius=new CornerRadius(2)};g.SizeChanged+=(_,_)=>bar.Width=Math.Max(1,g.ActualWidth*s.DurationMs/Math.Max(1,data.Subjects.Max(x=>x.DurationMs)));g.Children.Add(bar);content.Children.Add(g);}var b=Ui.Button(s.Name,()=>ShowRecords(null,s.SubjectId));b.Content=content;b.Padding=new Thickness(0,7,0,7);b.HorizontalContentAlignment=HorizontalAlignment.Stretch;b.ToolTip=DurationText.Format(s.DurationMs)+$" · {s.DurationMs*100d/Math.Max(1,data.DurationMs):0.0}%";list.Children.Add(b);}if(data.Subjects.Count==0)list.Children.Add(Ui.Text("有记录后显示科目投入",12,"LabMuted"));return list;
 }
 private UIElement RecordPreview()
 {
  var list=new StackPanel();foreach(var r in ScopedRecords(null,_subject).Take(3)){var name=Model.AllSubjects.FirstOrDefault(s=>s.Id==r.SubjectId)?.Name??r.SubjectId;var b=Ui.Button(name,()=>ShowRecord(r));b.Content=Ui.Header(Ui.Text(r.Parts.Min(p=>p.Date).ToString("MM.dd")+"  "+name,12),Ui.Text(Ui.Short(r.Parts.Where(p=>p.Date>=DateOnly.FromDateTime(Model.StatsFrom)&&p.Date<=DateOnly.FromDateTime(Model.StatsTo)).Sum(p=>p.DurationMs)),12,"LabMuted"));b.HorizontalContentAlignment=HorizontalAlignment.Stretch;b.Padding=new Thickness(0,8,0,8);list.Children.Add(b);list.Children.Add(Ui.Line(0));}return list;
 }
 private List<StudyRecord> ScopedRecords(DateOnly? date,string? subject,bool includeDeleted=false)=>Model.Database.Records(date,includeDeleted).Where(r=>(subject==null||r.SubjectId==subject)&&(date!=null||r.Parts.Any(p=>p.Date>=DateOnly.FromDateTime(Model.StatsFrom)&&p.Date<=DateOnly.FromDateTime(Model.StatsTo)))).OrderByDescending(r=>r.Parts.Max(p=>p.Date)).ToList();
 private void Overlay(string title,UIElement body,double width=500)
 {
  var prior=Keyboard.FocusedElement;foreach(UIElement lower in _root.Children)lower.IsEnabled=false;var shade=new Grid{Focusable=true,Background=new SolidColorBrush(Color.FromArgb(100,8,14,12))};var content=Ui.Stack(Ui.Header(Ui.Text(title,20,"LabInk",true),Ui.Button("关闭",CloseOverlay)),Ui.Line(16),body);var surface=Ui.Surface(new ScrollViewer{Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,MaxHeight=Math.Max(300,ActualHeight-130)},padding:24,radius:16);surface.Width=Math.Min(width,ActualWidth-100);surface.HorizontalAlignment=HorizontalAlignment.Right;surface.VerticalAlignment=VerticalAlignment.Stretch;surface.Margin=new Thickness(0,60,24,24);shade.Children.Add(surface);_focusOrigins[shade]=prior;KeyboardNavigation.SetTabNavigation(shade,KeyboardNavigationMode.Cycle);KeyboardNavigation.SetDirectionalNavigation(shade,KeyboardNavigationMode.Contained);_root.Children.Add(shade);_=Dispatcher.InvokeAsync(()=>{UpdateLayout();var first=Elements(shade).OfType<Control>().FirstOrDefault(control=>control.Focusable&&control.IsEnabled);if(first!=null)first.Focus();else shade.Focus();});
 }
 private void ShowManual()
 {
  var form=new StackPanel();var subjects=new ComboBox{Style=(Style)Application.Current.Resources["LabCombo"],ItemsSource=Model.Subjects,DisplayMemberPath="Name"};Ui.Bind(subjects,ComboBox.SelectedItemProperty,"ManualSubject",Model,BindingMode.TwoWay);var minutes=new TextBox();Ui.Bind(minutes,TextBox.TextProperty,"ManualMinutes",Model,BindingMode.TwoWay);AutomationProperties.SetName(minutes,"补录分钟数");AutomationProperties.SetAutomationId(minutes,"ManualMinutes");var date=new DatePicker{SelectedDate=Model.ManualDate,Margin=new Thickness(0,6,0,16)};Ui.Bind(date,DatePicker.SelectedDateProperty,"ManualDate",Model,BindingMode.TwoWay);var note=new TextBox{AcceptsReturn=true,Height=66,TextWrapping=TextWrapping.Wrap};Ui.Bind(note,TextBox.TextProperty,"ManualNote",Model,BindingMode.TwoWay);
  foreach(var item in new[]{("学习日期",(UIElement)date),("科目",subjects),("分钟数",minutes),("备注 · 可选",note)}){form.Children.Add(Ui.Text(item.Item1,12,"LabMuted"));item.Item2.SetValue(MarginProperty,new Thickness(0,6,0,14));form.Children.Add(item.Item2);}var warning=Ui.Text(Model.ManualWarning,12,"LabMuted");Ui.Bind(warning,TextBlock.TextProperty,"ManualWarning",Model);warning.TextWrapping=TextWrapping.Wrap;form.Children.Add(warning);form.Children.Add(Ui.Button("追加学习记录",async()=>{await Model.AddManualCommand.ExecuteAsync(null);if(Model.Status.StartsWith("补录已追加"))Build();else warning.Text=Model.Status;},"LabPrimary","ManualSave"));Overlay("快速补录",form,440);
 }
 private void CloseOverlay()
 {
  if(_root.Children.Count<=1)return;var top=_root.Children[_root.Children.Count-1];var previous=_focusOrigins.GetValueOrDefault(top);_focusOrigins.Remove(top);_root.Children.Remove(top);var active=_root.Children[_root.Children.Count-1];active.IsEnabled=true;
  if(_root.Children.Count==1&&_reviewPending){Build();return;}
  if(previous is UIElement target&&target.IsVisible&&target.IsEnabled)target.Focus();else Elements(active).OfType<Control>().FirstOrDefault(control=>control.Focusable&&control.IsEnabled)?.Focus();
 }
 private void ShowRecords(DateOnly? date,string? subject)
 {
  _lastDrill=(date,subject);var list=new StackPanel();list.Children.Add(Ui.Button(_includeDeleted?"隐藏已删除记录":"包含已删除记录",()=>{_includeDeleted=!_includeDeleted;ShowRecords(date,subject);},"LabPill"));list.Children.Add(Ui.Text(date!=null?date.Value.ToString("yyyy-MM-dd"):Model.StatsFrom.ToString("yyyy-MM-dd")+" — "+Model.StatsTo.ToString("yyyy-MM-dd"),12,"LabMuted"));foreach(var r in ScopedRecords(date,subject,_includeDeleted)){var name=Model.AllSubjects.FirstOrDefault(s=>s.Id==r.SubjectId)?.Name??r.SubjectId;var contribution=r.Parts.Where(p=>date!=null?p.Date==date:p.Date>=DateOnly.FromDateTime(Model.StatsFrom)&&p.Date<=DateOnly.FromDateTime(Model.StatsTo)).Sum(p=>p.DurationMs);var b=Ui.Button(name,()=>ShowRecord(r));b.Content=Ui.Stack(Ui.Header(Ui.Text(name+(r.Deleted?" · 已删除":""),14,r.Deleted?"LabMuted":"LabInk",true),Ui.Text(Ui.Short(contribution),14)),Ui.Text(r.Parts.Min(p=>p.Date).ToString("yyyy-MM-dd")+" · "+(r.Source==SessionSource.Timer?"计时":"补录"),11,"LabMuted"));b.HorizontalContentAlignment=HorizontalAlignment.Stretch;b.Padding=new Thickness(0,12,0,12);b.ToolTip="本范围贡献 "+DurationText.Format(contribution)+" · 整次 "+DurationText.Format(r.DurationMs);list.Children.Add(b);list.Children.Add(Ui.Line(0));}if(list.Children.Count==2)list.Children.Add(Ui.Text("这个范围尚无记录",14,"LabMuted"));Overlay("对应的原始学习记录",list);
 }
 private void ShowRecord(StudyRecord record)
 {
  var form=new StackPanel();var subject=Model.AllSubjects.FirstOrDefault(s=>s.Id==record.SubjectId)?.Name??record.SubjectId;form.Children.Add(Ui.Text(subject+" · "+DurationText.Format(record.DurationMs),17,"LabInk",true));form.Children.Add(Ui.Text("整次学习 · "+string.Join("、",record.Parts.Select(p=>p.Date.ToString("yyyy-MM-dd")).Distinct()),12,"LabMuted"));form.Children.Add(Ui.Line());foreach(var p in record.Parts){var label=p.Kind==PartKind.Interval?$"{TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(p.StartUtcMs!.Value),TimeParts.ResolveZone(p.ZoneId)):HH:mm:ss} — {TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeMilliseconds(p.EndUtcMs!.Value),TimeParts.ResolveZone(p.ZoneId)):HH:mm:ss}":"纯时长 · 未提供时间位置";var line=Ui.Text(p.Date+" · "+label+"\n"+DurationText.Format(p.DurationMs),12,"LabMuted");line.TextWrapping=TextWrapping.Wrap;line.Margin=new Thickness(0,0,0,12);form.Children.Add(line);}form.Children.Add(Ui.Text("备注",12,"LabMuted"));var note=new TextBox{Text=record.Note,TextWrapping=TextWrapping.Wrap,AcceptsReturn=true,Height=76,Margin=new Thickness(0,6,0,12)};AutomationProperties.SetAutomationId(note,"RecordNote");note.IsReadOnly=record.Deleted;form.Children.Add(note);if(record.Deleted){form.Children.Add(Ui.Button("恢复整次学习",async()=>{if(await Model.RunAsync(()=>Model.Database.RestoreRecord(record.Id,record.Version),"整次学习已恢复"))Build();},"LabPrimary","RecordRestore"));}else form.Children.Add(Ui.Button("保存备注修订",async()=>{var revisedNote=note.Text;if(await Model.RunAsync(()=>Model.Database.SaveRecord(record with{Note=revisedNote},"视觉样板：修改备注"),"备注修订已保存"))Build();},"LabPrimary"));form.Children.Add(Ui.Button("查看修订历史",()=>{var history=Ui.Text(string.Join("\n\n",Model.Database.History(record.Id).Select(h=>$"{h.At.ToLocalTime():yyyy-MM-dd HH:mm:ss} · v{h.Version}\n{h.Reason}")),12,"LabMuted");history.TextWrapping=TextWrapping.Wrap;Overlay("修订历史",history);}));var delete=Ui.Button("删除整次学习…",()=>{var message=Ui.Text("将删除 "+subject+" · "+DurationText.Format(record.DurationMs)+"\n影响日期："+string.Join("、",record.Parts.Select(p=>p.Date).Distinct())+"\n这会更新统计；原记录与修订保留，可恢复。",13);message.TextWrapping=TextWrapping.Wrap;var confirm=Ui.Stack(message,Ui.Button("确认软删除",async()=>{if(await Model.RunAsync(()=>Model.Database.DeleteRecord(record.Id,record.Version),"已软删除"))Build();},"LabPill"));Overlay("确认删除范围",confirm);});delete.Content=Ui.Text("删除整次学习…",13,"LabDanger");if(!record.Deleted)form.Children.Add(delete);Overlay("学习记录详情",form);
 }
 [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr h,int attribute,ref int value,int size);
 private void SetNativeTheme(){Icon=new DrawingImage(new GeometryDrawing(Ui.Brush("LabAccent"),null,Geometry.Parse("M2,2 L7,2 L7,14 L2,14 Z M9,2 L14,2 L14,14 L9,14 Z M2,16 L14,16 L14,18 L2,18 Z")));var h=new WindowInteropHelper(this).Handle;if(h!=IntPtr.Zero){int dark=_dark?1:0;DwmSetWindowAttribute(h,20,ref dark,4);}}
 private async Task RunVisualChecksAsync(string directory)
 {
  try{if(Directory.Exists(directory))throw new InvalidOperationException("截图目录须为全新目录。");Directory.CreateDirectory(directory);var results=new List<object>();
   async Task Capture(string name){NativeCapture.Present(this);if(name=="A-today-light"){var nodes=new List<string>();void Walk(DependencyObject node,int depth){if(node is FrameworkElement element)nodes.Add(new string(' ',depth*2)+node.GetType().Name+" "+element.Name+" "+element.ActualWidth+"x"+element.ActualHeight+(node is Control control?" bg="+control.Background+" template="+control.Template:""));if(depth<12)for(int j=0;j<VisualTreeHelper.GetChildrenCount(node);j++)Walk(VisualTreeHelper.GetChild(node,j),depth+1);}Walk(_root,0);File.WriteAllLines(Path.Combine(directory,"template-diagnostic.txt"),nodes);}await Dispatcher.InvokeAsync(()=>UpdateLayout(),DispatcherPriority.Render);await Task.Delay(400);var dpi=VisualTreeHelper.GetDpi(this);var rendered=new RenderTargetBitmap((int)Math.Ceiling(_root.ActualWidth*dpi.DpiScaleX),(int)Math.Ceiling(_root.ActualHeight*dpi.DpiScaleY),dpi.PixelsPerInchX,dpi.PixelsPerInchY,PixelFormats.Pbgra32);rendered.Render(_root);var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(rendered));using(var file=File.Create(Path.Combine(directory,name+"-visual-tree.png")))encoder.Save(file);NativeCapture.Save(this,Path.Combine(directory,name+".png"));results.Add(new{Name=name,Status="CAPTURED; visual review required",Width=ActualWidth,Height=ActualHeight,Dpi=dpi.PixelsPerInchX,Method="Native PrintWindow + actual WPF visual tree; verified own HWND"});}
   foreach(var direction in new[]{"A","B"})foreach(var dark in new[]{false,true})foreach(var stats in new[]{false,true}){_direction=direction;_dark=dark;_stats=stats;Build();await Capture(direction+"-"+(stats?"statistics":"today")+"-"+(dark?"dark":"light")+(_empty?"-empty":""));}
   if(!_empty)await VerifyInteractionsAsync(directory,Capture);
   if(!_empty){Width=900;Height=600;foreach(var d in new[]{"A","B"}){_direction=d;_dark=false;_stats=false;Build();await Capture(d+"-today-900x600");_stats=true;Build();await Capture(d+"-statistics-900x600");}_direction="A";Width=1500;Height=950;_stats=false;Build();await Capture("A-today-large");_stats=true;Build();await Capture("A-statistics-large");}
   File.WriteAllText(Path.Combine(directory,"capture-results.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));File.WriteAllText(Path.Combine(directory,"environment.json"),JsonSerializer.Serialize(new{Runtime=Environment.Version.ToString(),OS=Environment.OSVersion.ToString(),ProcessId=Environment.ProcessId,WindowsClock=WindowsClock.Sources,Fonts=Fonts.SystemFontFamilies.Where(f=>f.Source.Contains("Segoe")||f.Source.Contains("YaHei")||f.Source.Contains("Consolas")).Select(f=>f.Source).ToArray(),Empty=_empty,DataDirectory=_directory,Total=Model.Analysis!.DurationMs},new JsonSerializerOptions{WriteIndented=true}));Close();
  }catch(Exception ex){File.WriteAllText(Path.Combine(_directory,"visual-error.txt"),ex.ToString());Application.Current.Shutdown(1);}
 }
}
