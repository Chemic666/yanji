using System.Text.Json;
namespace YanJi.Tests;
internal static class M1TestDirectories
{
    internal static string NewDirectory()
    {
        var root = Environment.GetEnvironmentVariable("YANJI_M1_TEST_ROOT") ?? Path.Combine(Path.GetTempPath(), "YanJi-tests");
        var path = Path.Combine(root, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(path); return path;
    }
    internal static void Report(string root, string name, object data) => File.WriteAllText(Path.Combine(root, name + ".json"), JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
}
