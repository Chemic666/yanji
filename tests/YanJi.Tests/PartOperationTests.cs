using YanJi.Core;
using YanJi.Data;

namespace YanJi.Tests;
public sealed class PartOperationTests : IDisposable
{
    private readonly string _root=Path.Combine(AppContext.BaseDirectory,"scratch","parts-"+Guid.NewGuid().ToString("N"));
    private readonly YanJiDatabase _db;
    public PartOperationTests(){_db=new(_root);}
    private StudyRecord CrossDay()=>_db.SaveRecord(new(Guid.NewGuid().ToString("N"),"math",null,SessionSource.Timer,"跨日",
        TimeParts.Interval(DateTimeOffset.Parse("2026-10-13T23:50+08:00"),DateTimeOffset.Parse("2026-10-14T00:20+08:00"),"Asia/Shanghai")),"初始");
    [Fact] public void ExcludingOneRealPartChangesOnlyItsDayAndRestoresExactEvidence()
    {
        var r=CrossDay();var removed=r.Parts[0];_db.ExcludePart(r.Id,removed.Id,r.Version);
        var after=Assert.Single(_db.Records());Assert.False(after.Deleted);Assert.Single(after.Parts);Assert.Equal(1_200_000,after.DurationMs);
        Assert.Empty(_db.DailyTotals(removed.Date,removed.Date));Assert.Equal(removed,Assert.Single(_db.ExcludedParts(r.Id)));
        Assert.Equal(2,_db.History(r.Id).Count);_db.RestorePart(r.Id,removed.Id,after.Version);
        var restored=Assert.Single(_db.Records());Assert.Equal(1_800_000,restored.DurationMs);Assert.Contains(removed,restored.Parts);Assert.Empty(_db.ExcludedParts(r.Id));
        Assert.Equal(600_000,Assert.Single(_db.DailyTotals(removed.Date,removed.Date)).DurationMs);
    }
    [Fact] public void LastPartStaleVersionAndActiveSessionCannotBeExcluded()
    {
        var r=CrossDay();Assert.Throws<InvalidOperationException>(()=>_db.ExcludePart(r.Id,r.Parts[0].Id,0));
        _db.ExcludePart(r.Id,r.Parts[0].Id,r.Version);var after=Assert.Single(_db.Records());
        Assert.Throws<InvalidOperationException>(()=>_db.ExcludePart(after.Id,after.Parts[0].Id,after.Version));Assert.Single(_db.Records().Single().Parts);
        var timer=new TimerMachine();var start=DateTimeOffset.Parse("2026-10-15T08:00Z");timer.Start("english",null,"","Asia/Shanghai",new(start,0,0,"t"));
        timer.Advance(new(start.AddMinutes(1),60000,60000,"t"));var saved=_db.SaveTimer(timer.Snapshot);
        Assert.Throws<InvalidOperationException>(()=>_db.ExcludePart(saved.SessionId,saved.Parts[0].Id,saved.Version));
    }
    [Fact] public void RestoreOverlapIsAtomicAndDeletedSessionRequiresWholeRestore()
    {
        var r=CrossDay();var p=r.Parts[0];_db.ExcludePart(r.Id,p.Id,r.Version);r=_db.Records().Single();
        var other=_db.SaveRecord(new("conflict","english",null,SessionSource.Manual,"",[p with{Id="other"}]),"占用区间");
        var before=_db.History(r.Id).Count;Assert.Throws<InvalidOperationException>(()=>_db.RestorePart(r.Id,p.Id,r.Version));Assert.Equal(before,_db.History(r.Id).Count);
        Assert.Single(_db.Records().First(x=>x.Id==r.Id).Parts);_db.DeleteRecord(other.Id,other.Version);_db.DeleteRecord(r.Id,r.Version);
        r=_db.Records(null,true).First(x=>x.Id==r.Id);Assert.Throws<InvalidOperationException>(()=>_db.RestorePart(r.Id,p.Id,r.Version));
        _db.RestoreRecord(r.Id,r.Version);r=_db.Records().Single();_db.RestorePart(r.Id,p.Id,r.Version);Assert.Equal(1_800_000,_db.Records().Single().DurationMs);
    }
    [Fact] public void ExclusionSurvivesBackupRestoreAndDoesNotRewriteSchema()
    {
        var r=CrossDay();var p=r.Parts[1];_db.ExcludePart(r.Id,p.Id,r.Version);var backup=Path.Combine(_root,"parts.yanjibak");_db.Backup(backup);
        r=_db.Records().Single();_db.RestorePart(r.Id,p.Id,r.Version);_db.RestoreBackup(backup);
        Assert.Equal(p,Assert.Single(_db.ExcludedParts(r.Id)));r=_db.Records().Single();_db.RestorePart(r.Id,p.Id,r.Version);
        Assert.Equal("ok",_db.Integrity());Assert.Equal(1_800_000,r.DurationMs+p.DurationMs);
    }
    [Fact] public void StaleRestoreAndUnknownPartCannotModifyAuditOrTotals()
    {
        var r=CrossDay();var removed=r.Parts[0];_db.ExcludePart(r.Id,removed.Id,r.Version);var audit=_db.History(r.Id).Count;
        Assert.Throws<InvalidOperationException>(()=>_db.RestorePart(r.Id,removed.Id,r.Version));
        r=_db.Records().Single();Assert.Throws<InvalidOperationException>(()=>_db.RestorePart(r.Id,"not-a-part",r.Version));
        Assert.Throws<InvalidOperationException>(()=>_db.ExcludePart(r.Id,"not-a-part",r.Version));
        Assert.Equal(audit,_db.History(r.Id).Count);Assert.Equal(1_200_000,_db.Records().Single().DurationMs);
    }
    [Fact] public void RepeatedExcludeRestoreCyclesUseLatestRevisionAndRollBackOnWriteFailure()
    {
        var r=CrossDay();var p=r.Parts[0];_db.ExcludePart(r.Id,p.Id,r.Version);r=_db.Records().Single();_db.RestorePart(r.Id,p.Id,r.Version);
        r=_db.Records().Single();_db.ExcludePart(r.Id,p.Id,r.Version);Assert.Equal(p,Assert.Single(_db.ExcludedParts(r.Id)));
        r=_db.Records().Single();using var c=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_db.DatabasePath};Pooling=False");c.Open();using var cmd=c.CreateCommand();
        cmd.CommandText="CREATE TRIGGER fail_parts BEFORE INSERT ON parts BEGIN SELECT RAISE(ABORT,'isolated failure'); END";cmd.ExecuteNonQuery();
        var count=_db.History(r.Id).Count;Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(()=>_db.RestorePart(r.Id,p.Id,r.Version));
        Assert.Equal(count,_db.History(r.Id).Count);Assert.Single(_db.Records().Single().Parts);Assert.Equal(p,Assert.Single(_db.ExcludedParts(r.Id)));
    }
    public void Dispose(){_db.Dispose();Directory.Delete(_root,true);}
}
