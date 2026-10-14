using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
namespace YanJi.App;
public partial class KnowledgePackageWindow : Window
{
    public KnowledgePackageViewModel Model {get;}
    internal Task Initialization {get;private set;}=Task.CompletedTask;
    public KnowledgePackageWindow(MainWindow owner)
    {
        InitializeComponent();Owner=owner;Model=new(owner.Model,owner.KnowledgeView.Model?.Subject);DataContext=Model;
        Model.Confirm=message=>Task.FromResult(new ConfirmDialog(this,"确认知识目录变更",message,"确认应用",false).ShowDialog()==true);
        Loaded+=async(_,_)=>await (Initialization=Model.InitializeAsync());
    }
    private async void Import_Click(object sender,RoutedEventArgs e)
    {
        var file=new OpenFileDialog{Title="导入纯数据知识包",Filter="知识包 JSON|*.yanji-knowledge.json;*.json"};if(file.ShowDialog(this)==true)await Model.LoadFileAsync(file.FileName);
    }
}
