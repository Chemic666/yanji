using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace YanJi.App;

/// <summary>Resolve before any database, backup, mutex or diagnostic file access.</summary>
internal sealed record RuntimeLocation(bool TestMode, string DataDirectory)
{
    public string WindowTitle => TitleFor(TestMode);
    public static string TitleFor(bool testMode) => testMode ? "研迹 YanJi · 隔离测试" : "研迹 YanJi";
    public static string FormalDirectory(string localApplicationData)
    {
        if (string.IsNullOrWhiteSpace(localApplicationData) || !Path.IsPathFullyQualified(localApplicationData))
            throw new ArgumentException("无法确定 Windows 用户数据目录，未打开任何学习数据。");
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Combine(localApplicationData, "YanJi")));
    }

    // Pure resolution also allows tests to verify the formal route without touching it.
    public static RuntimeLocation Resolve(bool testMode, string? explicitDirectory, bool diagnostics, string localApplicationData)
    {
        var formal = FormalDirectory(localApplicationData);
        if (!testMode)
        {
            if (explicitDirectory != null || diagnostics)
                throw new ArgumentException("指定数据目录或执行诊断时，必须同时使用 --test-mode 和 --data-dir。");
            return new(false, formal);
        }
        if (string.IsNullOrWhiteSpace(explicitDirectory) || !Path.IsPathFullyQualified(explicitDirectory))
            throw new ArgumentException("隔离测试必须通过 --data-dir 指定独立的绝对路径。");
        var selected = Path.TrimEndingDirectorySeparator(Path.GetFullPath(explicitDirectory));
        if (selected.StartsWith(@"\\", StringComparison.Ordinal))
            throw new ArgumentException("隔离测试请使用本机普通目录，不使用网络共享或设备路径。");
        RejectOverlap(selected, formal);
        return new(true, selected);
    }

    private static void RejectOverlap(string selected, string formal)
    {
        static bool Within(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase)
            || a.StartsWith(Path.TrimEndingDirectorySeparator(b) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        if (Within(selected, formal) || Within(formal, selected))
            throw new ArgumentException("隔离测试路径不能是正式数据目录、其子目录或上级目录。");
    }

    // No directory creation. Check every existing ancestor, including junctions and 8.3 aliases.
    public static void ValidateTestPath(string path, string localApplicationData)
    {
        var selected = Resolve(true, path, false, localApplicationData).DataDirectory;
        var current = selected;
        while (current != null)
        {
            if (File.Exists(current)) throw new ArgumentException("隔离测试路径或其上级是文件，无法作为数据目录。");
            if (Directory.Exists(current))
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new ArgumentException("隔离测试路径不能经过符号链接或目录联接。");
                var longPath = new StringBuilder(32768);
                var count = GetLongPathName(current, longPath, (uint)longPath.Capacity);
                if (count == 0 || count >= longPath.Capacity) throw new IOException("无法核实隔离测试路径，未打开数据。");
                var tail = selected[current.Length..].TrimStart(Path.DirectorySeparatorChar);
                RejectOverlap(Path.GetFullPath(Path.Combine(longPath.ToString(), tail)), FormalDirectory(localApplicationData));
            }
            current = Path.GetDirectoryName(current);
        }
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetLongPathName(string shortPath, StringBuilder longPath, uint capacity);
}
