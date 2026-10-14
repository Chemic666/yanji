using System.Globalization;
using Microsoft.Data.Sqlite;
using YanJi.Core;

namespace YanJi.Data;

public sealed partial class YanJiDatabase
{
    private long _knowledgeEpoch;
    public (List<KnowledgeNode> Nodes, long Epoch) KnowledgeSnapshot(string? subject = null)
    { lock (_gate) return (KnowledgeNodes(subject), _knowledgeEpoch); }
    // Schema2 deliberately contains no M2 package tables or M3 learning-context tables.
    private const string Migration2Sql = """
        CREATE TABLE exam_profiles(id TEXT PRIMARY KEY,name TEXT NOT NULL,exam_key TEXT NOT NULL,admission_year INTEGER NOT NULL,exam_date TEXT);
        CREATE TABLE exam_subjects(profile_id TEXT NOT NULL REFERENCES exam_profiles(id),subject_id TEXT NOT NULL REFERENCES subjects(id),PRIMARY KEY(profile_id,subject_id));
        ALTER TABLE chapters ADD COLUMN profile_id TEXT REFERENCES exam_profiles(id);
        ALTER TABLE chapters ADD COLUMN parent_id TEXT REFERENCES chapters(id);
        ALTER TABLE chapters ADD COLUMN node_kind INTEGER NOT NULL DEFAULT 0 CHECK(node_kind IN(0,1));
        ALTER TABLE chapters ADD COLUMN description TEXT NOT NULL DEFAULT '';
        ALTER TABLE chapters ADD COLUMN sort_order INTEGER NOT NULL DEFAULT 0 CHECK(sort_order>=0);
        ALTER TABLE chapters ADD COLUMN learning_state INTEGER NOT NULL DEFAULT 0 CHECK(learning_state BETWEEN 0 AND 3);
        ALTER TABLE chapters ADD COLUMN row_version INTEGER NOT NULL DEFAULT 1 CHECK(row_version>0);
        CREATE INDEX chapters_scope_parent ON chapters(profile_id,subject,parent_id,sort_order,id);
        CREATE INDEX chapters_parent ON chapters(parent_id);
        CREATE INDEX sessions_chapter ON sessions(chapter);
        CREATE INDEX revisions_kind_entity ON revisions(kind,entity,at);
        CREATE TABLE schema_migrations(target_version INTEGER PRIMARY KEY,script_digest TEXT NOT NULL,applied_at TEXT NOT NULL);
        """;
    private static void ApplySchema2(SqliteConnection c, SqliteTransaction tx)
    {
        Exec(c, tx, Migration2Sql);
        var date = Scalar(c, tx, "SELECT payload FROM settings WHERE id='exam-date'") is string raw ? Parse<string>(raw) : "";
        Exec(c, tx, "INSERT INTO exam_profiles VALUES('legacy-default','2028 考研 · 历史默认档案','cn-postgraduate',2028,$date)", ("$date", date.Length == 0 ? null : date));
        Exec(c, tx, "INSERT INTO exam_subjects SELECT 'legacy-default',id FROM subjects");
        Exec(c, tx, "UPDATE chapters SET profile_id='legacy-default',sort_order=rowid,learning_state=CASE WHEN progress=0 THEN 0 WHEN progress=100 THEN 2 ELSE 1 END");
        Exec(c, tx, "INSERT INTO schema_migrations VALUES(2,$digest,$at)",
            ("$digest", Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Migration2Sql)))),
            ("$at", DateTimeOffset.UtcNow.ToString("O")));
        Exec(c, tx, "PRAGMA user_version=2");
    }
    private static List<KnowledgeNode> ReadKnowledgeNodes(SqliteConnection c, SqliteTransaction? tx = null)
    {
        using var cmd = Command(c, tx, "SELECT id,subject,name,parent_id,node_kind,sort_order,progress,mastery,archived,learning_state,description,profile_id,row_version FROM chapters ORDER BY sort_order,id");
        using var r = cmd.ExecuteReader(); List<KnowledgeNode> nodes = [];
        while (r.Read()) nodes.Add(new(r.GetString(0), r.GetString(1), r.GetString(2), r.IsDBNull(3) ? null : r.GetString(3),
            (KnowledgeNodeKind)r.GetInt32(4), r.GetInt32(5), r.GetInt32(6), (Mastery)r.GetInt32(7), r.GetBoolean(8),
            (LearningState)r.GetInt32(9), r.GetString(10), r.IsDBNull(11) ? "" : r.GetString(11), r.GetInt64(12)));
        return nodes;
    }
    private static List<KnowledgeNode> CheckKnowledgeData(SqliteConnection c)
    {
        var nodes = ReadKnowledgeNodes(c); KnowledgeTree.Validate(nodes);
        if (nodes.Any(n => n.Version <= 0) || Scalar(c, null, "SELECT c.id FROM chapters c LEFT JOIN exam_subjects e ON e.profile_id=c.profile_id AND e.subject_id=c.subject WHERE e.subject_id IS NULL LIMIT 1") != null)
            throw new InvalidDataException("知识目录档案或科目引用无效。");
        using var cmd = Command(c, null, "SELECT id,name,exam_key,admission_year,exam_date FROM exam_profiles");
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            if (string.IsNullOrWhiteSpace(r.GetString(1)) || string.IsNullOrWhiteSpace(r.GetString(2)) || r.GetInt32(3) is < 2000 or > 2200)
                throw new InvalidDataException("考试档案无效。");
            if (!r.IsDBNull(4)) DateOnly.ParseExact(r.GetString(4), "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        if (!MigrationDigestMatches(Migration2Sql, Scalar(c, null, "SELECT script_digest FROM schema_migrations WHERE target_version=2")))
            throw new InvalidDataException("迁移登记无效。");
        return nodes;
    }
    public List<ExamProfile> ExamProfiles()
    {
        lock (_gate)
        {
            using var c = Open(); using var cmd = Command(c, null, "SELECT id,name,exam_key,admission_year,exam_date FROM exam_profiles ORDER BY id");
            using var r = cmd.ExecuteReader(); List<ExamProfile> profiles = [];
            while (r.Read()) profiles.Add(new(r.GetString(0), r.GetString(1), r.GetString(2), r.GetInt32(3), r.IsDBNull(4) ? null : DateOnly.ParseExact(r.GetString(4), "yyyy-MM-dd")));
            return profiles;
        }
    }
    public List<KnowledgeNode> KnowledgeNodes(string? subject = null)
    { lock (_gate) { using var c = Open(); return CheckKnowledgeData(c).Where(n => subject == null || n.SubjectId == subject).ToList(); } }
    public List<KnowledgeNode> SaveKnowledgeNodes(IReadOnlyList<KnowledgeNode> changes, string reason = "目录修改", long? expectedEpoch = null)
    {
        if (changes.Count == 0) return [];
        if (changes.Select(n => n.Id).Distinct().Count() != changes.Count) throw new ArgumentException("重复节点修改。");
        lock (_gate)
        {
            if (expectedEpoch != null && expectedEpoch != _knowledgeEpoch) throw new InvalidOperationException("数据库已恢复，目录编辑已失效；请重新加载。");
            using var c = Open(); using var tx = c.BeginTransaction();
            var before = ReadKnowledgeNodes(c, tx).ToDictionary(n => n.Id);
            var after = new Dictionary<string, KnowledgeNode>(before); List<KnowledgeNode> saved = [];
            bool active = Scalar(c, tx, "SELECT payload FROM active_timer") != null;
            foreach (var input in changes)
            {
                before.TryGetValue(input.Id, out var old);
                if (input.Name.Length > 200 && (old == null || input.Name != old.Name)) throw new ArgumentException("新建或重命名节点名称最多200字符；旧长名称原样保留。");
                if ((old?.Version ?? 0) != input.Version) throw new InvalidOperationException("目录已被修改，请刷新后重试。");
                if (old != null && (old.SubjectId != input.SubjectId || old.ProfileId != input.ProfileId)) throw new ArgumentException("不能跨科目或考试档案移动。");
                if (active && (old == null || old.ParentId != input.ParentId || old.Name != input.Name || old.Archived != input.Archived || old.SortOrder != input.SortOrder || old.Kind != input.Kind))
                    throw new InvalidOperationException("请先结束或核对活动计时再修改目录结构。");
                if (Scalar(c, tx, "SELECT 1 FROM exam_subjects WHERE profile_id=$p AND subject_id=$s", ("$p", input.ProfileId), ("$s", input.SubjectId)) == null)
                    throw new ArgumentException("节点科目不属于考试档案。");
                var node = input with { Name = input.Name.Trim(), Version = input.Version + 1 };
                after[node.Id] = node; saved.Add(node);
            }
            KnowledgeTree.Validate(after.Values.ToList());
            // Batch may refer to another newly created node: defer FK checks to commit.
            Exec(c, tx, "PRAGMA defer_foreign_keys=ON");
            foreach (var n in saved)
            {
                WriteKnowledgeNode(c, tx, n);
                Audit(c, tx, "knowledge-node", n.Id, n.Version, reason, before.TryGetValue(n.Id, out var old) ? Json(old) : null, Json(n));
            }
            tx.Commit(); return saved;
        }
    }
    public void SetLearningState(string id, long version, LearningState state)
    {
        lock (_gate)
        {
            var node = KnowledgeNodes().Single(n => n.Id == id);
            if (node.Version != version) throw new InvalidOperationException("目录已被修改，请刷新后重试。");
            var progress = state == LearningState.NotStarted ? 0 : state == LearningState.Learned ? 100 : state == LearningState.Learning ? Math.Min(99, node.Progress) : node.Progress;
            SaveKnowledgeNodes([node with { State = state, Progress = progress }], "学习状态修改");
        }
    }
    public long KnowledgeDuration(IEnumerable<string> roots, DateOnly from, DateOnly to, bool descendants = true)
    {
        lock (_gate)
        {
            var ids = descendants ? KnowledgeTree.Descendants(KnowledgeNodes(), roots) : roots.ToHashSet();
            return Records().Where(r => r.ChapterId != null && ids.Contains(r.ChapterId)).SelectMany(r => r.Parts)
                .Where(p => p.Date >= from && p.Date <= to).DistinctBy(p => p.Id).Sum(p => p.DurationMs);
        }
    }
}
