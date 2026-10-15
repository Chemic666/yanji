using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
namespace YanJi.Data;
public sealed partial class YanJiDatabase
{
    public static IsolatedMigrationResult UpgradeSchema3Isolated(string directory)=>UpgradeSchema3Isolated(directory,null);
    internal static IsolatedMigrationResult UpgradeSchema3Isolated(string directory,Action<string>? checkpoint)
    {
        directory=Path.GetFullPath(directory);
        var marker=Path.Combine(directory,"m3-isolated-fixture.txt");
        if(!File.Exists(marker)||File.ReadAllText(marker)!="YanJi M3 synthetic")throw new InvalidOperationException("只接受明确标记的M3合成隔离数据库；未开放真实数据升级。");
        var identity=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(directory.TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant())))[..24];
        using var instance=new Mutex(true,"Local\\YanJi."+identity,out var created);if(!created)throw new IOException("隔离数据目录正在使用。");
        try
        {
            using var migrationLock=new FileStream(Path.Combine(directory,"migration.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
            using var old=new YanJiDatabase(directory,3);using(var c=old.Open()){CheckIntegrity(c);CheckAllData(c);if(Scalar(c,null,"SELECT payload FROM active_timer")!=null)throw new InvalidOperationException("请先结束或核对活动计时。");}
            var evidence=Path.Combine(directory,"migration-evidence",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(evidence);
            var backup=Path.Combine(evidence,"schema3.yanjibak");old.Backup(backup);old.ValidateBackup(backup);
            var candidate=Path.Combine(evidence,"candidate.sqlite");using(var zip=ZipFile.OpenRead(backup))zip.GetEntry("database.sqlite")!.ExtractToFile(candidate);
            var before=Schema3Fingerprint(old.DatabasePath);var rollback=Path.Combine(evidence,"rollback-schema3.sqlite");string after="";
            void Stage(string stage){using var f=new FileStream(Path.Combine(evidence,"operation.json"),FileMode.Create,FileAccess.Write,FileShare.Read);f.Write(Encoding.UTF8.GetBytes(Json(new{Stage=stage,Before=before,After=after,Backup=backup,Rollback=rollback})));f.Flush(true);}
            Stage("backup-validated");checkpoint?.Invoke("backup-validated");
            using(var c=old.Open(candidate)){using(var tx=c.BeginTransaction()){ApplySchema4(c,tx);checkpoint?.Invoke("after-ddl");tx.Commit();}CheckIdentity(c);CheckIntegrity(c);CheckAllData(c);}
            after=Schema3Fingerprint(candidate);if(before!=after)throw new InvalidDataException("迁移改变了Schema3原有内容。");Stage("candidate-validated");checkpoint?.Invoke("candidate-validated");
            using(var c=old.Open())CheckpointBeforeReplace(c);if(Schema3Fingerprint(old.DatabasePath)!=before)throw new IOException("迁移期间源数据变化。");
            Stage("before-replace");checkpoint?.Invoke("before-replace");File.Replace(candidate,old.DatabasePath,rollback,true);
            try{checkpoint?.Invoke("after-replace");using var c=old.Open();CheckIdentity(c);CheckIntegrity(c);CheckAllData(c);Stage("complete");}
            catch{File.Replace(rollback,old.DatabasePath,Path.Combine(evidence,"failed-schema4.sqlite"),true);Stage("rolled-back");throw;}
            return new(backup,rollback,evidence,before,after);
        }
        finally{instance.ReleaseMutex();}
    }
    internal static string Schema3Fingerprint(string path)
    {
        using var c=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString());c.Open();
        var data=new StringBuilder(LegacyFactFingerprint(c));
        foreach(var table in new[]{"chapters","exam_profiles","exam_subjects","knowledge_package_versions","knowledge_package_installs","knowledge_package_nodes","knowledge_package_operations"})
        {using var cmd=Command(c,null,$"SELECT * FROM {table} ORDER BY rowid");using var r=cmd.ExecuteReader();while(r.Read())data.Append(Json(Enumerable.Range(0,r.FieldCount).Select(i=>r.IsDBNull(i)?null:r.GetValue(i)).ToArray()));}
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(data.ToString())));
    }
}
