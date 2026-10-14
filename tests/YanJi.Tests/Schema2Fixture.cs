using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;
namespace YanJi.Tests;
// Frozen from f5f189a; never derives Schema2 fixtures from the new Schema3 code.
internal static class Schema2Fixture
{
    internal const string Sql = """
CREATE TABLE exam_profiles(id TEXT PRIMARY KEY,name TEXT NOT NULL,exam_key TEXT NOT NULL,admission_year INTEGER NOT NULL,exam_date TEXT);
CREATE TABLE exam_subjects(profile_id TEXT NOT NULL REFERENCES exam_profiles(id),subject_id TEXT NOT NULL REFERENCES subjects(id),PRIMARY KEY(profile_id,subject_id));
ALTER TABLE chapters ADD COLUMN profile_id TEXT REFERENCES exam_profiles(id);
ALTER TABLE chapters ADD COLUMN parent_id TEXT REFERENCES chapters(id);
ALTER TABLE chapters ADD COLUMN node_kind INTEGER NOT NULL DEFAULT 0 CHECK(node_kind IN(0,1));
ALTER TABLE chapters ADD COLUMN description TEXT NOT NULL DEFAULT '';
ALTER TABLE chapters ADD COLUMN sort_order INTEGER NOT NULL DEFAULT 0 CHECK(sort_order>=0);
ALTER TABLE chapters ADD COLUMN learning_state INTEGER NOT NULL DEFAULT 0 CHECK(learning_state BETWEEN 0 AND 3);
ALTER TABLE chapters ADD COLUMN row_version INTEGER NOT NULL DEFAULT 1 CHECK(row_version>0);
CREATE INDEX chapters_scope_parent ON chapters(profile_id,subject,parent_id,sort_order,id);
CREATE INDEX chapters_parent ON chapters(parent_id);
CREATE INDEX sessions_chapter ON sessions(chapter);
CREATE INDEX revisions_kind_entity ON revisions(kind,entity,at);
CREATE TABLE schema_migrations(target_version INTEGER PRIMARY KEY,script_digest TEXT NOT NULL,applied_at TEXT NOT NULL);
""";
    internal static string Create()
    {
        var root=LegacySchemaFixture.Create();
        using var c=new SqliteConnection($"Data Source={Path.Combine(root,"yanji.sqlite")};Pooling=False");c.Open();using var tx=c.BeginTransaction();
        using var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText=Sql;cmd.ExecuteNonQuery();
        cmd.CommandText="INSERT INTO exam_profiles VALUES('legacy-default','2028 考研 · 历史默认档案','cn-postgraduate',2028,NULL); INSERT INTO exam_subjects SELECT 'legacy-default',id FROM subjects; UPDATE chapters SET profile_id='legacy-default',sort_order=rowid,learning_state=CASE WHEN progress=0 THEN 0 WHEN progress=100 THEN 2 ELSE 1 END; INSERT INTO schema_migrations VALUES(2,$digest,$at); PRAGMA user_version=2;";
        cmd.Parameters.AddWithValue("$digest",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Sql))));cmd.Parameters.AddWithValue("$at",DateTimeOffset.UtcNow.ToString("O"));cmd.ExecuteNonQuery();tx.Commit();
        File.WriteAllText(Path.Combine(root,"m2-isolated-fixture.txt"),"YanJi M2 synthetic");return root;
    }
}
