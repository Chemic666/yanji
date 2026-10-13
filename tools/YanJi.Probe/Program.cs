using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using YanJi.Core;
using YanJi.Data;

if(args.Length<2)throw new ArgumentException("Specify --run <new isolated directory> or --child <directory>.");
var root=Path.GetFullPath(args[1]);
if(args[0] is "--compat-capture" or "--compat-verify" or "--compat-reset")
{
    if(!File.Exists(Path.Combine(root,"owned-upgrade.marker")))throw new ArgumentException("Compatibility probe requires an explicitly owned fixture marker.");
    using var db=new YanJiDatabase(Path.Combine(root,"data"));
    string State()=>JsonSerializer.Serialize(new{Records=db.Records(null,true).OrderBy(r=>r.Id),Subjects=db.Subjects(true).OrderBy(s=>s.Id),Chapters=db.Chapters(null,true).OrderBy(c=>c.Id),Tasks=db.Tasks().OrderBy(t=>t.Id),Weak=db.WeakPoints().OrderBy(w=>w.Id),Reflections=db.Records(null,true).SelectMany(r=>r.Parts.Select(p=>p.Date)).Distinct().Order().Select(d=>new{Date=d,Text=db.Reflection(d)}),History=db.Records(null,true).Select(r=>new{r.Id,Count=db.History(r.Id).Count}).OrderBy(r=>r.Id)});
    var baseline=Path.Combine(root,"v010-state.json");var oldBackup=Path.Combine(root,"v010-full.yanjibak");
    if(args[0]=="--compat-reset")
    {db.ValidateBackup(oldBackup);db.RestoreBackup(oldBackup);if(State()!=File.ReadAllText(baseline))throw new Exception("Owned fixture reset failed.");File.WriteAllText(Path.Combine(root,"fixture-reset-"+Guid.NewGuid().ToString("N")+".json"),JsonSerializer.Serialize(new{Status="PASS",Evidence="Explicit owned fixture reset from retained V0.1 full backup"}));return;}
    if(args[0]=="--compat-capture")
    {using var writer=new StreamWriter(new FileStream(baseline,FileMode.CreateNew));writer.Write(State());db.Backup(oldBackup);Console.WriteLine("V0.1 actual fixture captured.");return;}
    if(State()!=File.ReadAllText(baseline))throw new Exception("Upgrade changed existing record, part, audit or related facts.");
    var total=db.Records().Sum(r=>r.DurationMs);var cross=db.Records().FirstOrDefault(r=>r.Parts.Count>1)??throw new Exception("Actual old fixture must contain multiple parts.");
    var removed=cross.Parts[0];db.ExcludePart(cross.Id,removed.Id,cross.Version);var after=db.Records().Single(r=>r.Id==cross.Id);db.RestorePart(after.Id,removed.Id,after.Version);
    if(db.Records().Sum(r=>r.DurationMs)!=total)throw new Exception("Upgrade part roundtrip changed exact duration.");
    var upgraded=Path.Combine(root,"v011-full-"+Guid.NewGuid().ToString("N")+".yanjibak");db.Backup(upgraded);db.ValidateBackup(upgraded);db.ValidateBackup(oldBackup);db.RestoreBackup(oldBackup);
    if(State()!=File.ReadAllText(baseline)||db.Integrity()!="ok")throw new Exception("Old complete backup restore did not preserve exact old facts.");
    using var schemaConnection=new SqliteConnection($"Data Source={db.DatabasePath};Mode=ReadOnly;Pooling=False");schemaConnection.Open();using var schemaCommand=schemaConnection.CreateCommand();schemaCommand.CommandText="PRAGMA user_version";var schema=Convert.ToInt32(schemaCommand.ExecuteScalar());if(schema!=1)throw new Exception("Unexpected schema version.");
    var report=new{Status="PASS",Evidence="V0.1 binary wrote fixture; V0.1.1 binary opened it; exact full facts compared; upgraded part exclude/restore; both backup versions validated and V0.1 backup restored",Records=db.Records(null,true).Count,TotalMs=total,SchemaVersion=schema,Integrity=db.Integrity(),ExistingFactsUnchanged=true,Migration="none"};
    File.WriteAllText(Path.Combine(root,"compatibility.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine(JsonSerializer.Serialize(report));return;
}
if(args[0].StartsWith("--restore-child-",StringComparison.Ordinal))
{
    using var db=new YanJiDatabase(Path.Combine(root,"data"));var stop=args[0].EndsWith("before",StringComparison.Ordinal)?"before-replace":"after-replace";
    db.RestoreCheckpoint=phase=>{if(phase==stop){File.WriteAllText(Path.Combine(root,"ready.json"),JsonSerializer.Serialize(new{Pid=Environment.ProcessId,Phase=phase}));Thread.Sleep(60000);throw new Exception("Parent did not terminate restore probe.");}};
    db.RestoreBackup(Path.Combine(root,"baseline.yanjibak"));throw new Exception("Restore hook was not reached.");
}
if(args[0]=="--child")
{
    using var db=new YanJiDatabase(Path.Combine(root,"data"));
    using var connection=new SqliteConnection($"Data Source={db.DatabasePath};Pooling=False");connection.Open();
    var day=new DateOnly(2026,10,13);
    db.SaveRecord(new("committed","math",null,SessionSource.Manual,"durable WAL commit",[TimeParts.Duration(day,TimeSpan.FromMinutes(10),"Asia/Shanghai")]),"WAL commit");
    var machine=new TimerMachine();var utc=DateTimeOffset.Parse("2026-10-13T08:00Z");machine.Start("math",null,"","Asia/Shanghai",new(utc,0,0,"child"));machine.Advance(new(utc.AddSeconds(15),15000,15000,"child"));db.SaveTimer(machine.Snapshot,"durable checkpoint");
    using(var walWrite=connection.CreateCommand()){walWrite.CommandText="PRAGMA journal_mode=WAL; PRAGMA wal_autocheckpoint=0; INSERT INTO settings VALUES('probe-wal','\"durable\"')";walWrite.ExecuteNonQuery();}
    using var tx=connection.BeginTransaction();using var cmd=connection.CreateCommand();cmd.Transaction=tx;
    cmd.CommandText="INSERT INTO sessions VALUES('uncommitted','math',NULL,0,'rollback',1,0,'2026-10-13T00:00:00Z')";cmd.ExecuteNonQuery();
    File.WriteAllText(Path.Combine(root,"ready.json"),JsonSerializer.Serialize(new{Pid=Environment.ProcessId,WalExists=File.Exists(db.DatabasePath+"-wal"),WalBytes=new FileInfo(db.DatabasePath+"-wal").Length}));
    await Task.Delay(TimeSpan.FromMinutes(2));throw new Exception("Parent failed to terminate owned probe.");
}
if(args[0]!="--run"||Directory.Exists(root))throw new ArgumentException("Probe requires a new isolated directory.");
Directory.CreateDirectory(root);
var start=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true};
if(string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath),"dotnet",StringComparison.OrdinalIgnoreCase))start.ArgumentList.Add(typeof(Program).Assembly.Location);
start.ArgumentList.Add("--child");start.ArgumentList.Add(root);
using var child=Process.Start(start)??throw new Exception("Cannot start child.");
var ready=Path.Combine(root,"ready.json");var limit=Stopwatch.StartNew();while(!File.Exists(ready)&&limit.Elapsed<TimeSpan.FromSeconds(30)&&!child.HasExited)await Task.Delay(100);
if(!File.Exists(ready))throw new Exception("No ready evidence. Owned child has PID "+child.Id+"; no unrelated process was terminated.");
var evidence=JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(ready));if(evidence.GetProperty("Pid").GetInt32()!=child.Id)throw new Exception("Child ownership evidence failed.");
child.Kill();await child.WaitForExitAsync();
if(!evidence.GetProperty("WalExists").GetBoolean()||evidence.GetProperty("WalBytes").GetInt64()<=0)throw new Exception("Pending WAL evidence missing; not a PASS.");
using(var db=new YanJiDatabase(Path.Combine(root,"data")))
{
    var records=db.Records();var checkpoint=db.LoadTimer()??throw new Exception("Missing timer checkpoint.");var timer=new TimerMachine();timer.Restore(checkpoint,new(DateTimeOffset.UtcNow,0,0,"new-parent"));
    if(records.Any(r=>r.Id=="uncommitted")||!records.Any(r=>r.Id=="committed"&&r.DurationMs==600000)||db.Setting("probe-wal","")!="durable"||db.Integrity()!="ok"||timer.Snapshot.State!=TimerState.NeedsReview||timer.Snapshot.DurationMs!=15000)throw new Exception("Crash recovery invariants failed.");
    var report=new{Status="PASS",Evidence="Actual owned Windows child process forcibly terminated with open SQLite WAL transaction",ChildPid=child.Id,ChildExit=child.ExitCode,WalBytes=evidence.GetProperty("WalBytes").GetInt64(),CommittedRecordsSurvived=true,UncommittedRolledBack=true,TimerRecoveredMs=timer.Snapshot.DurationMs,TimerState=timer.Snapshot.State.ToString(),Integrity=db.Integrity()};
    File.WriteAllText(Path.Combine(root,"results.json"),JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine(JsonSerializer.Serialize(report));
}
List<object> restoreResults=[];
foreach(var phase in new[]{"before","after"})
{
    var restoreRoot=Path.Combine(root,"restore-"+phase);Directory.CreateDirectory(restoreRoot);var day=new DateOnly(2026,10,13);
    using(var db=new YanJiDatabase(Path.Combine(restoreRoot,"data")))
    {var fixture=db.SaveRecord(new("fixture","math",null,SessionSource.Manual,"",[TimeParts.Duration(day,TimeSpan.FromMinutes(5),"Asia/Shanghai")]),"fixture");db.Backup(Path.Combine(restoreRoot,"baseline.yanjibak"));db.SaveRecord(fixture with{Parts=[TimeParts.Duration(day,TimeSpan.FromMinutes(10),"Asia/Shanghai")]},"current");}
    var childStart=new ProcessStartInfo(Environment.ProcessPath!){UseShellExecute=false,CreateNoWindow=true};
    if(string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath),"dotnet",StringComparison.OrdinalIgnoreCase))childStart.ArgumentList.Add(typeof(Program).Assembly.Location);
    childStart.ArgumentList.Add("--restore-child-"+phase);childStart.ArgumentList.Add(restoreRoot);using var restoreChild=Process.Start(childStart)!;
    var marker=Path.Combine(restoreRoot,"ready.json");var watch=Stopwatch.StartNew();while(!File.Exists(marker)&&watch.Elapsed.TotalSeconds<20&&!restoreChild.HasExited)await Task.Delay(100);
    if(!File.Exists(marker))throw new Exception("Restore child did not reach checkpoint; PID="+restoreChild.Id);
    var owned=JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(marker));if(owned.GetProperty("Pid").GetInt32()!=restoreChild.Id)throw new Exception("Restore child ownership mismatch.");
    restoreChild.Kill();await restoreChild.WaitForExitAsync();
    using var reopened=new YanJiDatabase(Path.Combine(restoreRoot,"data"));var duration=reopened.Records().Single().DurationMs;var expected=phase=="before"?600000:300000;
    if(duration!=expected||reopened.Integrity()!="ok")throw new Exception("Interrupted restore lost old/new valid database.");
    var prior=Directory.GetFiles(Path.Combine(restoreRoot,"data","backups"),"*.yanjibak");if(prior.Length!=1)throw new Exception("Pre-restore backup missing.");reopened.ValidateBackup(prior[0]);
    restoreResults.Add(new{Status="PASS",Phase=phase+" file replacement",ChildPid=restoreChild.Id,DurationMs=duration,Integrity=reopened.Integrity(),PriorFullBackupValid=true});
}
File.WriteAllText(Path.Combine(root,"restore-results.json"),JsonSerializer.Serialize(restoreResults,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine(JsonSerializer.Serialize(restoreResults));
