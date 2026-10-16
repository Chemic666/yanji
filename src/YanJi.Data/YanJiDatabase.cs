using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using YanJi.Core;

namespace YanJi.Data;

/// <summary>One local writer. Mutations are atomic; statistics query canonical parts.</summary>
public sealed partial class YanJiDatabase : IDisposable
{
    private const int ApplicationId = 0x594A4931;
    private const int SchemaVersion = 4;
    private readonly int _expectedVersion;
    private const string SchemaSql = """
                CREATE TABLE subjects(id TEXT PRIMARY KEY, name TEXT NOT NULL, archived INTEGER NOT NULL CHECK(archived IN(0,1)));
                CREATE TABLE chapters(id TEXT PRIMARY KEY, subject TEXT NOT NULL REFERENCES subjects(id), name TEXT NOT NULL,
                    progress INTEGER NOT NULL CHECK(progress BETWEEN 0 AND 100), mastery INTEGER NOT NULL CHECK(mastery BETWEEN 0 AND 3), archived INTEGER NOT NULL CHECK(archived IN(0,1)));
                CREATE TABLE sessions(id TEXT PRIMARY KEY, subject TEXT NOT NULL REFERENCES subjects(id), chapter TEXT REFERENCES chapters(id),
                    source INTEGER NOT NULL CHECK(source IN(0,1)), note TEXT NOT NULL, version INTEGER NOT NULL CHECK(version>0), deleted INTEGER NOT NULL CHECK(deleted IN(0,1)), created TEXT NOT NULL);
                CREATE TABLE parts(id TEXT PRIMARY KEY, session TEXT NOT NULL REFERENCES sessions(id) ON DELETE CASCADE, date TEXT NOT NULL,
                    duration INTEGER NOT NULL CHECK(duration>0), zone TEXT NOT NULL, kind INTEGER NOT NULL CHECK(kind IN(0,1)), start INTEGER, end INTEGER,
                    CHECK((kind=0 AND start IS NULL AND end IS NULL) OR (kind=1 AND start IS NOT NULL AND end IS NOT NULL AND end>start AND duration=end-start)));
                CREATE INDEX parts_date ON parts(date); CREATE INDEX parts_interval ON parts(start,end);
                CREATE TABLE revisions(id TEXT PRIMARY KEY, kind TEXT NOT NULL, entity TEXT NOT NULL, version INTEGER NOT NULL, at TEXT NOT NULL, reason TEXT NOT NULL, before_json TEXT, after_json TEXT);
                CREATE INDEX revisions_entity ON revisions(entity,at);
                CREATE TABLE active_timer(slot INTEGER PRIMARY KEY CHECK(slot=1), payload TEXT NOT NULL);
                CREATE TABLE tasks(id TEXT PRIMARY KEY, payload TEXT NOT NULL);
                CREATE TABLE weakpoints(id TEXT PRIMARY KEY, payload TEXT NOT NULL);
                CREATE TABLE reflections(id TEXT PRIMARY KEY, payload TEXT NOT NULL);
                CREATE TABLE settings(id TEXT PRIMARY KEY, payload TEXT NOT NULL);
                INSERT INTO subjects VALUES('math','数学',0),('english','英语',0),('408','408',0),('politics','政治',0);
                """;
    private readonly object _gate = new();
    internal Action<string>? RestoreCheckpoint {get;set;}
    public string DirectoryPath { get; }
    public string DatabasePath => Path.Combine(DirectoryPath, "yanji.sqlite");
    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
    private static T Parse<T>(string text) => JsonSerializer.Deserialize<T>(text) ?? throw new InvalidDataException("数据内容为空。");
    public YanJiDatabase(string directory) : this(directory, SchemaVersion) { }
    private YanJiDatabase(string directory, int expectedVersion)
    {
        _expectedVersion = expectedVersion;
        DirectoryPath = Path.GetFullPath(directory); Directory.CreateDirectory(DirectoryPath);
        var exists = File.Exists(DatabasePath) && new FileInfo(DatabasePath).Length > 0;
        using var c = Open();
        if (exists) {CheckIdentity(c, expectedVersion);CheckSchema(c);}
        else
        {
            using var tx = c.BeginTransaction();
            Exec(c, tx, $"PRAGMA application_id={ApplicationId}; PRAGMA user_version={expectedVersion};");Exec(c, tx, SchemaSql); if(expectedVersion>=2)ApplySchema2(c,tx); if(expectedVersion>=3)ApplySchema3(c,tx); if(expectedVersion>=4)ApplySchema4(c,tx); tx.Commit();
        }
        Exec(c, null, "PRAGMA journal_mode=WAL; PRAGMA synchronous=FULL;");
    }
    private SqliteConnection Open(string? path = null, bool readOnly = false)
    {
        var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path ?? DatabasePath,
            Pooling = false, Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWriteCreate, DefaultTimeout = 5 }.ToString());
        c.Open(); Exec(c, null, "PRAGMA foreign_keys=ON; PRAGMA synchronous=FULL;"); return c;
    }
    private static SqliteCommand Command(SqliteConnection c, SqliteTransaction? tx, string sql, params (string Key, object? Value)[] parameters)
    {
        var command = c.CreateCommand(); command.Transaction = tx; command.CommandText = sql;
        foreach (var (key, value) in parameters) command.Parameters.AddWithValue(key, value ?? DBNull.Value); return command;
    }
    private static void Exec(SqliteConnection c, SqliteTransaction? tx, string sql, params (string Key, object? Value)[] parameters)
    { using var command = Command(c, tx, sql, parameters); command.ExecuteNonQuery(); }
    private static object? Scalar(SqliteConnection c, SqliteTransaction? tx, string sql, params (string Key, object? Value)[] parameters)
    { using var command = Command(c, tx, sql, parameters); return command.ExecuteScalar(); }
    private static void CheckIdentity(SqliteConnection c, int version = SchemaVersion)
    {
        if (Convert.ToInt32(Scalar(c, null, "PRAGMA application_id"), CultureInfo.InvariantCulture) != ApplicationId ||
            Convert.ToInt32(Scalar(c, null, "PRAGMA user_version"), CultureInfo.InvariantCulture) != version)
            throw new InvalidDataException("数据库版本不兼容；旧版数据需经过备份与显式升级，当前未迁移。");
    }
    private static void CheckSchema(SqliteConnection c)
    {
        static List<string> Definitions(SqliteConnection connection)
        {
            using var cmd=Command(connection,null,"SELECT type||':'||name||':'||COALESCE(sql,'') FROM sqlite_master WHERE name NOT LIKE 'sqlite_%' ORDER BY type,name");
            using var r=cmd.ExecuteReader();List<string> result=[];while(r.Read())result.Add(System.Text.RegularExpressions.Regex.Replace(r.GetString(0),@"\s+"," "));return result;
        }
        using var expected=new SqliteConnection("Data Source=:memory:;Pooling=False");expected.Open();Exec(expected,null,SchemaSql);
        var version=Convert.ToInt32(Scalar(c,null,"PRAGMA user_version"));
        if(version is 2 or 3 or 4){using var tx=expected.BeginTransaction();ApplySchema2(expected,tx);if(version>=3)ApplySchema3(expected,tx);if(version>=4)ApplySchema4(expected,tx);tx.Commit();}
        else if(version!=1)throw new InvalidDataException("数据库Schema版本不兼容。");
        if(!Definitions(c).SequenceEqual(Definitions(expected)))throw new InvalidDataException("数据库表、索引或约束与 V0.1 不兼容。");
    }
    private static void Audit(SqliteConnection c, SqliteTransaction tx, string kind, string id, long version, string reason, string? before, string? after) =>
        Exec(c, tx, "INSERT INTO revisions VALUES($id,$kind,$entity,$version,$at,$reason,$before,$after)",
            ("$id", Guid.NewGuid().ToString("N")), ("$kind", kind), ("$entity", id), ("$version", version),
            ("$at", DateTimeOffset.UtcNow.ToString("O")), ("$reason", reason), ("$before", before), ("$after", after));
    public List<Subject> Subjects(bool archived = false)
    {
        lock (_gate) { using var c = Open(); using var cmd = Command(c, null, "SELECT id,name,archived FROM subjects WHERE $all=1 OR archived=0 ORDER BY rowid", ("$all", archived));
            using var r = cmd.ExecuteReader(); List<Subject> list=[]; while(r.Read()) list.Add(new(r.GetString(0),r.GetString(1),r.GetBoolean(2))); return list; }
    }
    public void SaveSubject(Subject subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject.Name);
        lock(_gate) { var before=Subjects(true).Find(x=>x.Id==subject.Id); using var c=Open(); using var tx=c.BeginTransaction();
            Exec(c,tx,"INSERT INTO subjects VALUES($id,$name,$archived) ON CONFLICT(id) DO UPDATE SET name=excluded.name,archived=excluded.archived",
                ("$id",subject.Id),("$name",subject.Name.Trim()),("$archived",subject.Archived));
            Exec(c,tx,"INSERT OR IGNORE INTO exam_subjects VALUES('legacy-default',$id)",("$id",subject.Id));
            Audit(c,tx,"subject",subject.Id,0,"科目修改",before==null?null:Json(before),Json(subject)); tx.Commit(); }
    }
    public List<Chapter> Chapters(string? subjectId = null, bool archived = false)
    {
        lock(_gate) { using var c=Open(); using var cmd=Command(c,null,"SELECT id,subject,name,progress,mastery,archived FROM chapters WHERE ($subject IS NULL OR subject=$subject) AND ($all=1 OR archived=0) ORDER BY rowid",("$subject",subjectId),("$all",archived));
            using var r=cmd.ExecuteReader(); List<Chapter> list=[]; while(r.Read())list.Add(new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetInt32(3),(Mastery)r.GetInt32(4),r.GetBoolean(5)));r.Close(); if(!archived){var map=CheckKnowledgeData(c).ToDictionary(n=>n.Id);bool Available(Chapter chapter){for(var node=map[chapter.Id];;node=map[node.ParentId!]){if(node.Archived)return false;if(node.ParentId==null)return true;}}list=list.Where(Available).ToList();}return list; }
    }
    public void SaveChapter(Chapter chapter)
    {
        lock(_gate)
        {
            var old=KnowledgeNodes().Find(n=>n.Id==chapter.Id);
            var state=old!=null&&old.Progress==chapter.Progress?old.State:chapter.Progress==0?LearningState.NotStarted:chapter.Progress==100?LearningState.Learned:LearningState.Learning;
            var node=old??new KnowledgeNode(chapter.Id,chapter.SubjectId,chapter.Name,SortOrder:KnowledgeNodes(chapter.SubjectId).Select(n=>n.SortOrder).DefaultIfEmpty(-1).Max()+1);
            if(node.SubjectId!=chapter.SubjectId)throw new ArgumentException("已有章节不能移动到另一科目。");
            SaveKnowledgeNodes([node with{Name=chapter.Name,Progress=chapter.Progress,Mastery=chapter.Mastery,Archived=chapter.Archived,State=state}],"章节修改");
        }
    }
    private static List<StudyRecord> ReadRecords(SqliteConnection c, SqliteTransaction? tx = null)
    {
        List<StudyRecord> records=[];
        var contexts=Convert.ToInt32(Scalar(c,tx,"PRAGMA user_version"))>=4?ReadStudyContexts(c,tx):new Dictionary<string,LearningContext>();
        using(var cmd=Command(c,tx,"SELECT s.id,s.subject,s.chapter,s.source,s.note,s.version,s.deleted,s.created,f.payload FROM sessions s LEFT JOIN settings f ON f.id='focus-session:'||s.id ORDER BY s.created DESC,s.rowid DESC"))
        using(var r=cmd.ExecuteReader()) while(r.Read()) records.Add(new(r.GetString(0),r.GetString(1),r.IsDBNull(2)?null:r.GetString(2),(SessionSource)r.GetInt32(3),r.GetString(4),[],r.GetInt64(5),r.GetBoolean(6),DateTimeOffset.Parse(r.GetString(7),CultureInfo.InvariantCulture),r.IsDBNull(8)?null:Parse<FocusContext>(Parse<string>(r.GetString(8))),contexts.GetValueOrDefault(r.GetString(0))));
        var map=records.ToDictionary(x=>x.Id);
        using(var cmd=Command(c,tx,"SELECT id,session,date,duration,zone,kind,start,end FROM parts ORDER BY date,start,rowid"))
        using(var r=cmd.ExecuteReader())while(r.Read())map[r.GetString(1)].Parts.Add(new(r.GetString(0),DateOnly.ParseExact(r.GetString(2),"yyyy-MM-dd",CultureInfo.InvariantCulture),r.GetInt64(3),r.GetString(4),(PartKind)r.GetInt32(5),r.IsDBNull(6)?null:r.GetInt64(6),r.IsDBNull(7)?null:r.GetInt64(7)));
        return records;
    }
    public List<StudyRecord> Records(DateOnly? date = null, bool includeDeleted = false)
    { lock(_gate) {using var c=Open();return ReadRecords(c).Where(x=>(includeDeleted||!x.Deleted)&&(date==null||x.Parts.Any(p=>p.Date==date))).ToList();} }
    private static void ValidateParts(StudyRecord record, bool allowEmpty)
    {
        record.Focus?.Validate();
        if(record.Focus!=null&&record.Source!=SessionSource.Timer)throw new ArgumentException("专注关联仅用于计时记录。");
        if(!Enum.IsDefined(record.Source)||string.IsNullOrWhiteSpace(record.Id)||(!allowEmpty&&record.Parts.Count==0))throw new ArgumentException("学习记录无效。");
        if(record.Parts.Select(p=>p.Id).Distinct().Count()!=record.Parts.Count)throw new ArgumentException("学习片段标识重复。");
        foreach(var p in record.Parts)
        {
            TimeParts.ResolveZone(p.ZoneId);
            if(p.DurationMs<=0||!Enum.IsDefined(p.Kind))throw new ArgumentException("时长必须大于零。");
            if(p.Kind==PartKind.DurationOnly && (p.StartUtcMs!=null||p.EndUtcMs!=null))throw new ArgumentException("纯时长不能伪造区间。");
            if(p.Kind==PartKind.Interval)
            {
                if(p.StartUtcMs==null||p.EndUtcMs==null||p.EndUtcMs<=p.StartUtcMs||p.DurationMs!=p.EndUtcMs-p.StartUtcMs)throw new ArgumentException("区间与时长不一致。");
                var split=TimeParts.Interval(DateTimeOffset.FromUnixTimeMilliseconds(p.StartUtcMs.Value),DateTimeOffset.FromUnixTimeMilliseconds(p.EndUtcMs.Value),p.ZoneId);
                if(split.Count!=1||split[0].Date!=p.Date)throw new ArgumentException("区间必须按实际日期分配。");
            }
        }
        for(var a=0;a<record.Parts.Count;a++)for(var b=a+1;b<record.Parts.Count;b++)
            if(TimeParts.Overlaps(record.Parts[a],record.Parts[b]))throw new InvalidOperationException("记录内部时间区间重叠。");
    }
    private static void ValidateRecord(SqliteConnection c, SqliteTransaction tx, StudyRecord record, bool allowEmpty=false)
    {
        ValidateParts(record,allowEmpty);
        if(record.Focus?.TaskId is string task&&Scalar(c,tx,"SELECT id FROM tasks WHERE id=$id",("$id",task))==null)throw new ArgumentException("关联任务不存在。");
        if(Scalar(c,tx,"SELECT id FROM subjects WHERE id=$id",("$id",record.SubjectId))==null)throw new ArgumentException("科目不存在。");
        if(record.ChapterId!=null && !Equals(Scalar(c,tx,"SELECT subject FROM chapters WHERE id=$id",("$id",record.ChapterId)),record.SubjectId))throw new ArgumentException("章节不属于所选科目。");
        if(record.Deleted)return;
        foreach(var p in record.Parts.Where(p=>p.Kind==PartKind.Interval))
            if(Scalar(c,tx,"SELECT p.id FROM parts p JOIN sessions s ON s.id=p.session WHERE s.deleted=0 AND s.id<>$id AND p.kind=1 AND p.start<$end AND p.end>$start LIMIT 1",("$id",record.Id),("$start",p.StartUtcMs),("$end",p.EndUtcMs))!=null)
                throw new InvalidOperationException("已知学习时间区间重叠，请修正已有记录或选择另一时间。");
    }
    private static void WriteRecord(SqliteConnection c, SqliteTransaction tx, StudyRecord record)
    {
        Exec(c,tx,"INSERT INTO sessions VALUES($id,$subject,$chapter,$source,$note,$version,$deleted,$created) ON CONFLICT(id) DO UPDATE SET subject=excluded.subject,chapter=excluded.chapter,source=excluded.source,note=excluded.note,version=excluded.version,deleted=excluded.deleted",
            ("$id",record.Id),("$subject",record.SubjectId),("$chapter",record.ChapterId),("$source",(int)record.Source),("$note",record.Note),("$version",record.Version),("$deleted",record.Deleted),("$created",(record.CreatedAt??DateTimeOffset.UtcNow).ToString("O")));
        Exec(c,tx,"DELETE FROM parts WHERE session=$id",("$id",record.Id));
        foreach(var p in record.Parts)Exec(c,tx,"INSERT INTO parts VALUES($id,$session,$date,$duration,$zone,$kind,$start,$end)",
            ("$id",p.Id),("$session",record.Id),("$date",p.Date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)),("$duration",p.DurationMs),("$zone",p.ZoneId),("$kind",(int)p.Kind),("$start",p.StartUtcMs),("$end",p.EndUtcMs));
        if(record.Context!=null)WriteStudyContext(c,tx,record.Id,record.Context);
        if(record.Focus!=null)Exec(c,tx,"INSERT INTO settings VALUES($id,$payload) ON CONFLICT(id) DO UPDATE SET payload=excluded.payload",("$id","focus-session:"+record.Id),("$payload",Json(Json(record.Focus))));
    }
    public StudyRecord SaveRecord(StudyRecord record, string reason)
    {
        lock(_gate) {using var c=Open();using var tx=c.BeginTransaction();
            if(Scalar(c,tx,"SELECT payload FROM active_timer") is string active && Parse<TimerSnapshot>(active).SessionId==record.Id)throw new InvalidOperationException("活动计时记录请先结束再修正。");
            var before=ReadRecords(c,tx).Find(x=>x.Id==record.Id);
            record=PrepareStudyContext(c,tx,record,before,ContextCapture.Manual);
            if(before!=null&&before.Focus!=record.Focus)throw new ArgumentException("历史修正须保留原计时模式与任务关联。");
            if(before!=null && record.Version==0 && before.Version==1 && Equivalent(before,record))return before;
            if((before?.Version??0)!=record.Version)throw new InvalidOperationException("记录已变化，请刷新后再修改。");
            ValidateRecord(c,tx,record);var after=record with {Version=record.Version+1,CreatedAt=before?.CreatedAt??DateTimeOffset.UtcNow};
            WriteRecord(c,tx,after);Audit(c,tx,"session",after.Id,after.Version,reason,before==null?null:Json(before),Json(after));tx.Commit();return after; }
    }
    private static bool Equivalent(StudyRecord a,StudyRecord b)=>Json(a with{Version=0,CreatedAt=null,Parts=a.Parts.OrderBy(p=>p.Id).ToList()})==Json(b with{Version=0,CreatedAt=null,Parts=b.Parts.OrderBy(p=>p.Id).ToList()});
    public void DeleteRecord(string id, long version) { var record=Records(null,true).Single(x=>x.Id==id);SaveRecord(record with{Version=version,Deleted=true},"删除（可追溯）"); }
    public void RestoreRecord(string id,long version) {var record=Records(null,true).Single(x=>x.Id==id);SaveRecord(record with{Version=version,Deleted=false},"恢复已删除记录");}
    public void ExcludePart(string id,string partId,long version)
    {
        lock(_gate){var record=Records(null,true).Single(x=>x.Id==id);
            if(record.Deleted)throw new InvalidOperationException("请先恢复整次学习记录。");
            if(record.Parts.Count<=1)throw new InvalidOperationException("最后一个片段请使用整次记录软删除，可完整恢复。");
            if(!record.Parts.Any(p=>p.Id==partId))throw new InvalidOperationException("片段已变化，请刷新。");
            SaveRecord(record with{Version=version,Parts=record.Parts.Where(p=>p.Id!=partId).ToList()},"排除学习片段 "+partId);}
    }
    public void RestorePart(string id,string partId,long version)
    {
        lock(_gate){var record=Records(null,true).Single(x=>x.Id==id);
            if(record.Deleted)throw new InvalidOperationException("请先恢复整次学习记录。");
            var part=ExcludedParts(id).SingleOrDefault(p=>p.Id==partId)??throw new InvalidOperationException("未找到可恢复的排除片段。");
            SaveRecord(record with{Version=version,Parts=record.Parts.Append(part).OrderBy(p=>p.Date).ThenBy(p=>p.StartUtcMs).ToList()},"恢复学习片段 "+partId);}
    }
    public List<StudyPart> ExcludedParts(string id)
    {
        lock(_gate){var record=Records(null,true).Single(x=>x.Id==id);var visited=new HashSet<string>();List<StudyPart> excluded=[];
            foreach(var revision in History(id).Where(r=>r.EntityKind=="session"&&r.BeforeJson!=null&&r.AfterJson!=null))
            {
                var removing=revision.Reason.StartsWith("排除学习片段 ",StringComparison.Ordinal);
                if(!removing&&!revision.Reason.StartsWith("恢复学习片段 ",StringComparison.Ordinal))continue;
                var before=Parse<StudyRecord>(revision.BeforeJson!);var after=Parse<StudyRecord>(revision.AfterJson!);
                var changed=removing?before.Parts.Where(p=>!after.Parts.Any(a=>a.Id==p.Id)):after.Parts.Where(p=>!before.Parts.Any(a=>a.Id==p.Id));
                foreach(var part in changed)if(visited.Add(part.Id)&&removing&&!record.Parts.Any(p=>p.Id==part.Id))excluded.Add(part);
            }
            return excluded.OrderBy(p=>p.Date).ThenBy(p=>p.StartUtcMs).ToList();}
    }
    public List<Revision> History(string id)
    {
        lock(_gate){using var c=Open();using var cmd=Command(c,null,"SELECT id,kind,entity,version,at,reason,before_json,after_json FROM revisions WHERE entity=$id ORDER BY rowid DESC",("$id",id));
            using var r=cmd.ExecuteReader();List<Revision> list=[];while(r.Read())list.Add(new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetInt64(3),DateTimeOffset.Parse(r.GetString(4),CultureInfo.InvariantCulture),r.GetString(5),r.IsDBNull(6)?null:r.GetString(6),r.IsDBNull(7)?null:r.GetString(7)));return list;}
    }
    public List<DailyTotal> DailyTotals(DateOnly from,DateOnly to)
    {
        lock(_gate){using var c=Open();using var cmd=Command(c,null,"SELECT p.date,SUM(p.duration) FROM parts p JOIN sessions s ON s.id=p.session WHERE s.deleted=0 AND p.date BETWEEN $from AND $to GROUP BY p.date ORDER BY p.date",("$from",from.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)),("$to",to.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)));
            using var r=cmd.ExecuteReader();List<DailyTotal> list=[];while(r.Read())list.Add(new(DateOnly.Parse(r.GetString(0),CultureInfo.InvariantCulture),r.GetInt64(1)));return list;}
    }
    public List<SubjectTotal> SubjectTotals(DateOnly from,DateOnly to)=>Records().SelectMany(s=>s.Parts.Where(p=>p.Date>=from&&p.Date<=to).Select(p=>(s.SubjectId,p.DurationMs)))
        .GroupBy(x=>x.SubjectId).Select(g=>new SubjectTotal(g.Key,Subjects(true).Find(s=>s.Id==g.Key)?.Name??g.Key,g.Sum(x=>x.DurationMs))).ToList();
    public TimerSnapshot SaveTimer(TimerSnapshot snapshot,string? reason=null)
    {
        lock(_gate){using var c=Open();using var tx=c.BeginTransaction();var active=Scalar(c,tx,"SELECT payload FROM active_timer") as string;
            if(active!=null&&Parse<TimerSnapshot>(active).SessionId!=snapshot.SessionId)throw new InvalidOperationException("已有另一活动计时。");
            var before=ReadRecords(c,tx).Find(x=>x.Id==snapshot.SessionId);
            if(before!=null&&(before.SubjectId!=snapshot.SubjectId||before.ChapterId!=snapshot.ChapterId||snapshot.Context!=null&&Json(snapshot.Context)!=Json(before.Context)))throw new ArgumentException("活动计时的学习上下文不能被替换。");
            var requestedRecord=new StudyRecord(snapshot.SessionId,snapshot.SubjectId,snapshot.ChapterId,SessionSource.Timer,snapshot.Note,snapshot.Parts,Focus:snapshot.Focus,Context:snapshot.Context);
            var prepared=PrepareStudyContext(c,tx,requestedRecord,before,ContextCapture.Started,snapshot.LastSample?.Utc);snapshot=snapshot with{Context=prepared.Context};
            if(before!=null&&before.Focus!=snapshot.Focus)throw new ArgumentException("活动计时的模式与任务关联不能被替换。");
            if(active!=null)
            {var saved=Parse<TimerSnapshot>(active);if(Json(saved)==Json(snapshot with{Version=saved.Version}))return saved;}
            if(before!=null && active==null)
            {if(snapshot.State==TimerState.Completed&&Equivalent(before,new(snapshot.SessionId,snapshot.SubjectId,snapshot.ChapterId,SessionSource.Timer,snapshot.Note,snapshot.Parts,Focus:snapshot.Focus,Context:snapshot.Context)))return snapshot with{Version=before.Version};
                throw new InvalidOperationException("该计时已结束，不能重新覆盖。");}
            if((before?.Version??0)!=snapshot.Version)throw new InvalidOperationException("计时检查点版本过期。");
            var record=new StudyRecord(snapshot.SessionId,snapshot.SubjectId,snapshot.ChapterId,SessionSource.Timer,snapshot.Note,[..snapshot.Parts],(before?.Version??0)+1,false,before?.CreatedAt??DateTimeOffset.UtcNow,snapshot.Focus,snapshot.Context);
            ValidateRecord(c,tx,record,true);WriteRecord(c,tx,record);
            if(reason!=null)Audit(c,tx,"session",record.Id,record.Version,reason,before==null?null:Json(before),Json(record));
            if(snapshot.IsActive)Exec(c,tx,"INSERT INTO active_timer VALUES(1,$payload) ON CONFLICT(slot) DO UPDATE SET payload=excluded.payload",("$payload",Json(snapshot with{Version=record.Version})));
            else Exec(c,tx,"DELETE FROM active_timer");LearningCheckpoint?.Invoke("timer-before-commit");tx.Commit();return snapshot with{Version=record.Version};}
    }
    public TimerSnapshot? LoadTimer(){lock(_gate){using var c=Open();return Scalar(c,null,"SELECT payload FROM active_timer") is string text?Parse<TimerSnapshot>(text):null;}}
    private List<T> Entities<T>(string table)
    {lock(_gate){using var c=Open();using var cmd=Command(c,null,$"SELECT payload FROM {table} ORDER BY rowid DESC");using var r=cmd.ExecuteReader();List<T> list=[];while(r.Read())list.Add(Parse<T>(r.GetString(0)));return list;}}
    private void SaveEntity<T>(string table,string id,T value,string reason,Action<SqliteConnection,SqliteTransaction>? validate=null)
    {lock(_gate){using var c=Open();using var tx=c.BeginTransaction();var before=Scalar(c,tx,$"SELECT payload FROM {table} WHERE id=$id",("$id",id)) as string;
        validate?.Invoke(c,tx);var after=Json(value);if(before==after)return;Exec(c,tx,$"INSERT INTO {table} VALUES($id,$payload) ON CONFLICT(id) DO UPDATE SET payload=excluded.payload",("$id",id),("$payload",after));
        Audit(c,tx,table,id,0,reason,before,after);tx.Commit();}}
    public List<LearningTask> Tasks()=>Entities<LearningTask>("tasks");
    public void SaveTask(LearningTask task){ArgumentException.ThrowIfNullOrWhiteSpace(task.Title);if(task.SubjectId!=null&&!Subjects(true).Any(s=>s.Id==task.SubjectId))throw new ArgumentException("任务科目不存在。");SaveEntity("tasks",task.Id,task,"任务修改",(c,tx)=>ValidateLearningTarget(c,tx,task.ProfileId,task.SubjectId,task.ChapterId));}
    public List<WeakPoint> WeakPoints()=>Entities<WeakPoint>("weakpoints");
    public void SaveWeakPoint(WeakPoint point)
    {ArgumentException.ThrowIfNullOrWhiteSpace(point.Title);if(!Enum.IsDefined(point.State))throw new ArgumentException("薄弱点状态无效。");
        if(point.SubjectId!=null&&!Subjects(true).Any(s=>s.Id==point.SubjectId))throw new ArgumentException("科目不存在。");
        if(point.ChapterId!=null&&!Chapters(null,true).Any(s=>s.Id==point.ChapterId&&s.SubjectId==point.SubjectId))throw new ArgumentException("章节不属于该科目。");SaveEntity("weakpoints",point.Id,point,"薄弱点修改",(c,tx)=>ValidateLearningTarget(c,tx,point.ProfileId,point.SubjectId,point.ChapterId));}
    public string Reflection(DateOnly date)=>Entities<ReflectionEntry>("reflections").Find(x=>x.Date==date)?.Text??"";
    public Dictionary<DateOnly,string> Reflections()=>Entities<ReflectionEntry>("reflections").ToDictionary(r=>r.Date,r=>r.Text);
    public void SaveReflection(DateOnly date,string text)=>SaveEntity("reflections",date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),new ReflectionEntry(date,text),"每日感想修改");
    public string Setting(string key,string fallback)
    {lock(_gate){using var c=Open();return Scalar(c,null,"SELECT payload FROM settings WHERE id=$id",("$id",key)) is string text?Parse<string>(text):fallback;}}
    public void SaveSetting(string key,string value)=>SaveEntity("settings",key,value,"设置修改");
    private const string ReviewPrefix="period-review:v1:";
    private static void ValidateReview(PeriodReview item,string id,bool stored)
    {
        if(item.Format!=1||item.Id!=id||string.IsNullOrWhiteSpace(id)||id.Length>100||id.Any(c=>!char.IsLetterOrDigit(c)&&c!='-'&&c!='_')||
            item.To<item.From||item.To.DayNumber-item.From.DayNumber>3660||string.IsNullOrWhiteSpace(item.Body)||item.Body.Length>20000||
            item.Version<0||stored&&(item.Version==0||item.CreatedAt==null||item.UpdatedAt==null))
            throw new ArgumentException("阶段复盘字段或版本无效（正文 1–20000 字，范围最多十年）。");
    }
    public List<PeriodReview> PeriodReviews(bool includeDeleted=false)
    {
        return Settings().Where(p=>p.Key.StartsWith(ReviewPrefix,StringComparison.Ordinal)).Select(p=>
        {var item=Parse<PeriodReview>(p.Value);ValidateReview(item,p.Key[ReviewPrefix.Length..],true);return item;})
            .Where(p=>includeDeleted||!p.Deleted).OrderByDescending(p=>p.To).ThenByDescending(p=>p.UpdatedAt).ToList();
    }
    public PeriodReview SavePeriodReview(PeriodReview draft)
    {
        ValidateReview(draft,draft.Id,false);
        lock(_gate)
        {
            using var c=Open();using var tx=c.BeginTransaction();var key=ReviewPrefix+draft.Id;
            var raw=Scalar(c,tx,"SELECT payload FROM settings WHERE id=$id",("$id",key)) as string;
            var before=raw==null?null:Parse<PeriodReview>(Parse<string>(raw));
            if(before!=null)ValidateReview(before,draft.Id,true);
            if(draft.Version!=(before?.Version??0))throw new InvalidOperationException("阶段复盘已改变，请刷新后再编辑。");
            if(draft.SubjectId!=null&&Scalar(c,tx,"SELECT id FROM subjects WHERE id=$id",("$id",draft.SubjectId))==null)throw new ArgumentException("复盘科目不存在。");
            var now=DateTimeOffset.UtcNow;var saved=draft with{Version=draft.Version+1,CreatedAt=before?.CreatedAt??now,UpdatedAt=now};
            Exec(c,tx,"INSERT INTO settings VALUES($id,$payload) ON CONFLICT(id) DO UPDATE SET payload=excluded.payload",("$id",key),("$payload",Json(Json(saved))));
            Audit(c,tx,"period-review",saved.Id,saved.Version,saved.Deleted?"阶段复盘软删除":before?.Deleted==true?"阶段复盘恢复":"阶段复盘保存",before==null?null:Json(before),Json(saved));
            tx.Commit();return saved;
        }
    }
    public void SetPeriodReviewDeleted(string id,long version,bool deleted)
    {var item=PeriodReviews(true).Single(p=>p.Id==id);SavePeriodReview(item with{Version=version,Deleted=deleted});}
    public Dictionary<string,string> Settings()
    {lock(_gate){using var c=Open();using var cmd=Command(c,null,"SELECT id,payload FROM settings");using var r=cmd.ExecuteReader();Dictionary<string,string> result=[];while(r.Read())result.Add(r.GetString(0),Parse<string>(r.GetString(1)));return result;}}
    public string Integrity(){lock(_gate){using var c=Open();return (string)Scalar(c,null,"PRAGMA integrity_check")!;}}
    public Dictionary<string,string> Diagnostics()
    {lock(_gate){using var c=Open();return new(){["sqlite"]=Convert.ToString(Scalar(c,null,"SELECT sqlite_version()"))!,["journal_mode"]=Convert.ToString(Scalar(c,null,"PRAGMA journal_mode"))!,["foreign_keys"]=Convert.ToString(Scalar(c,null,"PRAGMA foreign_keys"))!,["synchronous"]=Convert.ToString(Scalar(c,null,"PRAGMA synchronous"))!};}}
    private sealed record ReflectionEntry(DateOnly Date,string Text);
    private sealed record BackupManifest(int Format,int Application,int Schema,string Sha256,long Sessions,long DurationMs,DateTimeOffset CreatedAt,long? KnowledgeNodes=null,long? Profiles=null);
    private static string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static (long Count,long Duration) Inventory(SqliteConnection c)=>
        (Convert.ToInt64(Scalar(c,null,"SELECT COUNT(*) FROM sessions")),Convert.ToInt64(Scalar(c,null,"SELECT COALESCE(SUM(duration),0) FROM parts")));
    public void Backup(string path)
    {
        lock(_gate){if(File.Exists(path))throw new IOException("备份路径已存在，请选择新文件。");
            var scratch=Path.Combine(DirectoryPath,"work","backup-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(scratch);
            try{var snapshot=Path.Combine(scratch,"database.sqlite");
                using(var source=Open())using(var target=Open(snapshot)){source.BackupDatabase(target);Exec(target,null,"PRAGMA journal_mode=DELETE;");}
                using(var c=Open(snapshot,true)){CheckIdentity(c,_expectedVersion);CheckIntegrity(c);var inventory=Inventory(c);
                    var manifest=new BackupManifest(_expectedVersion,ApplicationId,_expectedVersion,Hash(snapshot),inventory.Count,inventory.Duration,DateTimeOffset.UtcNow, _expectedVersion>=2?Convert.ToInt64(Scalar(c,null,"SELECT COUNT(*) FROM chapters")):null,_expectedVersion>=2?Convert.ToInt64(Scalar(c,null,"SELECT COUNT(*) FROM exam_profiles")):null);
                    var staging=Path.GetFullPath(path)+"."+Guid.NewGuid().ToString("N")+".partial";
                    try
                    {
                        using(var output=new FileStream(staging,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                        {using(var zip=new ZipArchive(output,ZipArchiveMode.Create,true))
                            {zip.CreateEntryFromFile(snapshot,"database.sqlite",CompressionLevel.Optimal);using var w=new StreamWriter(zip.CreateEntry("manifest.json").Open(),new UTF8Encoding(false));w.Write(Json(manifest));}
                            output.Flush(true);}
                        ValidateBackup(staging);File.Move(staging,path);
                    }
                    finally {if(File.Exists(staging))File.Delete(staging);}
                }
            }finally{Directory.Delete(scratch,true);}}
    }
    private static void CheckIntegrity(SqliteConnection c)
    {
        if(!Equals(Scalar(c,null,"PRAGMA integrity_check"),"ok"))throw new InvalidDataException("数据库完整性检查失败。");
        using var command=Command(c,null,"PRAGMA foreign_key_check");using var reader=command.ExecuteReader();if(reader.Read())throw new InvalidDataException("数据库引用完整性检查失败。");
    }
    private static void CheckAllData(SqliteConnection c)
    {
        CheckSchema(c);
        var expected=new Dictionary<string,string[]>
        {
            ["subjects"]=["id","name","archived"], ["chapters"]=["id","subject","name","progress","mastery","archived"],
            ["sessions"]=["id","subject","chapter","source","note","version","deleted","created"],
            ["parts"]=["id","session","date","duration","zone","kind","start","end"],
            ["revisions"]=["id","kind","entity","version","at","reason","before_json","after_json"],
            ["active_timer"]=["slot","payload"], ["tasks"]=["id","payload"], ["weakpoints"]=["id","payload"],
            ["reflections"]=["id","payload"], ["settings"]=["id","payload"]
        };
        var schema=Convert.ToInt32(Scalar(c,null,"PRAGMA user_version"));
        if(schema>=2){expected["chapters"]=["id","subject","name","progress","mastery","archived","profile_id","parent_id","node_kind","description","sort_order","learning_state","row_version"];CheckKnowledgeData(c);}
        if(schema>=3)CheckPackageData(c);
        if(schema>=4)CheckLearningData(c);
        foreach(var (table,columns) in expected)
        {
            using var cmd=Command(c,null,$"PRAGMA table_info({table})");using var r=cmd.ExecuteReader();List<string> actual=[];
            while(r.Read())actual.Add(r.GetString(1));if(!actual.SequenceEqual(columns))throw new InvalidDataException("备份表结构不兼容："+table);
        }
        if(Scalar(c,null,"SELECT id FROM subjects WHERE length(trim(name))=0 OR archived NOT IN(0,1) LIMIT 1")!=null ||
            Scalar(c,null,"SELECT id FROM chapters WHERE length(trim(name))=0 OR progress NOT BETWEEN 0 AND 100 OR mastery NOT BETWEEN 0 AND 3 OR archived NOT IN(0,1) LIMIT 1")!=null)
            throw new InvalidDataException("科目或章节状态无效。");
        var records=ReadRecords(c);foreach(var record in records)
        {ValidateParts(record,true);if(record.Version<=0)throw new InvalidDataException("记录版本无效。");
            if(record.ChapterId!=null&&!Equals(Scalar(c,null,"SELECT subject FROM chapters WHERE id=$id",("$id",record.ChapterId)),record.SubjectId))throw new InvalidDataException("章节引用无效。");}
        var known=records.Where(x=>!x.Deleted).SelectMany(x=>x.Parts).Where(p=>p.Kind==PartKind.Interval).OrderBy(p=>p.StartUtcMs).ToList();
        for(var i=1;i<known.Count;i++)if(TimeParts.Overlaps(known[i-1],known[i]))throw new InvalidDataException("备份包含重叠时间区间。");
        foreach(var table in new[]{"tasks","weakpoints","reflections","settings"})
        {
            using var cmd=Command(c,null,$"SELECT id,payload FROM {table}");using var r=cmd.ExecuteReader();
            while(r.Read())
            {
                var id=r.GetString(0);var payload=r.GetString(1);
                if(table=="tasks"){var item=Parse<LearningTask>(payload);if(item.Id!=id||string.IsNullOrWhiteSpace(item.Title)||item.SubjectId!=null&&Scalar(c,null,"SELECT id FROM subjects WHERE id=$id",("$id",item.SubjectId))==null)throw new InvalidDataException("任务内容无效。");}
                if(table=="weakpoints"){var item=Parse<WeakPoint>(payload);if(item.Id!=id||string.IsNullOrWhiteSpace(item.Title)||!Enum.IsDefined(item.State)||item.SubjectId!=null&&Scalar(c,null,"SELECT id FROM subjects WHERE id=$id",("$id",item.SubjectId))==null||item.ChapterId!=null&&!Equals(Scalar(c,null,"SELECT subject FROM chapters WHERE id=$id",("$id",item.ChapterId)),item.SubjectId))throw new InvalidDataException("薄弱点内容无效。");}
                if(table=="reflections"&&Parse<ReflectionEntry>(payload).Date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)!=id)throw new InvalidDataException("感想日期无效。");
                if(table=="settings")
                {var value=Parse<string>(payload);if(id.StartsWith("focus-session:",StringComparison.Ordinal)){var focus=Parse<FocusContext>(value);focus.Validate();var record=records.Find(x=>x.Id==id[14..]);if(record==null||record.Source!=SessionSource.Timer||record.Focus!=focus||focus.TaskId is string task&&Scalar(c,null,"SELECT id FROM tasks WHERE id=$id",("$id",task))==null)throw new InvalidDataException("专注关联无效。");}if(id.StartsWith(ReviewPrefix,StringComparison.Ordinal)){var item=Parse<PeriodReview>(value);ValidateReview(item,id[ReviewPrefix.Length..],true);if(item.SubjectId!=null&&Scalar(c,null,"SELECT id FROM subjects WHERE id=$id",("$id",item.SubjectId))==null)throw new InvalidDataException("复盘科目无效。");}if(id=="zone")TimeParts.ResolveZone(value);
                    if(id.StartsWith(ProgressNotePrefix,StringComparison.Ordinal))CheckStoredProgressNote(c,Parse<NodeProgressNote>(value),id[ProgressNotePrefix.Length..],schema);
                    if(id=="exam-date"&&value.Length>0)DateOnly.ParseExact(value,"yyyy-MM-dd",CultureInfo.InvariantCulture);
                    if(id=="checkpoint-seconds"&&(!int.TryParse(value,out var seconds)||seconds<2||seconds>60))throw new InvalidDataException("检查点偏好无效。");}
            }
        }
        using(var cmd=Command(c,null,"SELECT at,before_json,after_json,kind FROM revisions"))using(var r=cmd.ExecuteReader())
            while(r.Read()){DateTimeOffset.Parse(r.GetString(0),CultureInfo.InvariantCulture);for(var i=1;i<=2;i++)if(!r.IsDBNull(i)){using var document=JsonDocument.Parse(r.GetString(i));if(r.GetString(3)=="session")ValidateParts(Parse<StudyRecord>(r.GetString(i)),true);if(r.GetString(3)=="period-review"){var review=Parse<PeriodReview>(r.GetString(i));ValidateReview(review,review.Id,true);}}}
        CheckProgressNoteAudits(c,schema);
        if(Scalar(c,null,"SELECT payload FROM active_timer") is string timerJson)
        {
            var timer=Parse<TimerSnapshot>(timerJson);TimeParts.ResolveZone(timer.ZoneId);var current=records.Find(x=>x.Id==timer.SessionId);
            if(!timer.IsActive||current==null||current.Deleted||current.Source!=SessionSource.Timer||current.Version!=timer.Version||current.SubjectId!=timer.SubjectId||current.ChapterId!=timer.ChapterId||current.Note!=timer.Note||current.Focus!=timer.Focus||schema>=4&&Json(current.Context)!=Json(timer.Context)||Json(current.Parts.OrderBy(p=>p.Id))!=Json(timer.Parts.OrderBy(p=>p.Id))||timer.LastSample==null||timer.TrustedEndUtcMs==null)
                throw new InvalidDataException("活动计时与学习记录不一致。");
        }
    }
    public void ValidateBackup(string path)
    {
        try{ValidateBackupContent(path);}
        catch(Exception exception) when(exception is JsonException or FormatException or ArgumentException or
            TimeZoneNotFoundException or InvalidTimeZoneException or InvalidCastException or OverflowException ||
            exception is SqliteException{SqliteErrorCode:1 or 11 or 19 or 20 or 26})
        {throw new InvalidDataException("备份内容或结构无效，当前数据未被修改。",exception);}
    }
    private void ValidateBackupContent(string path)
    {
        lock(_gate)
        {
            var scratch=Path.Combine(DirectoryPath,"work","validate-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(scratch);
            try
            {
                using var zip=ZipFile.OpenRead(path);
                if(zip.Entries.Count!=2||!zip.Entries.Select(e=>e.FullName).Order().SequenceEqual(new[]{"database.sqlite","manifest.json"}))throw new InvalidDataException("备份文件结构无效。");
                var db=zip.GetEntry("database.sqlite")!;var meta=zip.GetEntry("manifest.json")!;
                if(db.Length>512L*1024*1024||meta.Length>65536)throw new InvalidDataException("备份超过校验大小限制。");
                BackupManifest manifest;using(var r=new StreamReader(meta.Open()))manifest=Parse<BackupManifest>(r.ReadToEnd());
                var candidate=Path.Combine(scratch,"database.sqlite");db.ExtractToFile(candidate);
                if(manifest.Format!=manifest.Schema||manifest.Schema is not(1 or 2 or 3 or 4)||manifest.Application!=ApplicationId||manifest.Sha256!=Hash(candidate))throw new InvalidDataException("备份版本或校验值无效。");
                using var c=Open(candidate,true);CheckIdentity(c,manifest.Schema);CheckIntegrity(c);CheckAllData(c);var inventory=Inventory(c);
                if(inventory.Count!=manifest.Sessions||inventory.Duration!=manifest.DurationMs)throw new InvalidDataException("备份记录总量校验失败。");
                if(manifest.Schema>=2&&(manifest.KnowledgeNodes!=Convert.ToInt64(Scalar(c,null,"SELECT COUNT(*) FROM chapters"))||manifest.Profiles!=Convert.ToInt64(Scalar(c,null,"SELECT COUNT(*) FROM exam_profiles"))))throw new InvalidDataException("备份知识目录总量校验失败。");
            }
            finally{Directory.Delete(scratch,true);}
        }
    }
    public void RestoreBackup(string path)
    {
        lock(_gate){if(LoadTimer()!=null)throw new InvalidOperationException("请先结束活动计时再恢复备份。");ValidateBackup(path);
            var scratch=Path.Combine(DirectoryPath,"work","restore-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(scratch);
            try{var candidate=Path.Combine(scratch,"database.sqlite");BackupManifest manifest;
                using(var zip=ZipFile.OpenRead(path))
                {
                    if(zip.Entries.Count!=2||!zip.Entries.Select(e=>e.FullName).Order().SequenceEqual(new[]{"database.sqlite","manifest.json"}))throw new InvalidDataException("备份文件结构无效。");
                    var db=zip.GetEntry("database.sqlite")!;var meta=zip.GetEntry("manifest.json")!;
                    if(db.Length>512*1024*1024||meta.Length>65536)throw new InvalidDataException("备份超过 V0.1 校验大小限制。");
                    using(var r=new StreamReader(meta.Open()))manifest=Parse<BackupManifest>(r.ReadToEnd());db.ExtractToFile(candidate);
                }
                if(manifest.Format!=manifest.Schema||manifest.Schema is not(1 or 2 or 3 or 4)||manifest.Application!=ApplicationId||manifest.Sha256!=Hash(candidate))throw new InvalidDataException("备份版本或校验值无效。");
                using(var c=Open(candidate,true))
                {CheckIdentity(c,manifest.Schema);CheckIntegrity(c);CheckAllData(c);var inventory=Inventory(c);if(inventory.Count!=manifest.Sessions||inventory.Duration!=manifest.DurationMs)throw new InvalidDataException("备份记录总量校验失败。");
                if(manifest.Schema>=2&&(manifest.KnowledgeNodes!=Convert.ToInt64(Scalar(c,null,"SELECT COUNT(*) FROM chapters"))||manifest.Profiles!=Convert.ToInt64(Scalar(c,null,"SELECT COUNT(*) FROM exam_profiles"))))throw new InvalidDataException("备份知识目录总量校验失败。");
                    var records=ReadRecords(c);foreach(var record in records)ValidateParts(record,true);
                    var known=records.Where(x=>!x.Deleted).SelectMany(x=>x.Parts).Where(p=>p.Kind==PartKind.Interval).OrderBy(p=>p.StartUtcMs).ToList();
                    for(var i=1;i<known.Count;i++)if(TimeParts.Overlaps(known[i-1],known[i]))throw new InvalidDataException("备份包含重叠时间区间。");
                    foreach(var record in records)if(record.ChapterId!=null&&!Equals(Scalar(c,null,"SELECT subject FROM chapters WHERE id=$id",("$id",record.ChapterId)),record.SubjectId))throw new InvalidDataException("备份章节引用无效。");
                }
                if(manifest.Schema<4){using var legacy=Open(candidate);using var tx=legacy.BeginTransaction();if(manifest.Schema==1)ApplySchema2(legacy,tx);if(manifest.Schema<3)ApplySchema3(legacy,tx);ApplySchema4(legacy,tx);tx.Commit();CheckIdentity(legacy);CheckAllData(legacy);}
                var backups=Path.Combine(DirectoryPath,"backups");Directory.CreateDirectory(backups);
                var stamp=DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss",CultureInfo.InvariantCulture)+"-"+Guid.NewGuid().ToString("N")[..8];
                Backup(Path.Combine(backups,"before-restore-"+stamp+".yanjibak"));
                using(var c=Open())CheckpointBeforeReplace(c);
                var rollback=Path.Combine(backups,"before-restore-"+stamp+".sqlite");
                RestoreCheckpoint?.Invoke("before-replace");
                File.Replace(candidate,DatabasePath,rollback,true);
                try{RestoreCheckpoint?.Invoke("after-replace");using var c=Open();CheckIdentity(c);CheckIntegrity(c);CheckAllData(c);Exec(c,null,"PRAGMA journal_mode=WAL;");_knowledgeEpoch++;}
                catch{File.Replace(rollback,DatabasePath,Path.Combine(backups,"failed-restore-"+stamp+".sqlite"),true);throw;}
            }finally{Directory.Delete(scratch,true);}}
    }
    public string AutoBackup()
    {
        var folder=Path.Combine(DirectoryPath,"backups");Directory.CreateDirectory(folder);
        var path=Path.Combine(folder,"daily-"+DateTime.Today.ToString("yyyyMMdd",CultureInfo.InvariantCulture)+".yanjibak");
        if(File.Exists(path)){try{ValidateBackup(path);return path;}catch(InvalidDataException){path=Path.Combine(folder,"daily-"+DateTime.Today.ToString("yyyyMMdd",CultureInfo.InvariantCulture)+"-"+Guid.NewGuid().ToString("N")[..8]+".yanjibak");}}
        Backup(path);return path;
    }
    public void ExportCsv(string path)
    {
        if(File.Exists(path))throw new IOException("导出文件已存在，请另选文件名。");
        static string Cell(string value){if(value.StartsWith('=')||value.StartsWith('+')||value.StartsWith('-')||value.StartsWith('@'))value="'"+value;return "\""+value.Replace("\"","\"\"")+"\"";}
        lock(_gate){using var writer=new StreamWriter(new FileStream(path,FileMode.CreateNew,FileAccess.Write),new UTF8Encoding(true));writer.WriteLine("记录ID,日期,科目,章节,来源,分钟,时间类型,开始UTC,结束UTC,备注,精确毫秒,显示整数秒");
            var subjects=Subjects(true).ToDictionary(x=>x.Id,x=>x.Name);var chapters=Chapters(null,true).ToDictionary(x=>x.Id,x=>x.Name);
            foreach(var record in Records())foreach(var p in record.Parts)writer.WriteLine(string.Join(',',new[]{record.Id,p.Date.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),subjects[record.SubjectId],record.ChapterId==null?"":chapters[record.ChapterId],record.Source.ToString(),(p.DurationMs/60000d).ToString("0.###",CultureInfo.InvariantCulture),p.Kind.ToString(),p.StartUtcMs is long start?DateTimeOffset.FromUnixTimeMilliseconds(start).ToString("O"):"",p.EndUtcMs is long end?DateTimeOffset.FromUnixTimeMilliseconds(end).ToString("O"):"",record.Note,p.DurationMs.ToString(CultureInfo.InvariantCulture),(p.DurationMs/1000).ToString(CultureInfo.InvariantCulture)}.Select(Cell)));}
    }
    public void Dispose() { /* Connections are short-lived and never pooled. */ }
}
