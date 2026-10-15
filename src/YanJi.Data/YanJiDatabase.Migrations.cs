using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace YanJi.Data;

public sealed record IsolatedMigrationResult(string BackupPath, string RollbackPath, string EvidenceDirectory,
    string BeforeFingerprint, string AfterFingerprint);

public sealed partial class YanJiDatabase
{
    private static void CheckpointBeforeReplace(SqliteConnection c)
    {
        using var cmd = Command(c, null, "PRAGMA wal_checkpoint(TRUNCATE)"); using var r = cmd.ExecuteReader();
        if (!r.Read() || r.GetInt32(0) != 0 || r.GetInt32(1) != r.GetInt32(2))
            throw new IOException("数据库仍有未完成写入或被占用，未替换文件。");
    }
    public static string LegacyFactFingerprint(string path)
    {
        using var c = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()); c.Open();
        return LegacyFactFingerprint(c);
    }
    private static string LegacyFactFingerprint(SqliteConnection c)
    {
        // Ignore only new M1 columns, never old payload text, IDs, timestamps or millisecond facts.
        var tables = new Dictionary<string, string>
        {
            ["subjects"] = "id,name,archived", ["chapters"] = "id,subject,name,progress,mastery,archived",
            ["sessions"] = "id,subject,chapter,source,note,version,deleted,created",
            ["parts"] = "id,session,date,duration,zone,kind,start,end",
            ["revisions"] = "id,kind,entity,version,at,reason,before_json,after_json", ["active_timer"] = "slot,payload",
            ["tasks"] = "id,payload", ["weakpoints"] = "id,payload", ["reflections"] = "id,payload", ["settings"] = "id,payload"
        };
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var (table, columns) in tables)
        {
            hash.AppendData(Encoding.UTF8.GetBytes(table + "\n"));
            using var cmd = Command(c, null, $"SELECT {columns} FROM {table} ORDER BY {(table == "active_timer" ? "slot" : "id")}");
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var values = Enumerable.Range(0, r.FieldCount).Select(i => r.IsDBNull(i) ? null : r.GetValue(i)).ToArray();
                hash.AppendData(Encoding.UTF8.GetBytes(Json(values) + "\n"));
            }
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    /// <summary>M1 experiment only: requires an explicit synthetic-fixture marker; never called by normal startup.</summary>
    public static IsolatedMigrationResult UpgradeLegacyIsolated(string directory) => UpgradeLegacyIsolated(directory, null);
    internal static IsolatedMigrationResult UpgradeLegacyIsolated(string directory, Action<string>? checkpoint)
    {
        directory = Path.GetFullPath(directory);
        var marker = Path.Combine(directory, "m1-isolated-fixture.txt");
        if (!File.Exists(marker) || File.ReadAllText(marker) != "YanJi M1 synthetic")
            throw new InvalidOperationException("M1只允许显式标记的合成隔离数据库，不提供真实用户升级入口。");
        var identity = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(directory.TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant())))[..24];
        using var instance = new Mutex(true, "Local\\YanJi." + identity, out var created);
        if (!created) throw new IOException("数据目录正在运行，请先关闭对应隔离程序。");
        try
        {
            using var migrationLock = new FileStream(Path.Combine(directory, "migration.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            using var legacy = new YanJiDatabase(directory, 1);
            using (var c = legacy.Open()) { CheckIntegrity(c); CheckAllData(c); if (Scalar(c, null, "SELECT payload FROM active_timer") != null) throw new InvalidOperationException("请先结束或核对活动计时。"); }
            var evidence = Path.Combine(directory, "migration-evidence", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(evidence);
            var backup = Path.Combine(evidence, "schema1.yanjibak"); legacy.Backup(backup); legacy.ValidateBackup(backup);
            var candidate = Path.Combine(evidence, "candidate.sqlite");
            using (var zip = ZipFile.OpenRead(backup)) zip.GetEntry("database.sqlite")!.ExtractToFile(candidate);
            var before = LegacyFactFingerprint(legacy.DatabasePath);
            string after = ""; var rollback = Path.Combine(evidence, "rollback-schema1.sqlite");
            var journal = Path.Combine(evidence, "operation.json");
            void Stage(string phase)
            {
                var bytes = Encoding.UTF8.GetBytes(Json(new { Phase = phase, Source = legacy.DatabasePath, Candidate = candidate, Backup = backup,
                    Rollback = rollback, BeforeFingerprint = before, AfterFingerprint = after, At = DateTimeOffset.UtcNow }));
                using var f = new FileStream(journal, FileMode.Create, FileAccess.Write, FileShare.Read); f.Write(bytes); f.Flush(true);
            }
            Stage("backup-validated"); checkpoint?.Invoke("backup-validated");
            using (var c = legacy.Open(candidate))
            {
                using (var tx = c.BeginTransaction()) { ApplySchema2(c, tx); ApplySchema3(c,tx); ApplySchema4(c,tx); checkpoint?.Invoke("after-ddl"); tx.Commit(); }
                CheckIdentity(c); CheckIntegrity(c); CheckAllData(c); after = LegacyFactFingerprint(c);
                if (after != before) throw new InvalidDataException("迁移改变了旧学习事实，未替换源数据库。");
            }
            Stage("candidate-validated"); checkpoint?.Invoke("candidate-validated");
            using (var source = legacy.Open()) CheckpointBeforeReplace(source);
            if (LegacyFactFingerprint(legacy.DatabasePath) != before) throw new IOException("迁移期间源数据库发生变化，未替换。");
            Stage("before-replace"); checkpoint?.Invoke("before-replace");
            File.Replace(candidate, legacy.DatabasePath, rollback, true);
            try
            {
                checkpoint?.Invoke("after-replace");
                using var c = legacy.Open(); CheckIdentity(c); CheckIntegrity(c); CheckAllData(c);
                if (LegacyFactFingerprint(c) != before) throw new InvalidDataException("提升后的事实校验失败。");
                Stage("complete");
            }
            catch
            {
                File.Replace(rollback, legacy.DatabasePath, Path.Combine(evidence, "failed-schema2.sqlite"), true);
                Stage("rolled-back"); throw;
            }
            return new(backup, rollback, evidence, before, after);
        }
        finally { instance.ReleaseMutex(); }
    }
}
