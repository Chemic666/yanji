using Microsoft.Data.Sqlite;
using YanJi.Data;

namespace YanJi.Tests;

public sealed class KnowledgeSchemaTests
{
    [Fact]
    public void FreshDatabaseUsesSchema4AndStableChapterStorage()
    {
        var root = M1TestDirectories.NewDirectory();
        using var database = new YanJiDatabase(root);
        using var c = new SqliteConnection($"Data Source={database.DatabasePath};Pooling=False");
        c.Open();
        using var cmd = c.CreateCommand();
        cmd.CommandText = "PRAGMA user_version";
        Assert.Equal(4L, (long)cmd.ExecuteScalar()!);
        cmd.CommandText = "SELECT COUNT(*) FROM pragma_table_info('chapters') WHERE name IN ('parent_id','node_kind','sort_order','learning_state','row_version','profile_id')";
        Assert.Equal(6L, (long)cmd.ExecuteScalar()!);
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name LIKE 'knowledge_package_%'";
        Assert.Equal(4L, (long)cmd.ExecuteScalar()!);
    }
}
