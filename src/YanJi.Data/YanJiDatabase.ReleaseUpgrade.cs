using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;
using Microsoft.Data.Sqlite;
namespace YanJi.Data;
public sealed record UpgradeCopyResult(int SourceSchema,string TargetDirectory,string BackupPath,string BeforeFingerprint,string AfterFingerprint,long OriginalDurationMs);
public sealed partial class YanJiDatabase
{
    public static UpgradeCopyResult PrepareUpgradeCopy(string sourceDirectory,string targetDirectory)
    {try{return PrepareUpgradeCopy(sourceDirectory,targetDirectory,null,null);}catch(SqliteException e){throw new InvalidDataException("旧数据库损坏、无法读取或正被占用；原目录未被替换。请关闭原版并检查完整备份。",e);}}
    internal static UpgradeCopyResult PrepareUpgradeCopy(string sourceDirectory,string targetDirectory,Action<string>? checkpoint,long? availableBytes)
    {
        sourceDirectory=Path.GetFullPath(sourceDirectory);targetDirectory=Path.GetFullPath(targetDirectory);
        var sourcePath=Path.Combine(sourceDirectory,"yanji.sqlite");
        if(!File.Exists(sourcePath))throw new FileNotFoundException("所选旧目录没有研迹数据库。",sourcePath);
        if((File.GetAttributes(sourcePath)&FileAttributes.ReparsePoint)!=0)throw new IOException("不支持链接数据库，请使用独立的本地文件。");
        if(targetDirectory.StartsWith(sourceDirectory.TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("升级目标必须在旧数据目录之外。");
        EnsureFreshUpgradeTarget(targetDirectory,new FileInfo(sourcePath).Length,availableBytes);
        using var sourceMutex=AcquireUpgradeMutex(sourceDirectory);using var targetMutex=AcquireUpgradeMutex(targetDirectory);
        try
        {
            using var source=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=sourcePath,Mode=SqliteOpenMode.ReadOnly,Pooling=false,DefaultTimeout=1}.ToString());source.Open();
            int schema=Convert.ToInt32(Scalar(source,null,"PRAGMA user_version"));if(schema is <1 or >4)throw new InvalidDataException("不支持的旧数据库版本。");
            CheckIdentity(source,schema);CheckIntegrity(source);CheckAllData(source);
            if(Scalar(source,null,"SELECT payload FROM active_timer")!=null)throw new InvalidOperationException("旧目录含未结束计时，请先在原版中结束或核对，再关闭原版重试。");
            var before=UpgradeFingerprint(sourcePath,schema);var inventory=Inventory(source);
            checkpoint?.Invoke("before-target-reservation");ReserveUpgradeDirectory(targetDirectory);var journal=Path.Combine(targetDirectory,"upgrade-operation.json");
            void Stage(string state,string phase)=>File.WriteAllText(journal,Json(new{State=state,Phase=phase,SourceSchema=schema,SourceDirectory=sourceDirectory,TargetDirectory=targetDirectory,Before=before,At=DateTimeOffset.UtcNow}));
            Stage("Pending","preparing-backup");
            try
            {
                var evidence=Path.Combine(targetDirectory,"upgrade-evidence");Directory.CreateDirectory(evidence);var snapshot=Path.Combine(evidence,"original.sqlite");
                using(var target=new SqliteConnection($"Data Source={snapshot};Pooling=False")){target.Open();source.BackupDatabase(target);Exec(target,null,"PRAGMA journal_mode=DELETE;");}
                if(UpgradeFingerprint(snapshot,schema)!=before)throw new IOException("源数据在生成快照期间发生变化，未执行升级。");
                var backup=Path.Combine(evidence,"before-upgrade.yanjibak");
                using(var c=new SqliteConnection($"Data Source={snapshot};Mode=ReadOnly;Pooling=False"))
                {
                    c.Open();CheckIntegrity(c);CheckAllData(c);var manifest=new BackupManifest(schema,ApplicationId,schema,Hash(snapshot),inventory.Count,inventory.Duration,DateTimeOffset.UtcNow,schema>=2?Convert.ToInt64(Scalar(c,null,"SELECT COUNT(*) FROM chapters")):null,schema>=2?Convert.ToInt64(Scalar(c,null,"SELECT COUNT(*) FROM exam_profiles")):null);
                    using var file=new FileStream(backup,FileMode.CreateNew,FileAccess.Write,FileShare.None);using(var zip=new ZipArchive(file,ZipArchiveMode.Create,true)){zip.CreateEntryFromFile(snapshot,"database.sqlite");using var w=new StreamWriter(zip.CreateEntry("manifest.json").Open(),new UTF8Encoding(false));w.Write(Json(manifest));}file.Flush(true);
                }
                using(var destination=new YanJiDatabase(targetDirectory))
                {
                    destination.ValidateBackup(backup);Stage("Pending","backup-validated");checkpoint?.Invoke("backup-validated");checkpoint?.Invoke("before-restore");
                    destination.RestoreBackup(backup);checkpoint?.Invoke("after-restore");
                    var after=UpgradeFingerprint(destination.DatabasePath,schema);if(after!=before||UpgradeFingerprint(sourcePath,schema)!=before)throw new InvalidDataException("升级前后原有事实不一致；原目录未被替换。");
                    using var c=destination.Open();CheckIdentity(c);CheckIntegrity(c);CheckAllData(c);
                    Stage("Complete","validated");return new(schema,targetDirectory,backup,before,after,inventory.Duration);
                }
            }
            catch{Stage("Failed","keep-source-and-validated-backup");throw;}
        }
        finally{ }
    }
    private static void EnsureFreshUpgradeTarget(string directory,long bytes,long? available=null)
    {
        if(Directory.Exists(directory)||File.Exists(directory))throw new IOException("目标目录已存在；为保护数据，请使用新的目录。重复升级不会覆盖或合并。");
        var root=Path.GetPathRoot(directory)!;if(root.StartsWith("\\\\",StringComparison.Ordinal))throw new IOException("候选版只支持本地磁盘升级目标。");
        if((available??new DriveInfo(root).AvailableFreeSpace)<checked(bytes*4+64L*1024*1024))throw new IOException("可用磁盘空间不足；升级前预留约数据库四倍加64MiB空间，原目录未修改。");
    }
    [DllImport("kernel32.dll",EntryPoint="CreateDirectoryW",CharSet=CharSet.Unicode,SetLastError=true)]
    [return:MarshalAs(UnmanagedType.Bool)]private static extern bool CreateUpgradeDirectory(string path,IntPtr security);
    private static void ReserveUpgradeDirectory(string directory)
    {
        if(!OperatingSystem.IsWindows())throw new PlatformNotSupportedException("升级复制仅在目标Windows平台受支持。");
        Directory.CreateDirectory(Path.GetDirectoryName(directory)!);
        if(!CreateUpgradeDirectory(directory,IntPtr.Zero))throw new IOException("目标目录已出现或无法独占创建；未写入该目录。",new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()));
    }
    private sealed class UpgradeMutexLease(Mutex mutex):IDisposable
    {public void Dispose(){mutex.ReleaseMutex();mutex.Dispose();}}
    private static UpgradeMutexLease AcquireUpgradeMutex(string directory)
    {
        var identity=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(directory.TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant())))[..24];
        var mutex=new Mutex(true,"Local\\YanJi."+identity,out var created);if(created)return new(mutex);mutex.Dispose();throw new IOException("此数据目录正由另一程序使用，请关闭对应研迹窗口和托盘进程。");
    }
    private static string UpgradeFingerprint(string path,int schema)
    {
        var old=schema==1?LegacyFactFingerprint(path):schema==2?Schema2Fingerprint(path):Schema3Fingerprint(path);if(schema<4)return old;
        using var c=new SqliteConnection($"Data Source={path};Mode=ReadOnly;Pooling=False");c.Open();var text=new StringBuilder(old);
        foreach(var table in new[]{"session_context","session_weakpoints"}){using var cmd=Command(c,null,$"SELECT * FROM {table} ORDER BY rowid");using var r=cmd.ExecuteReader();while(r.Read())text.Append(Json(Enumerable.Range(0,r.FieldCount).Select(i=>r.IsDBNull(i)?null:r.GetValue(i)).ToArray()));}
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }
    public static string RestoreOriginalToNewDirectory(string backup,string targetDirectory)=>RestoreOriginalToNewDirectory(backup,targetDirectory,null);
    internal static string RestoreOriginalToNewDirectory(string backup,string targetDirectory,Action<string>? checkpoint)
    {
        targetDirectory=Path.GetFullPath(targetDirectory);var info=new FileInfo(backup);if(!info.Exists)throw new FileNotFoundException("完整备份不存在。");
        EnsureFreshUpgradeTarget(targetDirectory,info.Length);using var instance=AcquireUpgradeMutex(targetDirectory);
        bool owned=false;
        try
        {
            ReserveUpgradeDirectory(targetDirectory);owned=true;File.WriteAllText(Path.Combine(targetDirectory,"upgrade-operation.json"),Json(new{State="Pending",Kind="original-format-restore"}));
            var frozen=Path.Combine(targetDirectory,"backup-validation-input.yanjibak");
            using(var input=new FileStream(backup,FileMode.Open,FileAccess.Read,FileShare.Read))using(var copy=new FileStream(frozen,FileMode.CreateNew,FileAccess.Write,FileShare.None)){input.CopyTo(copy);copy.Flush(true);}
            using var locked=new FileStream(frozen,FileMode.Open,FileAccess.Read,FileShare.Read);
            var validation=Path.Combine(targetDirectory,"validation-work");using(var checker=new YanJiDatabase(validation))checker.ValidateBackup(frozen);
            checkpoint?.Invoke("after-backup-validation");using var zip=ZipFile.OpenRead(frozen);var staged=Path.Combine(targetDirectory,"original-validated.sqlite");zip.GetEntry("database.sqlite")!.ExtractToFile(staged);
            BackupManifest manifest;using(var reader=new StreamReader(zip.GetEntry("manifest.json")!.Open()))manifest=Parse<BackupManifest>(reader.ReadToEnd());
            if(Hash(staged)!=manifest.Sha256)throw new InvalidDataException("校验后备份内容发生变化，未恢复。");
            var result=Path.Combine(targetDirectory,"yanji.sqlite");File.Move(staged,result);File.WriteAllText(Path.Combine(targetDirectory,"upgrade-operation.json"),Json(new{State="Complete",Kind="original-format-restore",Schema=manifest.Schema}));return result;
        }
        catch{if(owned)File.WriteAllText(Path.Combine(targetDirectory,"upgrade-operation.json"),Json(new{State="Failed",Kind="original-format-restore"}));throw;}
    }
    public static void CheckUpgradeReady(string directory)
    {
        var path=Path.Combine(directory,"upgrade-operation.json");if(!File.Exists(path))return;
        using var json=JsonDocument.Parse(File.ReadAllText(path));if(json.RootElement.GetProperty("State").GetString()!="Complete")throw new InvalidDataException("此目录升级未完成；请保留证据，使用原版目录或升级前备份，并选择新的目标目录重试。");
    }
}
