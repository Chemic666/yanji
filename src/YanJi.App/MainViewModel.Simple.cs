using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using YanJi.Core;
namespace YanJi.App;

public sealed partial class MainViewModel
{
    public event Action? DataRefreshed;
    private void NotifySimpleViews()=>DataRefreshed?.Invoke();
    [ObservableProperty] private int navigationIndex;
    private bool _syncNavigation;
    partial void OnNavigationIndexChanged(int value)
    { if (!_syncNavigation) Page = value switch { 0 => 0, 1 => 2, 2 => 4, _ => 7 }; }
    partial void OnPageChanged(int value)
    {
        if(value==1){Page=4;return;} // Existing calendar links now lead to the primary data page.
        _syncNavigation = true;
        NavigationIndex = value switch { 0 => 0, 2 => 1, 4 => 2, _ => 3 };
        _syncNavigation = false;
    }
    public string FocusOptionsLabel => FocusModeIndex switch
    { 1 => "倒计时 · 更换计时方式", 2 => "番茄钟 · 更换计时方式", _ => "自由计时 · 番茄钟 / 倒计时" };
    private static ReferenceScopes? _referenceScopes;
    internal static ReferenceScopes ScopeCatalog => _referenceScopes ??= ReferenceScopes.Load(Path.Combine(AppContext.BaseDirectory,"knowledge-packages"));
    internal string ReferenceVariant(string subjectId) => Database.Setting("reference-range:" + subjectId,"common");
    internal HashSet<string> VisibleReferenceIds(string subjectId)
    {
        var nodes = Database.KnowledgeNodes(subjectId);
        var result = nodes.Select(n => n.Id).ToHashSet();
        var variant = ReferenceVariant(subjectId);
        foreach (var install in Database.InstalledPackages().Where(i => i.Targets.Values.Contains(subjectId)))
            foreach (var binding in Database.PackageBindings(install.Id))
                if (!ScopeCatalog.Includes(install.PackageId,binding.SourceId,variant)) result.Remove(binding.NodeId);
        // A local child remains reachable even when its original template parent
        // belongs to another range. Filtering never rewrites its real ancestry.
        var map=nodes.ToDictionary(n=>n.Id);
        foreach(var id in result.ToArray())
            for(var parent=map[id].ParentId;parent!=null;parent=map[parent].ParentId)result.Add(parent);
        return result;
    }
    internal void EnsureTimerChapter(string? nodeId)
    {
        if (nodeId == null || TimerChapters.Any(c => c.Id == nodeId)) return;
        var node = Database.KnowledgeNodes(TimerSubject?.Id).FirstOrDefault(n => n.Id == nodeId && !n.Archived);
        if (node != null && Database.Chapters(node.SubjectId).Any(c => c.Id == nodeId))
            TimerChapters.Add(new(node.Id,node.SubjectId,node.Name,node.Progress,node.Mastery));
    }
    internal void SetReferenceVariant(string subjectId,string variant)
    {
        if (subjectId is not ("math" or "english")) return;
        var allowed = subjectId == "math" ? new[]{"common","math1","math2","all"} : new[]{"common","english1","english2","all"};
        if (!allowed.Contains(variant)) throw new ArgumentException("参考目录范围无效。");
        Database.SaveSetting("reference-range:"+subjectId,variant);
        Refresh();
    }
    internal static List<KnowledgePackage> LatestReferences() => Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory,"knowledge-packages"),"*.yanji-knowledge.json",SearchOption.AllDirectories)
        .Select(f=>KnowledgePackageFile.Parse(File.ReadAllBytes(f))).GroupBy(p=>p.PackageId)
        .Select(g=>g.OrderByDescending(p=>KnowledgePackageFile.VersionOf(p.PackageVersion)).First()).ToList();
}
