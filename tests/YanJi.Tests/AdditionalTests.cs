using System.Globalization;
using System.IO.Compression;
using YanJi.Core;
using YanJi.Data;

namespace YanJi.Tests;
public sealed class AdditionalTests
{
    [Theory]
    [InlineData("2026-03-08T05:00:00Z","2026-03-09T04:00:00Z",82_800_000)]
    [InlineData("2026-11-01T04:00:00Z","2026-11-02T05:00:00Z",90_000_000)]
    public void DstDaysUseActualElapsedTime(string start,string end,long milliseconds)
    {var parts=TimeParts.Interval(DateTimeOffset.Parse(start),DateTimeOffset.Parse(end),"America/New_York");Assert.Single(parts);Assert.Equal(milliseconds,parts.Sum(p=>p.DurationMs));}
    [Fact] public void SnapshotsCannotBeMutatedByCaller()
    {var timer=new TimerMachine();var t=DateTimeOffset.UtcNow;timer.Start("math",null,"","UTC",new(t,0,0,"a"));timer.Advance(new(t.AddSeconds(5),5000,5000,"a"));timer.Snapshot.Parts.Clear();Assert.Equal(5000,timer.Snapshot.DurationMs);}
    [Fact] public void FrequentCheckpointsCoalesceWithoutLosingMilliseconds()
    {var timer=new TimerMachine();var t=DateTimeOffset.Parse("2026-10-13T08:00Z");timer.Start("math",null,"","Asia/Shanghai",new(t,0,0,"a"));for(var i=1;i<=1000;i++)timer.Advance(new(t.AddMilliseconds(i*17),i*17,i*17,"a"));Assert.Equal(17000,timer.Snapshot.DurationMs);Assert.Single(timer.Snapshot.Parts);}
    [Fact] public void UncertainStateCannotFinishWithoutExplicitReview()
    {var timer=new TimerMachine();var t=DateTimeOffset.UtcNow;timer.Start("math",null,"","UTC",new(t,0,0,"a"));timer.MarkUncertain("test");Assert.Throws<InvalidOperationException>(()=>timer.Finish(new(t.AddHours(1),1,1,"b")));Assert.Equal(0,timer.Snapshot.DurationMs);}
    [Fact] public void BrokenDailyBackupIsNotAcceptedAsSuccessful()
    {WithDatabase((db,root)=>{var folder=Path.Combine(root,"data","backups");Directory.CreateDirectory(folder);var bad=Path.Combine(folder,"daily-"+DateTime.Today.ToString("yyyyMMdd")+".yanjibak");File.WriteAllText(bad,"");var valid=db.AutoBackup();Assert.NotEqual(bad,valid);db.ValidateBackup(valid);Assert.Equal(0,new FileInfo(bad).Length);});}
    [Fact] public void MalformedDailyManifestIsPreservedAndReplacedByValidNewBackup()
    {WithDatabase((db,root)=>{var bad=db.AutoBackup();using(var zip=ZipFile.Open(bad,ZipArchiveMode.Update)){zip.GetEntry("manifest.json")!.Delete();using var w=new StreamWriter(zip.CreateEntry("manifest.json").Open());w.Write("{");}
        var good=db.AutoBackup();Assert.NotEqual(bad,good);Assert.True(File.Exists(bad));db.ValidateBackup(good);});}
    [Fact] public void ActiveBackupAcceptsDifferentPartDateOrderingAndRestoresCheckpoint()
    {WithDatabase((db,root)=>{var t=DateTimeOffset.Parse("2026-10-13T08:00Z");var timer=new TimerMachine();timer.Start("math",null,"","Asia/Shanghai",new(t,0,0,"a"));timer.Advance(new(t.AddSeconds(5),5000,5000,"a"));timer.MarkUncertain("test");timer.ResolveReview([TimeParts.Duration(new(2026,10,12),TimeSpan.FromMinutes(2),"Asia/Shanghai")],new(t.AddSeconds(20),20000,20000,"a"));timer.ConfirmSaved(db.SaveTimer(timer.Snapshot).Version);var backup=Path.Combine(root,"active.yanjibak");db.Backup(backup);db.ValidateBackup(backup);timer.Finish(new(t.AddSeconds(30),30000,30000,"a"));db.SaveTimer(timer.Snapshot);db.RestoreBackup(backup);Assert.Equal(TimerState.Paused,db.LoadTimer()!.State);Assert.Equal(125000,db.LoadTimer()!.DurationMs);});}
    [Fact] public void TimerSaveRetryIsIdempotent()
    {WithDatabase((db,_)=>{var t=DateTimeOffset.UtcNow;var timer=new TimerMachine();timer.Start("math",null,"","UTC",new(t,0,0,"a"));var first=db.SaveTimer(timer.Snapshot,"start");var second=db.SaveTimer(timer.Snapshot,"retry");Assert.Equal(first.Version,second.Version);Assert.Single(db.History(first.SessionId));});}
    [Fact] public void TaskWeakPointReflectionAndCsvPersistWithAudit()
    {WithDatabase((db,root)=>{var day=new DateOnly(2026,10,13);db.SaveTask(new("task","中文任务",day,true,"math"));db.SaveWeakPoint(new("weak","概念","详情","概念理解","math",State:WeakPointState.Verified));db.SaveReflection(day,"感想1");db.SaveReflection(day,"感想2");Assert.True(Assert.Single(db.Tasks()).Done);Assert.Equal(WeakPointState.Verified,Assert.Single(db.WeakPoints()).State);Assert.Equal(2,db.History(day.ToString("yyyy-MM-dd")).Count);
        db.SaveRecord(new("manual","math",null,SessionSource.Manual,"=not-a-formula\n中文",[TimeParts.Duration(day,TimeSpan.FromMinutes(3),"Asia/Shanghai")]),"新增");var csv=Path.Combine(root,"records.csv");db.ExportCsv(csv);Assert.Contains("'=not-a-formula",File.ReadAllText(csv));Assert.Throws<IOException>(()=>db.ExportCsv(csv));});}
    [Fact] public void DeletedIntervalRestorationRechecksOverlap()
    {WithDatabase((db,_)=>{var parts=TimeParts.Interval(DateTimeOffset.Parse("2026-10-13T08:00Z"),DateTimeOffset.Parse("2026-10-13T09:00Z"),"Asia/Shanghai");var record=db.SaveRecord(new("one","math",null,SessionSource.Manual,"",parts),"新增");db.DeleteRecord(record.Id,record.Version);db.SaveRecord(new("two","math",null,SessionSource.Manual,"",parts.Select(p=>p with{Id=Guid.NewGuid().ToString("N")}).ToList()),"替代");Assert.Throws<InvalidOperationException>(()=>db.RestoreRecord("one",2));Assert.Equal(3600000,db.DailyTotals(new(2026,10,13),new(2026,10,13)).Sum(d=>d.DurationMs));});}
    private static void WithDatabase(Action<YanJiDatabase,string> action)
    {var root=Path.Combine(AppContext.BaseDirectory,"scratch","extra-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);try{using var db=new YanJiDatabase(Path.Combine(root,"data"));action(db,root);}finally{Directory.Delete(root,true);}}
    [Fact] public void SqliteReadOnlyAndPageLimitFailWithoutChangingData()
    {
        WithDatabase((db,_)=>
        {
            using(var c=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={db.DatabasePath};Mode=ReadOnly;Pooling=False"))
            {c.Open();using var cmd=c.CreateCommand();cmd.CommandText="INSERT INTO settings VALUES('readonly','\"x\"')";var exception=Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(()=>cmd.ExecuteNonQuery());Assert.Equal(8,exception.SqliteErrorCode);}
            using(var c=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={db.DatabasePath};Pooling=False"))
            {c.Open();using var cmd=c.CreateCommand();cmd.CommandText="PRAGMA page_count";var pages=Convert.ToInt64(cmd.ExecuteScalar());cmd.CommandText=$"PRAGMA max_page_count={pages}";Assert.Equal(pages,Convert.ToInt64(cmd.ExecuteScalar()));cmd.CommandText="INSERT INTO settings VALUES('full',zeroblob(1000000))";var exception=Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(()=>cmd.ExecuteNonQuery());Assert.Equal(13,exception.SqliteErrorCode);}
            Assert.Equal("missing",db.Setting("readonly","missing"));Assert.Equal("missing",db.Setting("full","missing"));Assert.Equal("ok",db.Integrity());
        });
    }
    [Fact] public void SafeSqliteFlagsAreActuallySet()
    {WithDatabase((db,_)=>{var flags=db.Diagnostics();Assert.Equal("wal",flags["journal_mode"]);Assert.Equal("1",flags["foreign_keys"]);Assert.Equal("2",flags["synchronous"]);Assert.NotEmpty(flags["sqlite"]);});}
    [Fact] public void BackupCapturesCommittedDataStillInWal()
    {
        WithDatabase((db,root)=>
        {
            var backup=Path.Combine(root,"wal.yanjibak");
            using(var c=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={db.DatabasePath};Pooling=False"))
            {c.Open();using var cmd=c.CreateCommand();cmd.CommandText="PRAGMA wal_autocheckpoint=0";cmd.ExecuteScalar();cmd.CommandText="INSERT INTO settings VALUES('wal-backup','\"present\"')";cmd.ExecuteNonQuery();
                Assert.True(new FileInfo(db.DatabasePath+"-wal").Length>0);db.Backup(backup);db.ValidateBackup(backup);}
            db.SaveSetting("wal-backup","changed");db.RestoreBackup(backup);Assert.Equal("present",db.Setting("wal-backup","missing"));
        });
    }
    [Fact] public void PersistedDatesAreIndependentOfWindowsCulture()
    {
        var prior=CultureInfo.CurrentCulture;try{CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("th-TH");
            WithDatabase((db,_)=>{var day=new DateOnly(2026,10,13);db.SaveRecord(new("culture","math",null,SessionSource.Manual,"",[TimeParts.Duration(day,TimeSpan.FromMinutes(1),"Asia/Shanghai")]),"culture");Assert.Equal(day,Assert.Single(db.Records()).Parts[0].Date);Assert.Equal(60000,Assert.Single(db.DailyTotals(day,day)).DurationMs);});}
        finally{CultureInfo.CurrentCulture=prior;}
    }
    [Fact] public void RawFileCopyIsNotReliableBackupWhenCommittedWalExists()
    {
        WithDatabase((db,root)=>
        {
            using var connection=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={db.DatabasePath};Pooling=False");connection.Open();
            using var command=connection.CreateCommand();command.CommandText="PRAGMA wal_autocheckpoint=0";command.ExecuteScalar();command.CommandText="INSERT INTO settings VALUES('pending-wal','\"only in WAL\"')";command.ExecuteNonQuery();
            Assert.True(new FileInfo(db.DatabasePath+"-wal").Length>0);var copied=Path.Combine(root,"raw.sqlite");File.Copy(db.DatabasePath,copied);
            using var raw=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={copied};Mode=ReadOnly;Pooling=False");raw.Open();using var query=raw.CreateCommand();query.CommandText="SELECT COUNT(*) FROM settings WHERE id='pending-wal'";Assert.Equal(0L,(long)query.ExecuteScalar()!);
            Assert.ThrowsAny<Exception>(()=>db.ValidateBackup(copied));Assert.Equal("only in WAL",db.Setting("pending-wal","missing"));
        });
    }
    [Fact] public void SqliteConstraintsRejectBadDurationAndReferences()
    {
        WithDatabase((db,_)=>
        {
            using var connection=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={db.DatabasePath};Pooling=False");connection.Open();using var command=connection.CreateCommand();command.CommandText="PRAGMA foreign_keys=ON";command.ExecuteNonQuery();
            foreach(var sql in new[]{"INSERT INTO sessions VALUES('bad','missing',NULL,0,'',1,0,'2026-10-13T00:00:00Z')","INSERT INTO chapters VALUES('bad','math','chapter',200,0,0)","INSERT INTO parts VALUES('bad','missing','2026-10-13',0,'UTC',0,NULL,NULL)"})
            {command.CommandText=sql;var exception=Assert.Throws<Microsoft.Data.Sqlite.SqliteException>(()=>command.ExecuteNonQuery());Assert.Equal(19,exception.SqliteErrorCode);}
            Assert.Empty(db.Records());Assert.Equal("ok",db.Integrity());
        });
    }
}
