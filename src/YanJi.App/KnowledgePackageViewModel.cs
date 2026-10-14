using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YanJi.Core;
using YanJi.Data;
namespace YanJi.App;

public sealed record PackageCatalogItem(KnowledgePackage Package,string Path)
{public string Name=>Package.Name;public string Detail=>Package.PackageVersion+" · "+Package.Nodes.Count+" 个节点 · 参考目录";}
public sealed partial class PackageTargetRow : ObservableObject
{
    public string Key {get;} public string Label {get;} public IReadOnlyList<Subject> Subjects {get;}
    [ObservableProperty] private Subject? selected;
    public PackageTargetRow(PackageSubject s,IReadOnlyList<Subject> subjects,Subject? initial){Key=s.Key;Label=s.Name+(s.Variant==null?"":" · "+s.Variant);Subjects=subjects;selected=initial;}
}
public sealed partial class PackageConflictRow : ObservableObject
{
    public PackageConflict Conflict {get;} public string Label=>Conflict.NodeName+" · "+FieldLabel(Conflict.Field);
    public string Values=>"原模板："+(Conflict.Baseline??"根目录")+"\n本地："+(Conflict.Local??"根目录")+"\n新模板："+(Conflict.Incoming??"根目录");
    public bool Selectable {get;} public static string[] Options {get;}=["保留本地","采用模板"];
    [ObservableProperty] private int choiceIndex;
    public PackageConflictRow(PackageConflict c,bool rollback){Conflict=c;Selectable=c.Field is not("Removal")&&!(rollback&&c.Field=="Description");}
    private static string FieldLabel(string f)=>f switch{"Name"=>"名称","Description"=>"备注","ParentId"=>"层级","SortOrder"=>"排序","Kind"=>"节点类型","Binding"=>"模板绑定","Removal"=>"安全保留",_=>f};
}
public sealed partial class KnowledgePackageViewModel : ObservableObject
{
    public MainViewModel Owner {get;} public Func<string,Task<bool>>? Confirm {get;set;}
    public IReadOnlyList<ExamProfile> Profiles {get;} public IReadOnlyList<Subject> Subjects {get;}
    private List<PackageCatalogItem> _catalog=[];
    [ObservableProperty] private IReadOnlyList<PackageCatalogItem> catalog=[];
    [ObservableProperty] private PackageCatalogItem? selectedCatalog;
    [ObservableProperty] private Subject? filterSubject;
    [ObservableProperty] private ExamProfile? profile;
    [ObservableProperty] private bool busy;
    [ObservableProperty] private string feedback="浏览参考目录，或导入本地 JSON 知识包。";
    [ObservableProperty] private string packageName="尚未选择知识包";
    [ObservableProperty] private string description="所有模板均为纯数据；学习时长和掌握程度不会随模板更新改变。";
    [ObservableProperty] private string sourceText="";
    [ObservableProperty] private string summary="先选择模板和安装目标，再生成差异预览。";
    [ObservableProperty] private IReadOnlyList<PackageTargetRow> targets=[];
    [ObservableProperty] private IReadOnlyList<KnowledgeTreeRow> roots=[];
    [ObservableProperty] private IReadOnlyList<PackageConflictRow> conflicts=[];
    [ObservableProperty] private IReadOnlyList<string> changeRows=[];
    [ObservableProperty] private IReadOnlyList<PackageInstall> installed=[];
    [ObservableProperty] private PackageInstall? selectedInstall;
    [ObservableProperty] private string historyText="选择已安装包查看版本历史。";
    [ObservableProperty] private int tabIndex;
    [ObservableProperty] private bool canApply;
    private KnowledgePackage? _package; private PackagePreview? _preview;
    public KnowledgePackageViewModel(MainViewModel owner,Subject? current)
    {Owner=owner;Profiles=owner.Database.ExamProfiles();Subjects=owner.Database.Subjects();profile=Profiles.FirstOrDefault();filterSubject=current??Subjects.FirstOrDefault();}
    public async Task InitializeAsync()
    {
        await RunAsync(async()=>
        {
            _catalog=await Task.Run(()=>Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory,"knowledge-packages"),"*.yanji-knowledge.json",SearchOption.AllDirectories)
                .Select(f=>new PackageCatalogItem(KnowledgePackageFile.Parse(File.ReadAllBytes(f)),f)).OrderBy(p=>p.Package.PackageId).ToList());
            Filter();await RefreshInstalledAsync();
        });
    }
    partial void OnFilterSubjectChanged(Subject? value)=>Filter();
    private void Filter(){Catalog=_catalog.Where(p=>FilterSubject==null||p.Package.Subjects.Any(s=>s.Key==FilterSubject.Id||s.Key=="custom")).ToArray();SelectedCatalog=Catalog.FirstOrDefault();}
    partial void OnSelectedCatalogChanged(PackageCatalogItem? value){if(value!=null)SelectPackage(value.Package);}
    partial void OnProfileChanged(ExamProfile? value){_preview=null;CanApply=false;if(_package!=null)SelectPackage(_package);}
    partial void OnSelectedInstallChanged(PackageInstall? value)
    {
        if(value==null){HistoryText="尚无安装记录";return;}
        _=RunAsync(async()=>{var history=await Task.Run(()=>Owner.Database.PackageHistory(value.Id));if(SelectedInstall?.Id==value.Id)HistoryText=string.Join("\n\n",history.Select(h=>$"{h.At.ToLocalTime():yyyy-MM-dd HH:mm:ss} · {h.Kind switch{"install"=>"安装","update"=>"更新",_=>"回滚"}} {h.FromVersion??"无"} → {h.ToVersion}"+(h.RolledBack?" · 已撤销":"")+"\n"+h.Changes.Count+" 项目录变更"));});
    }
    public void SelectPackage(KnowledgePackage p)
    {
        _package=p;_preview=null;CanApply=false;Conflicts=[];ChangeRows=[];
        PackageName=p.Name+" · "+p.PackageVersion;Description=p.Description;
        SourceText="维护方："+p.Maintainer+"\n许可："+p.License.Id+" · "+p.License.Text+"\n"+string.Join("\n",p.Sources.Select(s=>s.Title+(s.Url==null?"":"\n"+s.Url)))+"\nSHA-256："+p.ContentHash;
        Targets=p.Subjects.Select(s=>new PackageTargetRow(s,Subjects,Subjects.FirstOrDefault(x=>x.Id==s.Key)??(s.Key=="custom"?FilterSubject:null))).ToArray();
        Roots=PreviewTree(p);Summary=$"{p.Nodes.Count} 个参考节点；请核对科目变体和目标档案。";Feedback="已载入纯数据模板；尚未写入数据库。";
    }
    private static IReadOnlyList<KnowledgeTreeRow> PreviewTree(KnowledgePackage p)
    {
        var map=p.Nodes.ToDictionary(n=>n.Id);var children=p.Nodes.Where(n=>n.ParentId!=null).ToLookup(n=>n.ParentId!);
        KnowledgeTreeRow Row(PackageNode n)
        {
            List<string> path=[];for(PackageNode? current=n;current!=null;current=current.ParentId==null?null:map[current.ParentId])path.Add(current.Name);path.Reverse();
            return new(new(n.Id,n.SubjectKey,n.Name,Kind:n.Kind,Description:n.Description),string.Join(" / ",path),id=>children[id].OrderBy(n=>n.Order).Select(Row),children[n.Id].Any());
        }
        return p.Nodes.Where(n=>n.ParentId==null).OrderBy(n=>n.Order).Select(Row).ToArray();
    }
    public async Task LoadFileAsync(string file)
    {
        TabIndex=0;
        await RunAsync(async()=>
        {
            var package=await Task.Run(()=>
            {
                using var stream=new FileStream(file,FileMode.Open,FileAccess.Read,FileShare.Read);
                if(stream.Length>KnowledgePackageFile.MaximumBytes)throw new InvalidDataException("知识包超过16 MiB，请使用较小文件。");
                var bytes=new byte[checked((int)stream.Length)];stream.ReadExactly(bytes);return KnowledgePackageFile.Parse(bytes);
            });
            SelectPackage(package);TabIndex=0;
        });
    }
    private Dictionary<string,string> TargetMap()=>Targets.ToDictionary(t=>t.Key,t=>t.Selected?.Id??throw new InvalidDataException("请明确选择每个包科目的安装目标。"));
    private Dictionary<string,PackageChoice> Choices()=>Conflicts.ToDictionary(c=>c.Conflict.Key,c=>c.ChoiceIndex==1&&c.Selectable?PackageChoice.UseTemplate:PackageChoice.KeepLocal);
    private void ShowPreview(PackagePreview preview)
    {
        _preview=preview;Conflicts=preview.Conflicts.Select(c=>new PackageConflictRow(c,preview.RollbackOperationId!=null)).ToArray();
        ChangeRows=preview.Changes.Select(c=>c.Summary+" · "+c.After.Name+(c.Before==null?"":"\n"+c.Before.Name+" → "+c.After.Name)+(c.HistoricalReferences==0?"":"\n"+c.HistoricalReferences+" 项直接历史关联（含已删除记录与薄弱点）")).ToArray();
        Summary=preview.NoOp?"已安装相同版本和内容，无需重复写入。":$"新增 {preview.Added} · 修改 {preview.Changed} · 归档 {preview.Archived} · 需要关注 {preview.Conflicts.Count}";
        Feedback=preview.Error??"请检查差异与冲突。默认保留本地；选择采用模板后会重新验证最终目录。";CanApply=preview.Error==null&&!preview.NoOp;TabIndex=2;
    }
    [RelayCommand] private async Task PreviewAsync()
    {
        await RunAsync(async()=>{if(_package==null||Profile==null)throw new InvalidDataException("请先选择模板和考试档案。");var package=_package;var profile=Profile.Id;var targets=TargetMap();ShowPreview(await Task.Run(()=>Owner.Database.PreviewPackage(package,profile,targets)));});
    }
    [RelayCommand] private async Task RollbackAsync()
    {
        await RunAsync(async()=>{var id=SelectedInstall?.Id??throw new InvalidDataException("请先选择已安装包。");ShowPreview(await Task.Run(()=>Owner.Database.PreviewPackageRollback(id)));});
    }
    [RelayCommand] private async Task ResolveAsync()
    {
        await RunAsync(async()=>
        {
            var original=_preview??throw new InvalidDataException("请先生成预览。");var choices=Choices();
            var resolved=await Task.Run(()=>original.RollbackOperationId==null?Owner.Database.PreviewPackage(KnowledgePackageFile.Parse(System.Text.Encoding.UTF8.GetBytes(original.Payload)),original.ProfileId,original.Targets,choices):Owner.Database.PreviewPackageRollback(original.InstallId,choices));
            if(resolved.Token!=original.Token)throw new InvalidOperationException("数据已变化，请重新预览。");
            ShowPreview(resolved);foreach(var conflict in Conflicts)conflict.ChoiceIndex=choices.GetValueOrDefault(conflict.Conflict.Key)==PackageChoice.UseTemplate?1:0;
        });
    }
    [RelayCommand] private async Task ApplyAsync()
    {
        await RunAsync(async()=>
        {
            var original=_preview??throw new InvalidDataException("请先生成预览。");var choices=Choices();
            var resolved=await Task.Run(()=>original.RollbackOperationId==null?Owner.Database.PreviewPackage(KnowledgePackageFile.Parse(System.Text.Encoding.UTF8.GetBytes(original.Payload)),original.ProfileId,original.Targets,choices):Owner.Database.PreviewPackageRollback(original.InstallId,choices));
            if(resolved.Token!=original.Token)throw new InvalidOperationException("目录或学习记录已变化，请重新预览。");if(resolved.Error!=null)throw new InvalidDataException(resolved.Error);
            var body=Summary+"\n目标："+(Profile?.Name??original.ProfileId)+"\n"+string.Join("、",original.Targets.Values.Select(id=>Subjects.Single(s=>s.Id==id).Name))+"\n仅调整目录；保留学习记录、进度与掌握状态。应用前将验证完整备份。";
            if(Confirm==null||!await Confirm(body))return;
            var result=await Task.Run(()=>Owner.Database.ApplyPackage(original,choices));CanApply=false;Feedback=result.NoOp?"相同内容已安装，没有重复写入。":"目录已更新；学习记录与状态完整保留。完整备份已验证。";
            Owner.Refresh();await RefreshInstalledAsync();
        });
    }
    private async Task RefreshInstalledAsync(){Installed=await Task.Run(()=>Owner.Database.InstalledPackages());}
    private async Task RunAsync(Func<Task> action)
    {
        if(Busy)return;Busy=true;
        try{await action();}catch(Exception e) when(e is InvalidDataException or IOException or ArgumentException or InvalidOperationException or Microsoft.Data.Sqlite.SqliteException){Feedback="未完成："+e.Message+" 当前学习数据未被部分修改。";CanApply=false;}
        finally{Busy=false;}
    }
}
