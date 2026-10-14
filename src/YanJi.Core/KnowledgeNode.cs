namespace YanJi.Core;

public enum KnowledgeNodeKind { Chapter, KnowledgePoint }
public enum LearningState { NotStarted, Learning, Learned, Deferred }
public sealed record ExamProfile(string Id, string Name, string ExamKey, int AdmissionYear, DateOnly? ExamDate);
public sealed record KnowledgeNode(string Id, string SubjectId, string Name, string? ParentId = null,
    KnowledgeNodeKind Kind = KnowledgeNodeKind.Chapter, int SortOrder = 0, int Progress = 0,
    Mastery Mastery = Mastery.Unknown, bool Archived = false, LearningState State = LearningState.NotStarted,
    string Description = "", string ProfileId = "legacy-default", long Version = 0);

/// <summary>Directory structure, not learning-time or mastery facts.</summary>
public static class KnowledgeTree
{
    public const int MaximumDepth = 32;
    public static void Validate(IReadOnlyCollection<KnowledgeNode> nodes)
    {
        var map = nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Id) || string.IsNullOrWhiteSpace(node.Name) ||
                node.Description.Length > 20000 || node.SortOrder < 0 || node.Progress is < 0 or > 100 ||
                !Enum.IsDefined(node.Kind) || !Enum.IsDefined(node.State) || !Enum.IsDefined(node.Mastery) ||
                string.IsNullOrWhiteSpace(node.ProfileId)) throw new ArgumentException("知识节点状态无效。");
            if (node.State == LearningState.NotStarted && node.Progress != 0 || node.State == LearningState.Learned && node.Progress != 100 || node.State == LearningState.Learning && node.Progress == 100)
                throw new ArgumentException("学习状态与进度不一致，请明确选择状态。");
            var seen = new HashSet<string>(StringComparer.Ordinal) { node.Id };
            var cursor = node;
            while (cursor.ParentId is string parent)
            {
                if (!map.TryGetValue(parent, out var next)) throw new ArgumentException("父节点不存在。");
                if (!seen.Add(parent)) throw new ArgumentException("目录不允许循环引用。");
                if (seen.Count > MaximumDepth) throw new ArgumentException("目录层级超过32层。");
                if (next.SubjectId != node.SubjectId || next.ProfileId != node.ProfileId)
                    throw new ArgumentException("不能跨科目或考试档案移动。");
                cursor = next;
            }
        }
    }
    public static IReadOnlyList<KnowledgeNode> Path(IEnumerable<KnowledgeNode> nodes, string id)
    {
        var map = nodes.ToDictionary(n => n.Id, StringComparer.Ordinal);
        List<KnowledgeNode> path = [];
        var seen = new HashSet<string>();
        for (string? cursor = id; cursor != null;)
        {
            if (!seen.Add(cursor) || path.Count >= MaximumDepth) throw new ArgumentException("目录层级无效。");
            var node = map[cursor]; path.Add(node); cursor = node.ParentId;
        }
        path.Reverse(); return path;
    }
    public static HashSet<string> Descendants(IEnumerable<KnowledgeNode> nodes, IEnumerable<string> roots)
    {
        var list = nodes.ToList();
        var children = list.Where(n => n.ParentId != null).ToLookup(n => n.ParentId!);
        var valid = list.Select(n => n.Id).ToHashSet();
        HashSet<string> result = []; var queue = new Queue<string>(roots);
        while (queue.TryDequeue(out var id))
            if (valid.Contains(id) && result.Add(id)) foreach (var child in children[id]) queue.Enqueue(child.Id);
        return result;
    }
}
