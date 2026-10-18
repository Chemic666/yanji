using Microsoft.Data.Sqlite;

namespace YanJi.Data;

public sealed partial class YanJiDatabase
{
    /// <summary>Fail closed before the normal writable constructor. Never migrates or creates a database.</summary>
    public static void ValidateStartupDirectory(string directory)
    {
        var root = Path.GetFullPath(directory);
        for (var ancestor = root; ancestor != null; ancestor = Path.GetDirectoryName(ancestor))
            if (Directory.Exists(ancestor) && (File.GetAttributes(ancestor) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("数据目录不能经过符号链接或目录联接，未打开学习数据。");
        if (File.Exists(root)) throw new IOException("数据目录是文件，未打开学习数据。");
        if (!Directory.Exists(root)) return;
        var path = Path.Combine(root, "yanji.sqlite");
        if (!File.Exists(path))
        {
            if (Directory.EnumerateFileSystemEntries(root).Any())
                throw new InvalidDataException("数据目录已有文件但未找到学习数据库，未创建或覆盖数据。请保留目录并检查原有文件。");
            return;
        }
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0 || new FileInfo(path).Length == 0)
            throw new InvalidDataException("数据库文件为空或是链接，未重建、覆盖或升级数据。请使用原有备份排查。");
        using var c = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false, DefaultTimeout = 5
        }.ToString());
        c.Open();
        CheckIdentity(c);
        CheckSchema(c);
        if (!Equals(Scalar(c, null, "PRAGMA quick_check"), "ok"))
            throw new InvalidDataException("数据库完整性检查未通过，未启动写入。请保留原文件并检查备份。");
    }
}
