using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;
namespace YanJi.Tests;
// Frozen Schema3 DDL from M2 code commit 430a67b, independent of current migrations.
internal static class Schema3Fixture
{
    internal const string Sql="""
        CREATE TABLE knowledge_package_versions(package_id TEXT NOT NULL,version TEXT NOT NULL,digest TEXT NOT NULL,source_identity TEXT NOT NULL,payload TEXT NOT NULL,PRIMARY KEY(package_id,version));
        CREATE TABLE knowledge_package_installs(id TEXT PRIMARY KEY,profile_id TEXT NOT NULL REFERENCES exam_profiles(id),package_id TEXT NOT NULL,version TEXT NOT NULL,targets TEXT NOT NULL,row_version INTEGER NOT NULL CHECK(row_version>0),UNIQUE(profile_id,package_id),FOREIGN KEY(package_id,version) REFERENCES knowledge_package_versions(package_id,version));
        CREATE TABLE knowledge_package_nodes(install_id TEXT NOT NULL REFERENCES knowledge_package_installs(id),source_id TEXT NOT NULL,node_id TEXT NOT NULL UNIQUE REFERENCES chapters(id),active INTEGER NOT NULL CHECK(active IN(0,1)),PRIMARY KEY(install_id,source_id));
        CREATE TABLE knowledge_package_operations(id TEXT PRIMARY KEY,install_id TEXT NOT NULL REFERENCES knowledge_package_installs(id),at TEXT NOT NULL,rolled_back INTEGER NOT NULL CHECK(rolled_back IN(0,1)),payload TEXT NOT NULL);
        CREATE INDEX knowledge_package_operations_install ON knowledge_package_operations(install_id,at,id);
        """;
    internal static string Create()
    {
        var root=Schema2Fixture.Create();using var c=new SqliteConnection($"Data Source={Path.Combine(root,"yanji.sqlite")};Pooling=False");c.Open();using var tx=c.BeginTransaction();using var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText=Sql;cmd.ExecuteNonQuery();
        cmd.CommandText="INSERT INTO schema_migrations VALUES(3,$hash,$at); PRAGMA user_version=3;";cmd.Parameters.AddWithValue("$hash",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Sql.Replace("\r\n","\n")))));cmd.Parameters.AddWithValue("$at",DateTimeOffset.UtcNow.ToString("O"));cmd.ExecuteNonQuery();tx.Commit();
        File.WriteAllText(Path.Combine(root,"m3-isolated-fixture.txt"),"YanJi M3 synthetic");return root;
    }
}
