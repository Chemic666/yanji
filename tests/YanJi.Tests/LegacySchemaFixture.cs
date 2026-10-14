using System.Text.Json;
using Microsoft.Data.Sqlite;
using YanJi.Core;
namespace YanJi.Tests;
internal static class LegacySchemaFixture
{
    internal const string Sql = """
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
    internal static string Create(bool active=false)
    {
        var root=M1TestDirectories.NewDirectory();Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root,"m1-isolated-fixture.txt"),"YanJi M1 synthetic");
        using var c=new SqliteConnection($"Data Source={Path.Combine(root,"yanji.sqlite")};Pooling=False");c.Open();
        using var cmd=c.CreateCommand();cmd.CommandText=Sql+"PRAGMA application_id=1498040625; PRAGMA user_version=1;";cmd.ExecuteNonQuery();
        cmd.CommandText="INSERT INTO chapters VALUES('old-chapter','math','旧中文章节',35,1,0),('old-archived','english','历史归档',100,3,1)";cmd.ExecuteNonQuery();
        cmd.CommandText="INSERT INTO sessions VALUES('old-session','math','old-chapter',0,'原备注',1,0,'2026-10-14T00:00:00.0000000+00:00'); INSERT INTO parts VALUES('old-part','old-session','2026-10-14',12345,'Asia/Shanghai',0,NULL,NULL)";cmd.ExecuteNonQuery();
        cmd.CommandText="INSERT INTO tasks VALUES('old-task',$task); INSERT INTO weakpoints VALUES('old-weak',$weak); INSERT INTO settings VALUES('exam-date',$date)";
        cmd.Parameters.AddWithValue("$task",JsonSerializer.Serialize(new LearningTask("old-task","旧任务",SubjectId:"math")));
        cmd.Parameters.AddWithValue("$weak",JsonSerializer.Serialize(new WeakPoint("old-weak","旧薄弱点",SubjectId:"math",ChapterId:"old-chapter")));
        cmd.Parameters.AddWithValue("$date",JsonSerializer.Serialize("2027-12-18"));cmd.ExecuteNonQuery();
        cmd.Parameters.Clear();
        var focus = new FocusContext(FocusMode.Countdown, 120000, TaskId: "old-task", TaskTitle: "旧任务");
        var split = TimeParts.Interval(DateTimeOffset.Parse("2026-10-13T15:59:59.500Z"), DateTimeOffset.Parse("2026-10-13T16:00:00.750Z"), "Asia/Shanghai");
        cmd.CommandText = "INSERT INTO sessions VALUES('old-deleted-timer','math','old-chapter',1,'跨日历史',2,1,'2026-10-13T15:59:59Z'); INSERT INTO settings VALUES('focus-session:old-deleted-timer',$focus)";
        cmd.Parameters.AddWithValue("$focus", JsonSerializer.Serialize(JsonSerializer.Serialize(focus))); cmd.ExecuteNonQuery();
        foreach (var part in split)
        {
            cmd.Parameters.Clear(); cmd.CommandText = "INSERT INTO parts VALUES($id,'old-deleted-timer',$date,$duration,$zone,1,$start,$end)";
            cmd.Parameters.AddWithValue("$id", part.Id); cmd.Parameters.AddWithValue("$date", part.Date.ToString("yyyy-MM-dd")); cmd.Parameters.AddWithValue("$duration", part.DurationMs);
            cmd.Parameters.AddWithValue("$zone", part.ZoneId); cmd.Parameters.AddWithValue("$start", part.StartUtcMs!); cmd.Parameters.AddWithValue("$end", part.EndUtcMs!); cmd.ExecuteNonQuery();
        }
        var oldRecord = new StudyRecord("old-session", "math", "old-chapter", SessionSource.Manual, "原备注", [new("old-part", new(2026,10,14),12345,"Asia/Shanghai",PartKind.DurationOnly)],1);
        cmd.Parameters.Clear();cmd.CommandText="INSERT INTO revisions VALUES('old-revision','session','old-session',1,'2026-10-14T00:00:00Z','历史修订',NULL,$record); INSERT INTO reflections VALUES('2026-10-14',$reflection)";
        cmd.Parameters.AddWithValue("$record",JsonSerializer.Serialize(oldRecord));cmd.Parameters.AddWithValue("$reflection",JsonSerializer.Serialize(new { Date=new DateOnly(2026,10,14),Text="旧版每日感想" }));cmd.ExecuteNonQuery();
        if (active)
        {
            var sample = new ClockSample(DateTimeOffset.Parse("2026-10-14T00:00:01Z"), 1000, 1000, "legacy-test");
            var snapshot = new TimerSnapshot("old-active", "math", "old-chapter", "暂停检查点", "Asia/Shanghai",TimerState.Paused,[],sample,sample.Utc.ToUnixTimeMilliseconds(),Version:1);
            cmd.Parameters.Clear();cmd.CommandText="INSERT INTO sessions VALUES('old-active','math','old-chapter',1,'暂停检查点',1,0,'2026-10-14T00:00:00Z'); INSERT INTO active_timer VALUES(1,$timer)";
            cmd.Parameters.AddWithValue("$timer",JsonSerializer.Serialize(snapshot));cmd.ExecuteNonQuery();
        }
        return root;
    }
}
