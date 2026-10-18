using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YanJi.Core;

namespace YanJi.App;
public partial class MainWindow
{
    private IInputElement? _detailFocus;
    internal Action<Window>? TestDialogShown {get;set;}
    private bool Confirm(string title,string message,string action)
    {var dialogOwner=Application.Current.Windows.OfType<EditorDialog>().FirstOrDefault(w=>w.Owner==this&&w.IsVisible) as Window??this;var dialog=new ConfirmDialog(dialogOwner,title,message,action);TestDialogShown?.Invoke(dialog);return dialog.ShowDialog()==true;}
    private void RefreshChartThemes(){foreach(var chart in VisualChildren(this).OfType<StudyChart>())chart.RefreshTheme();}
    private static IEnumerable<DependencyObject> VisualChildren(DependencyObject root)
    {for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);yield return child;foreach(var d in VisualChildren(child))yield return d;}}
    private DateOnly? _studioDate;
    private void UpdateStudioDate()
    {
        var day=Model.LocalToday();if(_studioDate==day)return;
        _studioDate=day;StudioDate.Text=day.ToString("MM月dd日 dddd");
    }
    private void Layout_Changed(object sender,SizeChangedEventArgs e)
    {
        if(NavColumn==null||TodayReflection==null)return;
        var narrow=ActualWidth<1050;
        NavColumn.Width=new GridLength(narrow?142:172);
        BrandWord.FontSize=24;BrandHint.Visibility=VersionHint.Visibility=Visibility.Visible;
        ExitButton.Content="退出应用";ExitButton.Padding=new Thickness(8);ExitButton.Margin=new Thickness(0);
        foreach(var text in VisualChildren(this).OfType<TextBlock>().Where(t=>Equals(t.Tag,"NavLabel")))text.Visibility=Visibility.Visible;
        foreach(var item in NavigationList.Items.OfType<ListBoxItem>())item.Padding=new Thickness(narrow?8:12,11,8,11);
        MainScroll.Padding=new Thickness(narrow?24:32,narrow?22:29,narrow?24:32,20);
        TimerDigits.FontSize=narrow?88:104;StudioTodayTitle.FontSize=narrow?32:38;
        AtelierClockBox.Height=narrow?100:120;
        AtelierFocusWidth.Margin=new Thickness(0,0,narrow?0:72,20);
        AtelierFocusPlane.Padding=new Thickness(narrow?20:26,narrow?18:22,narrow?20:26,narrow?18:22);
        AtelierClockGrid.Margin=new Thickness(0,narrow?12:16,0,narrow?12:16);
        TodayReflection.Padding=narrow?new Thickness(14,9,14,9):new Thickness(16,13,16,13);
        UpdateStudioDate();
        if(RecordList!=null)RecordList.MaxHeight=Math.Max(200,ActualHeight-300);
        var content=Math.Max(0,ActualWidth-NavColumn.Width.Value-MainScroll.Padding.Left-MainScroll.Padding.Right-24);
        TodayLeft.Width=new GridLength(1,GridUnitType.Star);TodayGap.Width=new GridLength(narrow?19:24);TodayRight.Width=new GridLength(narrow?180:210);
        Grid.SetColumn(TodayReflection,narrow?0:1);Grid.SetRow(TodayReflection,narrow?1:0);
        TodayReflection.Margin=narrow?new Thickness(0,14,0,0):new Thickness(24,8,0,0);
        AtelierHeader.Margin=new Thickness(0,2,0,narrow?14:26);
        AtelierJournal.ColumnDefinitions[1].Width=new GridLength(narrow?0:20);
        AtelierJournal.ColumnDefinitions[2].Width=new GridLength(narrow?0:1,GridUnitType.Star);
        Grid.SetColumn(AtelierNext,narrow?0:2);Grid.SetRow(AtelierNext,narrow?1:0);
        AtelierNext.Margin=new Thickness(0,narrow?16:0,0,0);
        CalendarLeft.Width=StatsLeft.Width=new GridLength(1,GridUnitType.Star);
        CalendarGap.Width=StatsGap.Width=new GridLength(narrow?16:24);
        CalendarRight.Width=new GridLength(narrow?220:285);StatsRight.Width=new GridLength(narrow?190:265);
        Grid.SetColumn(CalendarDetail,2);Grid.SetRow(CalendarDetail,0);Grid.SetColumn(StatsDailyCard,2);Grid.SetRow(StatsDailyCard,0);
        if(TimerFields!=null&&TimerChapterField!=null)
        {TimerFields.ColumnDefinitions[0].Width=new GridLength(1,GridUnitType.Star);TimerFields.ColumnDefinitions[1].Width=new GridLength(12);TimerFields.ColumnDefinitions[2].Width=new GridLength(1,GridUnitType.Star);Grid.SetColumn(TimerChapterField,2);Grid.SetRow(TimerChapterField,0);}
        if(DrawerSurface!=null)DrawerSurface.Width=Math.Min(620,Math.Max(0,ActualWidth-32));
        foreach(var pair in new[]{(AuxGrid2,AuxRight2),(AuxGrid3,AuxRight3),(AuxGrid5,AuxRight5)})
        {var stack=content<740;pair.Item1.ColumnDefinitions[0].Width=new GridLength(stack?1:1.6,GridUnitType.Star);pair.Item1.ColumnDefinitions[1].Width=new GridLength(stack?0:16);pair.Item1.ColumnDefinitions[2].Width=new GridLength(stack?0:1,GridUnitType.Star);Grid.SetColumn(pair.Item2,stack?0:2);Grid.SetRow(pair.Item2,stack?1:0);}
    }
    private async void Window_KeyDown(object sender,KeyEventArgs e)
    {
        if(e.Key==Key.Escape&&Model.DrawerOpen){CloseDrawer_Click(this,e);e.Handled=true;return;}
        if(Model.DrawerOpen||Keyboard.Modifiers!=ModifierKeys.Control)return;
        if(e.Key==Key.M){e.Handled=true;ShowMiniTimer();return;}
        if(e.Key is Key.N or Key.L){QuickAdd_Click(this,e);e.Handled=true;return;}
        if(e.OriginalSource is System.Windows.Controls.Primitives.TextBoxBase or ComboBox or ComboBoxItem||Keyboard.FocusedElement is System.Windows.Controls.Primitives.TextBoxBase or ComboBox or ComboBoxItem)return;
        if(e.Key==Key.Enter){e.Handled=true;await Model.StartCommand.ExecuteAsync(null);}else if(e.Key==Key.Space){e.Handled=true;await Model.PauseResumeCommand.ExecuteAsync(null);}
    }
    private async void QuickAdd_Click(object sender,RoutedEventArgs e)
    {
        if(Model.Subjects.Count==0){Model.Status="请先添加或恢复一个使用中的科目，再补录学习。";return;}
        var requestId=Id();
        await EditPersistAsync("快速补录 · 追加一次学习",[new("date","日期 *（yyyy-MM-dd）",Model.LocalToday().ToString("yyyy-MM-dd")),new("subject","科目 *",Model.ManualSubject?.Id??Model.Subjects.First().Id,SubjectChoices(includeArchived:false)),new("minutes","时长（分钟）*","30"),new("chapter","主要章节（可选）","",ChapterChoices(includeArchived:false)),new("task","任务（可选）","",TaskChoices()),new("note","备注（可选）","",Multiline:true)],
            "仅日期、科目、时长必填。纯时长不虚构起止位置；已有记录请使用“编辑”，不要重复追加。",v=>{Date(v["date"]);if(!double.TryParse(v["minutes"],out var m)||!double.IsFinite(m)||m<=0||m>1440)return "分钟数必须大于 0、最多 1440。";return Required(v,"date","subject","minutes")??CheckChapter(v);},commit:async v=>
        {
            Model.ManualDate=Date(v["date"])!.Value.ToDateTime(TimeOnly.MinValue);Model.ManualSubject=Model.Subjects.FirstOrDefault(s=>s.Id==v["subject"]);
        Model.ManualChapter=Model.ManualChapters.FirstOrDefault(c=>c.Id==v["chapter"]);Model.ManualMinutes=v["minutes"];Model.ManualNote=v["note"];Model.ManualTask=Model.Database.Tasks().FirstOrDefault(t=>t.Id==v["task"]);
        if(Model.ManualWarning.StartsWith("这一天")&&!Confirm("这一天已有该科目记录",Model.ManualWarning+"\n\n确认是在追加另一段学习，而非修正原记录？","仍然追加"))return false;
        return await Model.AddManualRecordAsync(requestId);
        });
    }
    private async void Metric_Click(object sender,RoutedEventArgs e)
    {if(!await Model.SaveReflectionAsync())return;var today=Model.LocalToday();var tag=((Button)sender).Tag?.ToString();var from=tag=="week"?today.AddDays(-(((int)today.DayOfWeek+6)%7)):tag=="month"?new DateOnly(today.Year,today.Month,1):today;Model.OpenRecords(from,today);MainScroll.ScrollToTop();}
    private async void TodayRecords_Click(object sender,RoutedEventArgs e){if(await Model.SaveReflectionAsync()){Model.OpenRecords(Model.LocalToday(),Model.LocalToday());MainScroll.ScrollToTop();}}
    private async void DayRecords_Click(object sender,RoutedEventArgs e){if(await Model.SaveReflectionAsync()){var date=DateOnly.FromDateTime(Model.SelectedDate);Model.OpenRecords(date,date);MainScroll.ScrollToTop();}}
    private async void StatsRecords_Click(object sender,RoutedEventArgs e){if(await Model.SaveReflectionAsync()){Model.OpenRecords(DateOnly.FromDateTime(Model.StatsFrom),DateOnly.FromDateTime(Model.StatsTo),Model.StatsSubjectId);MainScroll.ScrollToTop();}}
    private void StatsRange_Click(object sender,RoutedEventArgs e)=>Model.StatsRange(((Button)sender).Tag.ToString()!);
    private void DataSection_Click(object sender,RoutedEventArgs e)
    {
        var target=((Button)sender).Tag?.ToString() switch{"trends"=>(FrameworkElement)StatsOverviewHeading,"chapters"=>ChapterAnalysis,"review"=>StudyReviewSection,_=>LearningCalendar};
        target.BringIntoView();
    }
    private void StatsView_Click(object sender,RoutedEventArgs e){Model.StatsView=int.Parse(((Button)sender).Tag.ToString()!);}
    private void ManageChapters_Click(object sender,RoutedEventArgs e){Model.Page=2;MainScroll.ScrollToTop();}
    private void NewReview_Click(object sender,RoutedEventArgs e)=>Model.NewPeriodReview();
    private async void SaveReview_Click(object sender,RoutedEventArgs e)=>await Model.SavePeriodReviewAsync();
    private void EditReview_Click(object sender,RoutedEventArgs e){Model.EditPeriodReview((ReviewRow)((Button)sender).Tag);ReviewEditor.BringIntoView();}
    private void ReviewRecords_Click(object sender,RoutedEventArgs e){var row=(ReviewRow)((Button)sender).Tag;Model.OpenRecords(row.Review.From,row.Review.To,row.Review.SubjectId);MainScroll.ScrollToTop();}
    private async void DeleteReview_Click(object sender,RoutedEventArgs e)
    {var row=(ReviewRow)((Button)sender).Tag;if(Confirm("删除阶段复盘",row.Label+"\n可在显示已删除复盘后恢复。","软删除复盘"))await Model.RunAsync(()=>Model.Database.SetPeriodReviewDeleted(row.Review.Id,row.Review.Version,true),"复盘已软删除");}
    private async void RestoreReview_Click(object sender,RoutedEventArgs e)
    {var row=(ReviewRow)((Button)sender).Tag;await Model.RunAsync(()=>Model.Database.SetPeriodReviewDeleted(row.Review.Id,row.Review.Version,false),"复盘已恢复");}
    private async void Chart_Drill(PlotPoint point)
    {if(!await Model.SaveReflectionAsync())return;Model.OpenRecords(point.From??DateOnly.FromDateTime(Model.StatsFrom),point.To??DateOnly.FromDateTime(Model.StatsTo),point.SubjectId);MainScroll.ScrollToTop();}
    private async void Heatmap_Drill(PlotPoint point)
    {if(point.From is DateOnly date){await Model.ChangeDayAsync(date.ToDateTime(TimeOnly.MinValue));Model.Page=4;LearningCalendar.BringIntoView();}}
    private void Timeline_Drill(PlotPoint point){var row=Model.DayRecords.FirstOrDefault(r=>r.Record.Id==point.Key);if(row!=null)ShowDetail(row);}
    private async void ChapterDrill_Click(object sender,RoutedEventArgs e)
    {if(!await Model.SaveReflectionAsync())return;var point=(PlotPoint)((Button)sender).Tag;Model.OpenRecords(DateOnly.FromDateTime(Model.StatsFrom),DateOnly.FromDateTime(Model.StatsTo),point.SubjectId,point.Key);MainScroll.ScrollToTop();}
    private void Detail_Click(object sender,RoutedEventArgs e){if(((Button)sender).Tag is RecordRow row)ShowDetail(row);}
    private void ShowDetail(RecordRow row){_detailFocus=Keyboard.FocusedElement;Model.OpenDetail(row);Dispatcher.BeginInvoke(new Action(()=>DrawerCloseButton.Focus()));}
    private void CloseDrawer_Click(object sender,RoutedEventArgs e)
    {Model.DrawerOpen=false;Dispatcher.BeginInvoke(new Action(()=>{if(_detailFocus is FrameworkElement {IsVisible:true,IsEnabled:true} element&&PresentationSource.FromVisual(element)!=null)element.Focus();else if(Model.Page==0)TodayRecordList.Focus();else if(Model.Page==6)RecordList.Focus();else NavigationList.Focus();}));}
    private async void RestDay_Click(object sender,RoutedEventArgs e)=>await Model.ToggleRestDayAsync();
    internal static string DeletionImpact(RecordRow row)=>$"科目：{row.Subject}\n整次时长：{row.Duration}\n受影响日期：{row.Dates}\n{row.Record.Parts.Count} 个有效片段\n\n删除的是整次学习，所有上述日期的贡献会从日历与统计移除。记录和修订历史保留，可在已删除明细中恢复。";
    private async void DeleteRecord_Click(object sender,RoutedEventArgs e)
    {if(((FrameworkElement)sender).Tag is not RecordRow row||row.Deleted)return;if(Confirm("删除整次学习",DeletionImpact(row),"软删除整次记录"))await Model.RunAsync(()=>Model.Database.DeleteRecord(row.Record.Id,row.Record.Version),"整次记录已软删除，日历与统计已同步更新");}
    private async void RestoreRecord_Click(object sender,RoutedEventArgs e)
    {if(((Button)sender).Tag is RecordRow row&&row.Deleted)await Model.RunAsync(()=>Model.Database.RestoreRecord(row.Record.Id,row.Record.Version),"整次记录已恢复，统计已更新");}
    private async void ExcludePart_Click(object sender,RoutedEventArgs e)
    {if(Model.SelectedRecord is not { } row||((Button)sender).Tag is not PartRow part)return;
        if(Confirm("排除单个学习片段",$"科目：{row.Subject}\n日期：{part.Date}\n片段：{part.Time}\n时长：{part.Duration}\n\n仅此片段不再计入统计，整次身份和其他日期保留。原片段可在详情中恢复。","排除此片段"))await Model.RunAsync(()=>Model.Database.ExcludePart(row.Record.Id,part.Part.Id,row.Record.Version),"仅选中的片段已排除，其他片段保留");}
    private async void RestorePart_Click(object sender,RoutedEventArgs e)
    {if(Model.SelectedRecord is { } row&&((Button)sender).Tag is PartRow part)await Model.RunAsync(()=>Model.Database.RestorePart(row.Record.Id,part.Part.Id,row.Record.Version),"片段已恢复，所有视图已同步更新");}
}
