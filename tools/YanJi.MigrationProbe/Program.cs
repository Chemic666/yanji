using System.IO.Compression;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using YanJi.Data;
using YanJi.Tests;

if (args.Length < 2) throw new ArgumentException("prepare <owned artifacts folder> | crash <synthetic data folder> <phase> | inspect <synthetic data folder>");
var owned = Path.GetFullPath(args[1]);
if (!owned.StartsWith(Path.GetFullPath("artifacts") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Probe must stay inside project artifacts.");
if(args[0]=="m4-original")
{
    var target=Path.GetFullPath(args[2]);if(!target.StartsWith(Path.GetFullPath("artifacts")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Rollback target must stay in artifacts.");
    Console.WriteLine(YanJiDatabase.RestoreOriginalToNewDirectory(owned,target));
}
else if(args[0]=="m4-upgrade")
{
    var target=Path.GetFullPath(args[2]);if(!target.StartsWith(Path.GetFullPath("artifacts")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Upgrade target must stay in artifacts.");
    var result=YanJiDatabase.PrepareUpgradeCopy(owned,target);File.WriteAllText(Path.Combine(target,"m4-upgrade-result.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));Console.WriteLine(JsonSerializer.Serialize(result));
}
else if (args[0] == "prepare")
{
    Directory.CreateDirectory(owned); Environment.SetEnvironmentVariable("YANJI_M1_TEST_ROOT", owned);
    var root = LegacySchemaFixture.Create(); var result = YanJiDatabase.UpgradeLegacyIsolated(root);
    var oldDir = Path.Combine(owned, "old-schema1"); var newDir = Path.Combine(owned, "new-schema2");
    Directory.CreateDirectory(oldDir); Directory.CreateDirectory(newDir);
    File.Copy(result.RollbackPath, Path.Combine(oldDir, "yanji.sqlite"));
    using (var db = new YanJiDatabase(root))
    { var zip = Path.Combine(owned, "schema2.yanjibak"); db.Backup(zip); using var archive = ZipFile.OpenRead(zip); archive.GetEntry("database.sqlite")!.ExtractToFile(Path.Combine(newDir, "yanji.sqlite")); }
    var faults = new Dictionary<string, string>();
    foreach (var phase in new[] { "after-ddl", "before-replace", "after-replace" }) faults[phase] = LegacySchemaFixture.Create();
    File.WriteAllText(Path.Combine(owned, "prepared.json"), JsonSerializer.Serialize(new { Root = root, Result = result, OldDirectory = oldDir, NewDirectory = newDir, Faults = faults }, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine("Prepared synthetic databases and validated backups.");
}
else
{
    if (!File.Exists(Path.Combine(owned, "m1-isolated-fixture.txt"))) throw new InvalidOperationException("Missing synthetic marker.");
    if (args[0] == "crash")
        YanJiDatabase.UpgradeLegacyIsolated(owned, phase => { if (phase == args[2]) { Console.WriteLine("Terminating only this probe at " + phase); Console.Out.Flush(); System.Diagnostics.Process.GetCurrentProcess().Kill(); } });
    else if (args[0] == "inspect")
    {
        using var c = new SqliteConnection($"Data Source={Path.Combine(owned, "yanji.sqlite")};Mode=ReadOnly;Pooling=False"); c.Open();
        using var cmd = c.CreateCommand(); cmd.CommandText = "PRAGMA user_version"; var schema = (long)cmd.ExecuteScalar()!;
        cmd.CommandText = "PRAGMA integrity_check"; var integrity = (string)cmd.ExecuteScalar()!;
        if (schema == 2) { using var db = new YanJiDatabase(owned); var path = Path.Combine(owned, "after-crash-validated.yanjibak"); db.Backup(path); db.ValidateBackup(path); }
        var report = new { Schema = schema, Integrity = integrity, Fingerprint = YanJiDatabase.LegacyFactFingerprint(Path.Combine(owned, "yanji.sqlite")),
            Outcome = schema == 1 ? "SOURCE1_INTACT_RETRY_POSSIBLE" : "VALID_SCHEMA2_KEEP_DATA_REVIEW_OPERATION_JOURNAL", RealPower = "NOT TESTED" };
        File.WriteAllText(Path.Combine(owned, "crash-inspection.json"), JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(JsonSerializer.Serialize(report));
    }
    else throw new ArgumentException("Unknown probe mode.");
}
