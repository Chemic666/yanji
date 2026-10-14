using System.Windows;
using System.Windows.Controls;
namespace YanJi.App;
public partial class MainWindow
{
    internal async System.Threading.Tasks.Task RestoreDataAsync(string path)
    {
        KnowledgeView.Model?.InvalidateAfterRestore();
        if (await Model.RunAsync(() => Model.Database.RestoreBackup(path), "完整备份已恢复")) await Model.InitializeAsync();
        if (KnowledgeView.Model != null) await KnowledgeView.Model.ReloadAsync();
    }
    internal void ShowLegacyProgress()
    {
        // Keep existing subject/task editing and timer commands; no M3 rewrite.
        var parent = (Grid)LegacyProgress.Parent; var index = parent.Children.IndexOf(LegacyProgress);
        parent.Children.Remove(LegacyProgress);
        var scroll = new ScrollViewer { Content = LegacyProgress, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(24) };
        var dialog = new Window { Owner = this, Title = "科目与学习任务", Width = 1000, Height = 680, MinWidth = 900, MinHeight = 600,
            DataContext = Model, Content = scroll, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        try { dialog.ShowDialog(); }
        finally { scroll.Content = null; parent.Children.Insert(index, LegacyProgress); }
    }
}
