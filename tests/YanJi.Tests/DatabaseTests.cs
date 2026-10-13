using System.IO.Compression;
using Microsoft.Data.Sqlite;
using YanJi.Core;
using YanJi.Data;

namespace YanJi.Tests;

public sealed class DatabaseTests : IDisposable
{
    private readonly string _root = Path.Combine(AppContext.BaseDirectory, "scratch", "yanji-test-" + Guid.NewGuid().ToString("N"));
    private readonly YanJiDatabase _db;
    private static readonly DateOnly Day = new(2026, 10, 13);
    public DatabaseTests() { Directory.CreateDirectory(_root); _db = new(Path.Combine(_root, "data")); }
    private static StudyRecord Record(double minutes = 30) => new(Guid.NewGuid().ToString("N"), "math", null,
        SessionSource.Manual, "", [TimeParts.Duration(Day, TimeSpan.FromMinutes(minutes), "Asia/Shanghai")]);
    [Fact] public void PersistenceEditDeleteRestoreAndAuditStayConsistent()
    {
        var record = _db.SaveRecord(Record(), "新增");
        Assert.Equal(1, record.Version);
        record = _db.SaveRecord(record with { Parts = [TimeParts.Duration(Day, TimeSpan.FromMinutes(45), "Asia/Shanghai")] }, "修正");
        Assert.Equal(2_700_000, Assert.Single(_db.DailyTotals(Day, Day)).DurationMs);
        Assert.Throws<InvalidOperationException>(() => _db.SaveRecord(record with { Version = 1 }, "过期修改"));
        _db.DeleteRecord(record.Id, record.Version);
        Assert.Empty(_db.DailyTotals(Day, Day));
        _db.RestoreRecord(record.Id, 3);
        using var restarted = new YanJiDatabase(Path.Combine(_root, "data"));
        Assert.Equal(2_700_000, Assert.Single(restarted.Records()).DurationMs);
        Assert.Equal(4, _db.History(record.Id).Count);
        Assert.Equal("ok", _db.Integrity());
    }
    [Fact] public void RepeatedCreationHasNoDuplicateAuditOrStatistics()
    {
        var record = Record(); _db.SaveRecord(record, "新增"); _db.SaveRecord(record, "重复提交");
        Assert.Single(_db.Records()); Assert.Single(_db.History(record.Id));
    }
    [Fact] public void KnownOverlapBlockedDurationOnlyAllowedAndFailedEditLeavesOriginal()
    {
        var a = Record() with { Parts = TimeParts.Interval(DateTimeOffset.Parse("2026-10-13T08:00Z"), DateTimeOffset.Parse("2026-10-13T09:00Z"), "Asia/Shanghai") };
        a = _db.SaveRecord(a, "新增");
        Assert.Throws<InvalidOperationException>(() => _db.SaveRecord(a with { Id = Guid.NewGuid().ToString("N"), Version = 0 }, "重叠"));
        _db.SaveRecord(Record(), "纯时长");
        Assert.Throws<ArgumentException>(() => _db.SaveRecord(a with { Parts = [a.Parts[0] with { DurationMs = -1 }] }, "非法"));
        Assert.Equal(5_400_000, Assert.Single(_db.DailyTotals(Day, Day)).DurationMs);
    }
    [Fact] public void ChapterProgressAndMasteryAreIndependentAndSubjectMatchRequired()
    {
        _db.SaveChapter(new("c1", "math", "极限", 80, Mastery.Weak));
        Assert.Equal(Mastery.Weak, Assert.Single(_db.Chapters()).Mastery);
        Assert.Throws<ArgumentException>(() => _db.SaveRecord(Record() with { SubjectId = "english", ChapterId = "c1" }, "非法章节"));
        _db.SaveSubject(new("math", "数学", true));
        Assert.DoesNotContain(_db.Subjects(), s => s.Id == "math");
        Assert.Contains(_db.Subjects(true), s => s.Id == "math");
    }
    [Fact] public void BackupWithWalRestoresAndRejectsTamperingWithoutChangingCurrent()
    {
        var record = _db.SaveRecord(Record(), "新增");
        var backup = Path.Combine(_root, "snapshot.yanjibak"); _db.Backup(backup);
        _db.DeleteRecord(record.Id, record.Version);
        _db.RestoreBackup(backup);
        Assert.Equal(1_800_000, Assert.Single(_db.Records()).DurationMs);
        var corrupt = Path.Combine(_root, "bad.yanjibak"); File.Copy(backup, corrupt);
        using (var zip = ZipFile.Open(corrupt, ZipArchiveMode.Update)) { zip.GetEntry("database.sqlite")!.Delete(); var e=zip.CreateEntry("database.sqlite"); using var w=new StreamWriter(e.Open()); w.Write("corrupt"); }
        Assert.ThrowsAny<Exception>(() => _db.RestoreBackup(corrupt));
        Assert.Equal(1_800_000, Assert.Single(_db.Records()).DurationMs);
        Assert.Equal("ok", _db.Integrity());
    }
    [Fact] public void ActiveTimerPersistsPrefixAndPreventsSecondIdentity()
    {
        var machine=new TimerMachine(); var epoch=DateTimeOffset.Parse("2026-10-13T08:00Z");
        machine.Start("math",null,"","Asia/Shanghai",new(epoch,0,0,"a"));
        machine.Advance(new(epoch.AddSeconds(15),15000,15000,"a")); machine.ConfirmSaved(_db.SaveTimer(machine.Snapshot,"检查点").Version);
        Assert.Equal(15000,_db.LoadTimer()!.DurationMs);
        Assert.Throws<InvalidOperationException>(() => _db.SaveTimer(machine.Snapshot with { SessionId="other" }));
        machine.Finish(new(epoch.AddSeconds(20),20000,20000,"a")); _db.SaveTimer(machine.Snapshot,"结束");
        Assert.Null(_db.LoadTimer()); Assert.Equal(20000,Assert.Single(_db.Records()).DurationMs);
    }
    [Fact] public void SQLiteWriteFailureDoesNotPartiallyChangeRecord()
    {
        var record=_db.SaveRecord(Record(),"新增");
        using var connection=new SqliteConnection($"Data Source={_db.DatabasePath};Pooling=False"); connection.Open();
        using var command=connection.CreateCommand(); command.CommandText="CREATE TRIGGER fail_part BEFORE INSERT ON parts BEGIN SELECT RAISE(ABORT,'isolated failure'); END"; command.ExecuteNonQuery();
        Assert.Throws<SqliteException>(() => _db.SaveRecord(record with { Note="must roll back", Parts=[TimeParts.Duration(Day,TimeSpan.FromMinutes(80),"Asia/Shanghai")] },"故障"));
        Assert.Equal("",Assert.Single(_db.Records()).Note); Assert.Single(_db.History(record.Id));
    }
    [Fact] public void StaleTimerCannotReopenCompletedSession()
    {
        var epoch=DateTimeOffset.Parse("2026-10-13T08:00Z"); var timer=new TimerMachine();
        timer.Start("math",null,"","Asia/Shanghai",new(epoch,0,0,"a")); timer.Advance(new(epoch.AddSeconds(10),10000,10000,"a"));
        var stale=_db.SaveTimer(timer.Snapshot); timer.ConfirmSaved(stale.Version);
        timer.Finish(new(epoch.AddSeconds(20),20000,20000,"a")); _db.SaveTimer(timer.Snapshot);
        Assert.Throws<InvalidOperationException>(()=>_db.SaveTimer(stale));
        Assert.Null(_db.LoadTimer());Assert.Equal(20000,Assert.Single(_db.Records()).DurationMs);
    }
    [Fact] public void RestoreRejectsMissingTablesBeforeReplacingCurrent()
    {
        _db.SaveRecord(Record(),"新增");var path=Path.Combine(_root,"incomplete.yanjibak");_db.Backup(path);
        var extracted=Path.Combine(_root,"edited.sqlite");
        using(var z=ZipFile.OpenRead(path))z.GetEntry("database.sqlite")!.ExtractToFile(extracted);
        using(var c=new SqliteConnection($"Data Source={extracted};Pooling=False")){c.Open();using var cmd=c.CreateCommand();cmd.CommandText="DROP TABLE tasks";cmd.ExecuteNonQuery();}
        using(var z=ZipFile.Open(path,ZipArchiveMode.Update))
        {z.GetEntry("database.sqlite")!.Delete();z.CreateEntryFromFile(extracted,"database.sqlite");
            var entry=z.GetEntry("manifest.json")!;System.Text.Json.Nodes.JsonNode node;
            using(var reader=new StreamReader(entry.Open()))node=System.Text.Json.Nodes.JsonNode.Parse(reader.ReadToEnd())!;
            node["Sha256"]=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(extracted)));
            entry.Delete();using var writer=new StreamWriter(z.CreateEntry("manifest.json").Open());writer.Write(node.ToJsonString());}
        Assert.ThrowsAny<Exception>(()=>_db.RestoreBackup(path));Assert.Equal(1800000,Assert.Single(_db.Records()).DurationMs);
    }
    public void Dispose() { _db.Dispose(); if (Directory.Exists(_root)) Directory.Delete(_root,true); }
}
