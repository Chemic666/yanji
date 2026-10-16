using Microsoft.Data.Sqlite;
using YanJi.Core;

namespace YanJi.Data;

public sealed partial class YanJiDatabase
{
    private const string ProgressNotePrefix = "study-progress:v1:";

    private static void ValidateProgressNote(NodeProgressNote item, string nodeId, bool stored)
    {
        static bool ValidText(string? text, int limit) => text != null && text.Length <= limit &&
            !text.Any(c => char.IsControl(c) && c is not ('\r' or '\n' or '\t'));
        if (string.IsNullOrWhiteSpace(nodeId) || item.NodeId != nodeId ||
            !ValidText(item.Content, 20000) || !ValidText(item.Next, 2000) || item.Version < 0 ||
            stored && (item.Version == 0 || item.UpdatedAt == null))
            throw new ArgumentException("学习进度字段或版本无效（进度最多20000字，下一步最多2000字）。");
    }

    private static void CheckStoredProgressNote(SqliteConnection c, NodeProgressNote item, string nodeId, int schema)
    {
        ValidateProgressNote(item, nodeId, true);
        if (schema < 2 || Scalar(c, null, "SELECT row_version FROM chapters WHERE id=$id", ("$id", nodeId)) is not long version || item.Version > version)
            throw new InvalidDataException("学习进度节点引用或版本无效。");
    }

    private static void CheckProgressNoteAudits(SqliteConnection c, int schema)
    {
        using var cmd = Command(c, null, "SELECT entity,version,before_json,after_json FROM revisions WHERE kind='study-progress'");
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(3)) throw new InvalidDataException("学习进度审计缺少保存内容。");
            var after = Parse<NodeProgressNote>(reader.GetString(3));
            CheckStoredProgressNote(c, after, reader.GetString(0), schema);
            if (after.Version != reader.GetInt64(1)) throw new InvalidDataException("学习进度审计版本无效。");
            if (!reader.IsDBNull(2))
            {
                var before = Parse<NodeProgressNote>(reader.GetString(2));
                CheckStoredProgressNote(c, before, reader.GetString(0), schema);
                if (before.Version != after.Version - 1) throw new InvalidDataException("学习进度审计版本不连续。");
            }
            else if (after.Version != 1) throw new InvalidDataException("学习进度初始审计版本无效。");
        }
    }

    public List<NodeProgressNote> ProgressNotes()
    {
        lock (_gate)
        {
            using var c = Open();
            var schema = Convert.ToInt32(Scalar(c, null, "PRAGMA user_version"));
            using var cmd = Command(c, null, "SELECT id,payload FROM settings WHERE substr(id,1,$length)=$prefix ORDER BY id",
                ("$length", ProgressNotePrefix.Length), ("$prefix", ProgressNotePrefix));
            using var reader = cmd.ExecuteReader(); List<NodeProgressNote> result = [];
            while (reader.Read())
            {
                var note = Parse<NodeProgressNote>(Parse<string>(reader.GetString(1)));
                CheckStoredProgressNote(c, note, reader.GetString(0)[ProgressNotePrefix.Length..], schema);
                result.Add(note);
            }
            return result;
        }
    }

    public NodeProgressNote SaveProgressNote(string nodeId, long nodeVersion, LearningState state, string content, string next)
    {
        ValidateProgressNote(new(nodeId, content, next), nodeId, false);
        if (!Enum.IsDefined(state)) throw new ArgumentException("学习状态无效。");
        lock (_gate)
        {
            using var c = Open(); using var tx = c.BeginTransaction();
            var nodes = ReadKnowledgeNodes(c, tx); KnowledgeTree.Validate(nodes);
            var node = nodes.SingleOrDefault(n => n.Id == nodeId) ?? throw new ArgumentException("学习节点不存在。");
            if (node.Version != nodeVersion) throw new InvalidOperationException("目录已被修改，请刷新后重试。");
            if (KnowledgeTree.Path(nodes, nodeId).Any(n => n.Archived) ||
                Equals(Scalar(c, tx, "SELECT archived FROM subjects WHERE id=$id", ("$id", node.SubjectId)), 1L))
                throw new InvalidOperationException("已归档的节点或科目不可保存新进度；历史进度仍可查看。");
            var key = ProgressNotePrefix + nodeId;
            var raw = Scalar(c, tx, "SELECT payload FROM settings WHERE id=$id", ("$id", key)) as string;
            var before = raw == null ? null : Parse<NodeProgressNote>(Parse<string>(raw));
            if (before != null)
            {
                ValidateProgressNote(before, nodeId, true);
                if (before.Version > node.Version) throw new InvalidDataException("学习进度版本无效。");
            }
            var progress = state == LearningState.NotStarted ? 0 : state == LearningState.Learned ? 100 :
                state == LearningState.Learning ? Math.Min(99, node.Progress) : node.Progress;
            var updated = node with { State = state, Progress = progress, Version = checked(node.Version + 1) };
            var saved = new NodeProgressNote(nodeId, content, next, checked((before?.Version ?? 0) + 1), DateTimeOffset.UtcNow);
            WriteKnowledgeNode(c, tx, updated);
            Audit(c, tx, "knowledge-node", nodeId, updated.Version, "学习进度保存", Json(node), Json(updated));
            Exec(c, tx, "INSERT INTO settings VALUES($id,$payload) ON CONFLICT(id) DO UPDATE SET payload=excluded.payload",
                ("$id", key), ("$payload", Json(Json(saved))));
            Audit(c, tx, "study-progress", nodeId, saved.Version, "学习进度保存", before == null ? null : Json(before), Json(saved));
            tx.Commit(); return saved;
        }
    }

    public List<PackageBinding> PackageBindings(string installId)
    {
        lock (_gate)
        {
            using var c = Open();
            if (!ReadInstalls(c).Any(i => i.Id == installId)) throw new ArgumentException("知识包安装不存在。");
            return ReadBindings(c, installId);
        }
    }
}
