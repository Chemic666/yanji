using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YanJi.Core;

namespace YanJi.App;

public sealed record ParentChoice(string? Id, string Label);
public sealed record KnowledgeRecordRow(StudyRecord Record)
{
    public string Label=>DurationText.Format(Record.DurationMs)+" · "+string.Join("、",Record.Parts.Select(p=>p.Date).Distinct());
    public string Context=>Record.Context?.NodePath??"历史未采集路径";
}
public sealed class KnowledgeTreeRow : ObservableObject
{
    public KnowledgeNode Node { get; }
    public string Name => Node.Name;
    public string Label => (Node.Archived ? "已归档 · " : "") + Name;
    public string Detail => KnowledgeWorkspaceViewModel.StateLabels[(int)Node.State] + " · " + Node.Progress + "%";
    public string FullPath { get; }
    public ObservableCollection<KnowledgeTreeRow> Children { get; } = [];
    private readonly Func<string, IEnumerable<KnowledgeTreeRow>>? _load;
    private bool _loaded;
    private bool _expanded;
    private bool _selected;
    public bool Expanded { get => _expanded; set { if (SetProperty(ref _expanded, value) && value) Load(); } }
    public bool Selected { get => _selected; set => SetProperty(ref _selected, value); }
    internal KnowledgeTreeRow(KnowledgeNode node, string path, Func<string, IEnumerable<KnowledgeTreeRow>>? load, bool hasChildren)
    {
        Node = node; FullPath = path; _load = load;
        if (hasChildren) Children.Add(new(new("__placeholder", node.SubjectId, "加载目录…"), "", null, false));
    }
    public void Load()
    {
        if (_loaded) return; _loaded = true; Children.Clear();
        if (_load != null) foreach (var child in _load(Node.Id)) Children.Add(child);
    }
}

public sealed partial class KnowledgeWorkspaceViewModel : ObservableObject
{
    public static string[] StateLabels { get; } = ["未开始", "学习中", "已学习", "暂缓"];
    public static string[] MasteryLabels { get; } = ["尚未自评", "薄弱", "巩固中", "自评掌握"];
    public static string[] KindLabels { get; } = ["章节", "知识点"];
    public MainViewModel Owner { get; }
    public IReadOnlyList<Subject> Subjects { get; private set; }
    public string ProfileLabel { get; private set; }
    [ObservableProperty] private Subject? subject;
    [ObservableProperty] private bool knowledgeBusy;
    [ObservableProperty] private bool showArchived;
    [ObservableProperty] private string search = "";
    [ObservableProperty] private string feedback = "选择科目，梳理知识目录。";
    [ObservableProperty] private KnowledgeTreeRow? selected;
    [ObservableProperty] private string nodeName = "";
    [ObservableProperty] private string description = "";
    [ObservableProperty] private string progressText = "0";
    [ObservableProperty] private int stateIndex;
    [ObservableProperty] private int kindIndex;
    [ObservableProperty] private int masteryIndex;
    [ObservableProperty] private ParentChoice? parent;
    [ObservableProperty] private string orderText = "0";
    [ObservableProperty] private string pathLabel = "尚未选择节点";
    [ObservableProperty] private string durationLabel = "";
    [ObservableProperty] private string historyText = "";
    [ObservableProperty] private string studySummary = "";
    [ObservableProperty] private IReadOnlyList<KnowledgeRecordRow> linkedRecords = Array.Empty<KnowledgeRecordRow>();
    [ObservableProperty] private IReadOnlyList<LearningTask> linkedTasks = Array.Empty<LearningTask>();
    [ObservableProperty] private IReadOnlyList<WeakPoint> linkedWeakPoints = Array.Empty<WeakPoint>();
    [ObservableProperty] private IReadOnlyList<KnowledgeTreeRow> roots = Array.Empty<KnowledgeTreeRow>();
    [ObservableProperty] private IReadOnlyList<KnowledgeTreeRow> searchResults = Array.Empty<KnowledgeTreeRow>();
    [ObservableProperty] private IReadOnlyList<ParentChoice> parents = Array.Empty<ParentChoice>();
    public bool Searching => !string.IsNullOrWhiteSpace(Search);
    public bool HasSelection => Selected != null;
    public string ArchiveAction => Selected?.Node.Archived == true ? "重新启用" : "归档";
    public string NodeId => Selected?.Node.Id ?? "";
    private List<KnowledgeNode> _nodes = [];
    private Dictionary<string, KnowledgeNode> _map = [];
    private Dictionary<string, string> _paths = [];
    private ILookup<string, KnowledgeNode>? _children;
    private CancellationTokenSource? _searchCancellation;
    private int _generation;
    private long _epoch = -1;
    public double LastSearchMilliseconds { get; private set; }
    internal bool MatchesSnapshot(IReadOnlyList<KnowledgeNode> nodes)=>nodes.Count==_nodes.Count&&nodes.All(n=>_map.TryGetValue(n.Id,out var current)&&current.Version==n.Version);
    public void InvalidateAfterRestore()
    {
        _generation++; _searchCancellation?.Cancel(); _epoch = -1;
        Selected = null; Roots = []; SearchResults = []; _nodes = []; _map = []; _paths = [];
        NodeName = Description = HistoryText = DurationLabel = ""; Parents = []; Parent = null; Search = "";
    }
    public KnowledgeWorkspaceViewModel(MainViewModel owner)
    {
        Owner = owner; Subjects = owner.Database.Subjects(true); ProfileLabel = owner.Database.ExamProfiles().First().Name;
        subject = Subjects.FirstOrDefault();
    }
    private bool _skipSubjectReload;
    partial void OnSubjectChanged(Subject? value) { if (_skipSubjectReload) return; Selected = null; Search = ""; _ = ReloadAsync(); }
    private bool _skipArchiveReload;
    partial void OnShowArchivedChanged(bool value) { if (!_skipArchiveReload) _ = ReloadAsync(); }
    partial void OnSearchChanged(string value) { OnPropertyChanged(nameof(Searching)); _ = SearchAsync(); }
    partial void OnSelectedChanged(KnowledgeTreeRow? value)
    {
        OnPropertyChanged(nameof(HasSelection)); OnPropertyChanged(nameof(ArchiveAction)); OnPropertyChanged(nameof(NodeId));
        if (value == null) return;
        var n = value.Node; NodeName = n.Name; Description = n.Description; ProgressText = n.Progress.ToString(CultureInfo.InvariantCulture);
        StateIndex = (int)n.State; KindIndex = (int)n.Kind; MasteryIndex = (int)n.Mastery; OrderText = n.SortOrder.ToString(CultureInfo.InvariantCulture);
        PathLabel = value.FullPath;
        var excluded = KnowledgeTree.Descendants(_nodes, [n.Id]);
        Parents = new[] { new ParentChoice(null, "根目录 · 无父节点") }.Concat(_nodes.Where(x => !excluded.Contains(x.Id))
            .Select(x => new ParentChoice(x.Id, _paths[x.Id]))).ToArray();
        Parent = Parents.FirstOrDefault(p => p.Id == n.ParentId) ?? Parents[0];
        _ = DetailAsync(n);
    }
    private async Task DetailAsync(KnowledgeNode n)
    {
        try
        {
            var details = await Task.Run(() =>
            {
                var study = Owner.Database.KnowledgeStudy(n.Id, DateOnly.MinValue, DateOnly.MaxValue);
                var history = string.Join("\n", Owner.Database.History(n.Id).Where(r => r.EntityKind is "chapter" or "knowledge-node").TakeLast(8)
                    .Select(r => r.At.ToLocalTime().ToString("MM-dd HH:mm") + " · " + r.Reason));
                return (study, history);
            });
            if (Selected?.Node.Id != n.Id) return;
            DurationLabel = "自身 " + DurationText.Format(details.study.OwnDurationMs) + " · 含后代 " + DurationText.Format(details.study.SubtreeDurationMs);
            StudySummary = details.study.SessionCount + " 次有效学习 · 最近 " + (details.study.LastStudyDate?.ToString("yyyy-MM-dd")??"尚无记录") + "\n本科目未关联节点 " + DurationText.Format(details.study.UnlinkedDurationMs) + " · 含后代按片段去重，非掌握评价";
            LinkedRecords = details.study.Records.Select(r=>new KnowledgeRecordRow(r)).ToArray(); LinkedTasks = details.study.Tasks; LinkedWeakPoints = details.study.WeakPoints;
            HistoryText = details.history.Length == 0 ? "尚无修改历史。" : details.history;
        }
        catch (Exception e) { Feedback = e.Message; }
    }
    public async Task ReloadAsync(string? selectId = null)
    {
        int generation = ++_generation; var subjectId = Subject?.Id;
        KnowledgeBusy = true;
        try
        {
            var data = await Task.Run(() =>
            {
                var snapshot = Owner.Database.KnowledgeSnapshot(subjectId);
                var nodes = subjectId == null ? new List<KnowledgeNode>() : snapshot.Nodes;
                var map = nodes.ToDictionary(n => n.Id); var paths = new Dictionary<string, string>();
                foreach (var n in nodes)
                {
                    List<string> names = []; for (KnowledgeNode? cursor = n; cursor != null; cursor = cursor.ParentId == null ? null : map[cursor.ParentId]) names.Add(cursor.Name);
                    names.Reverse(); paths[n.Id] = string.Join(" / ", names);
                }
                return (nodes, map, paths, snapshot.Epoch, subjects: (IReadOnlyList<Subject>)Owner.Database.Subjects(true));
            });
            if (generation != _generation) return;
            _epoch = data.Epoch;
            Subjects = data.subjects; OnPropertyChanged(nameof(Subjects));
            ProfileLabel=Owner.Database.ExamProfiles().First(p=>p.Id=="legacy-default").Name;OnPropertyChanged(nameof(ProfileLabel));
            _skipSubjectReload = true; Subject = Subjects.FirstOrDefault(s => s.Id == subjectId) ?? Subjects.FirstOrDefault(); _skipSubjectReload = false;
            _nodes = data.nodes; _map = data.map; _paths = data.paths; _children = _nodes.Where(n => n.ParentId != null).ToLookup(n => n.ParentId!);
            Roots = Rows(null).ToArray(); Selected = null;
            if (selectId != null) Locate(selectId);
            Feedback = _nodes.Count == 0 ? "暂无目录。新增章节，或先整理一个知识点。" : $"{_nodes.Count} 个节点 · 进度与自评掌握独立记录";
            await SearchAsync();
        }
        catch (Exception e) { Feedback = "目录未加载：" + e.Message; }
        finally { if (generation == _generation) KnowledgeBusy = false; }
    }
    private IEnumerable<KnowledgeTreeRow> Rows(string? parent)
    {
        var candidates = parent == null ? _nodes.Where(n => n.ParentId == null) : _children![parent];
        return candidates.Where(n => ShowArchived || !n.Archived).OrderBy(n => n.SortOrder).ThenBy(n => n.Id)
            .Select(n => new KnowledgeTreeRow(n, _paths[n.Id], Rows, _children![n.Id].Any(c => ShowArchived || !c.Archived)));
    }
    public void Locate(string id)
    {
        if (!_map.ContainsKey(id)) return;
        var path = KnowledgeTree.Path(_nodes, id); var rows = Roots;
        foreach (var node in path)
        {
            var row = rows.FirstOrDefault(r => r.Node.Id == node.Id); if (row == null) return;
            if (node.Id == id) { row.Selected = true; Selected = row; return; }
            row.Expanded = true; rows = row.Children;
        }
    }
    private async Task SearchAsync()
    {
        _searchCancellation?.Cancel(); _searchCancellation = new(); var token = _searchCancellation.Token;
        var query = Search.Trim(); var generation = _generation; var nodes = _nodes; var paths = _paths; var show = ShowArchived; var map = _map;
        if (query.Length == 0) { SearchResults = []; return; }
        try
        {
            await Task.Delay(180, token);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            bool Active(KnowledgeNode n)
            { for (KnowledgeNode? p = n; p != null; p = p.ParentId == null ? null : map[p.ParentId]) { token.ThrowIfCancellationRequested(); if (p.Archived) return false; } return true; }
            var matches = await Task.Run(() => nodes.Where(n => (show || Active(n)) &&
                (paths[n.Id].Contains(query, StringComparison.OrdinalIgnoreCase) || n.Id.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .Take(200).Select(n => new KnowledgeTreeRow(n, paths[n.Id], null, false)).ToArray(), token);
            if (generation == _generation && !token.IsCancellationRequested) { SearchResults = matches; LastSearchMilliseconds = watch.Elapsed.TotalMilliseconds; Feedback = matches.Length == 200 ? "显示前200项；继续细化搜索词。" : $"找到 {matches.Length} 个节点"; }
        }
        catch (OperationCanceledException) { }
        catch (Exception e) { Feedback = e.Message; }
    }
    [RelayCommand] private async Task NewRootAsync() => await CreateAsync(null);
    [RelayCommand] private async Task NewChildAsync() => await CreateAsync(Selected?.Node.Id);
    private async Task CreateAsync(string? parentId)
    {
        if (Subject == null) return;
        var node = new KnowledgeNode(Guid.NewGuid().ToString("N"), Subject.Id, "新章节", parentId,
            SortOrder: _nodes.Where(n => n.ParentId == parentId).Select(n => n.SortOrder).DefaultIfEmpty(-1).Max() + 1);
        await WriteAsync([node], "新增目录", node.Id);
    }
    [RelayCommand] private async Task SaveAsync()
    {
        if (Selected == null) return;
        if (!int.TryParse(ProgressText, out var progress) || progress is < 0 or > 100 || !int.TryParse(OrderText, out var order) || order < 0)
        { Feedback = "进度需为0～100，排序需为非负整数。"; return; }
        progress = StateIndex == 0 ? 0 : StateIndex == 2 ? 100 : StateIndex == 1 ? Math.Min(99, progress) : progress;
        await WriteAsync([Selected.Node with { Name = NodeName, Description = Description, ParentId = Parent?.Id,
            Kind = (KnowledgeNodeKind)KindIndex, State = (LearningState)StateIndex, Progress = progress, Mastery = (Mastery)MasteryIndex, SortOrder = order }], "节点与学习状态修改", Selected.Node.Id);
    }
    [RelayCommand] private async Task ArchiveAsync()
    {
        if (Selected == null) return; var node = Selected.Node;
        _skipArchiveReload = true; ShowArchived = true; _skipArchiveReload = false;
        await WriteAsync([node with { Archived = !node.Archived }], node.Archived ? "重新启用" : "归档目录", node.Id);
    }
    private async Task WriteAsync(IReadOnlyList<KnowledgeNode> nodes, string reason, string id)
    {
        if (KnowledgeBusy) return; KnowledgeBusy = true;
        try {
            var epoch = _epoch;
            await Task.Run(() => Owner.Database.SaveKnowledgeNodes(nodes, reason, epoch)); Owner.Refresh();
            await ReloadAsync(id); Feedback = reason + "已保存；学习时长未变更。"; }
        catch (Exception e) { Feedback = e.Message; }
        finally { KnowledgeBusy = false; }
    }
}
