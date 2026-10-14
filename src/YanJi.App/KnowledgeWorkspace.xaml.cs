using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace YanJi.App;
public partial class KnowledgeWorkspace : UserControl
{
    public KnowledgeWorkspaceViewModel? Model { get; private set; }
    private bool _detail;
    public KnowledgeWorkspace() { InitializeComponent(); PreviewKeyDown += Keys; }
    private async void Workspace_Loaded(object sender, RoutedEventArgs e)
    {
        if (Model != null) return;
        if (Window.GetWindow(this) is not MainWindow owner) return;
        Model = new(owner.Model); DataContext = Model;
        Model.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(Model.HasSelection) && !Model.HasSelection) { _detail = false; Adapt(); } };
        await Model.ReloadAsync(); Adapt();
    }
    private void Workspace_SizeChanged(object sender, SizeChangedEventArgs e) => Adapt();
    private void Adapt()
    {
        if (DirectoryPanel == null) return;
        bool narrow = ActualWidth < 850;
        Columns.ColumnDefinitions[0].Width = new(narrow ? 1 : .85, GridUnitType.Star);
        Columns.ColumnDefinitions[1].Width = new(narrow ? 0 : 16);
        Columns.ColumnDefinitions[2].Width = new(narrow ? 0 : 1.4, GridUnitType.Star);
        Grid.SetColumn(DetailPanel, narrow ? 0 : 2);
        DirectoryPanel.Visibility = narrow && _detail ? Visibility.Collapsed : Visibility.Visible;
        DetailPanel.Visibility = !narrow || _detail ? Visibility.Visible : Visibility.Collapsed;
        BackButton.Visibility = narrow ? Visibility.Visible : Visibility.Collapsed;
    }
    private void Tree_Selected(object sender, RoutedPropertyChangedEventArgs<object> e)
    { if (Model == null || e.NewValue is not KnowledgeTreeRow row) return; Model.Selected = row; _detail = true; Adapt(); }
    private void Search_Selected(object sender, SelectionChangedEventArgs e)
    { if (Model?.Selected == null) return; _detail = true; Adapt(); }
    private void Back_Click(object sender, RoutedEventArgs e) { _detail = false; Adapt(); DirectoryTree.Focus(); }
    private void Keys(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F2 && Model?.HasSelection == true) { _detail = true; Adapt(); NameBox.Focus(); NameBox.SelectAll(); e.Handled = true; }
        if (e.Key == Key.Escape && _detail) { _detail = false; Adapt(); e.Handled = true; }
        if (e.Key == Key.Enter && sender != NameBox && Keyboard.FocusedElement is ListBoxItem && Model?.Selected != null) { _detail = true; Adapt(); e.Handled = true; }
    }
    private async void Archive_Click(object sender, RoutedEventArgs e)
    {
        if (Model?.Selected == null) return;
        var n = Model.Selected.Node;
        var dialog = new ConfirmDialog(Window.GetWindow(this), n.Archived ? "重新启用目录" : "归档目录", n.Name + "\n归档在新学习选择中隐藏此目录及其后代。历史学习记录和时长保留。", n.Archived ? "重新启用" : "归档");
        if (dialog.ShowDialog() == true) await Model.ArchiveCommand.ExecuteAsync(null);
    }
    private async void Subjects_Click(object sender, RoutedEventArgs e) { ((MainWindow)Window.GetWindow(this)).ShowLegacyProgress(); if(Model!=null) await Model.ReloadAsync(); }
    private async void Tasks_Click(object sender, RoutedEventArgs e) { ((MainWindow)Window.GetWindow(this)).ShowLegacyProgress(); if(Model!=null) await Model.ReloadAsync(); }
}
