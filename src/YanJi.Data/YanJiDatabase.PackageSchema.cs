using Microsoft.Data.Sqlite;
using YanJi.Core;
using System.Security.Cryptography;
using System.Text;
namespace YanJi.Data;
public sealed partial class YanJiDatabase
{
    private const string Migration3Sql = """
        CREATE TABLE knowledge_package_versions(package_id TEXT NOT NULL,version TEXT NOT NULL,digest TEXT NOT NULL,source_identity TEXT NOT NULL,payload TEXT NOT NULL,PRIMARY KEY(package_id,version));
        CREATE TABLE knowledge_package_installs(id TEXT PRIMARY KEY,profile_id TEXT NOT NULL REFERENCES exam_profiles(id),package_id TEXT NOT NULL,version TEXT NOT NULL,targets TEXT NOT NULL,row_version INTEGER NOT NULL CHECK(row_version>0),UNIQUE(profile_id,package_id),FOREIGN KEY(package_id,version) REFERENCES knowledge_package_versions(package_id,version));
        CREATE TABLE knowledge_package_nodes(install_id TEXT NOT NULL REFERENCES knowledge_package_installs(id),source_id TEXT NOT NULL,node_id TEXT NOT NULL UNIQUE REFERENCES chapters(id),active INTEGER NOT NULL CHECK(active IN(0,1)),PRIMARY KEY(install_id,source_id));
        CREATE TABLE knowledge_package_operations(id TEXT PRIMARY KEY,install_id TEXT NOT NULL REFERENCES knowledge_package_installs(id),at TEXT NOT NULL,rolled_back INTEGER NOT NULL CHECK(rolled_back IN(0,1)),payload TEXT NOT NULL);
        CREATE INDEX knowledge_package_operations_install ON knowledge_package_operations(install_id,at,id);
        """;
    private static void ApplySchema3(SqliteConnection c,SqliteTransaction tx)
    {
        Exec(c,tx,Migration3Sql);
        Exec(c,tx,"INSERT INTO schema_migrations VALUES(3,$digest,$at)",("$digest",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Migration3Sql.Replace("\r\n","\n"))))),("$at",DateTimeOffset.UtcNow.ToString("O")));
        Exec(c,tx,"PRAGMA user_version=3");
    }
    private static string SourceIdentity(KnowledgePackage p) => Json(new {p.Maintainer, Sources=p.Sources.OrderBy(s=>s.Id,StringComparer.Ordinal).Select(s=>new{s.Id,s.Url}).ToArray()});
    private static void CheckPackageData(SqliteConnection c)
    {
        if(!MigrationDigestMatches(Migration3Sql,Scalar(c,null,"SELECT script_digest FROM schema_migrations WHERE target_version=3")))throw new InvalidDataException("Schema3迁移登记无效。");
        Dictionary<(string,string),KnowledgePackage> versions=[];
        using(var cmd=Command(c,null,"SELECT package_id,version,digest,source_identity,payload FROM knowledge_package_versions"))using(var r=cmd.ExecuteReader())while(r.Read())
        {
            var p=KnowledgePackageFile.Parse(Encoding.UTF8.GetBytes(r.GetString(4)));
            if(p.PackageId!=r.GetString(0)||p.PackageVersion!=r.GetString(1)||p.ContentHash!=r.GetString(2)||SourceIdentity(p)!=r.GetString(3))throw new InvalidDataException("知识包版本快照与身份不一致。");
            versions.Add((p.PackageId,p.PackageVersion),p);
        }
        if(versions.Values.GroupBy(p=>p.PackageId).Any(g=>g.Select(SourceIdentity).Distinct().Count()!=1))throw new InvalidDataException("知识包来源身份冲突。");
        var nodes=ReadKnowledgeNodes(c).ToDictionary(n=>n.Id);
        foreach(var install in ReadInstalls(c))
        {
            var p=versions[(install.PackageId,install.Version)];ValidateTargets(c,null,p,install.ProfileId,install.Targets);
            var bindings=ReadBindings(c,install.Id);var sources=p.Nodes.ToDictionary(n=>n.Id);var bindingIds=bindings.Select(b=>b.SourceId).ToHashSet();
            foreach(var b in bindings)
                if(!nodes.TryGetValue(b.NodeId,out var n)||n.ProfileId!=install.ProfileId||b.Active&&(!sources.TryGetValue(b.SourceId,out var s)||install.Targets[s.SubjectKey]!=n.SubjectId))throw new InvalidDataException("知识包节点映射不一致。");
            if(p.Nodes.Any(n=>!bindingIds.Contains(n.Id)))throw new InvalidDataException("知识包节点映射缺失。");
        }
        using(var cmd=Command(c,null,"SELECT id,install_id,at,rolled_back,payload FROM knowledge_package_operations"))using(var r=cmd.ExecuteReader())while(r.Read())
        {
            var op=Parse<PackageOperation>(r.GetString(4));
            if(op.Id!=r.GetString(0)||op.InstallId!=r.GetString(1)||op.At.ToString("O")!=r.GetString(2)||op.RolledBack!=r.GetBoolean(3)||op.Kind is not("install" or "update" or "rollback"))throw new InvalidDataException("知识包操作历史无效。");
            var after=KnowledgePackageFile.Parse(Encoding.UTF8.GetBytes(op.AfterPayload));if(after.PackageVersion!=op.ToVersion)throw new InvalidDataException("知识包操作版本无效。");
            if(op.BeforePayload!=null){var before=KnowledgePackageFile.Parse(Encoding.UTF8.GetBytes(op.BeforePayload));if(before.PackageVersion!=op.FromVersion||before.PackageId!=after.PackageId)throw new InvalidDataException("知识包操作基线无效。");}
            if(op.Changes.Any(change=>!nodes.TryGetValue(change.After.Id,out var node)||node.SubjectId!=change.After.SubjectId||node.ProfileId!=change.After.ProfileId||change.After.Version<=0||change.Before!=null&&(change.Before.Id!=change.After.Id||change.Before.SubjectId!=node.SubjectId||change.Before.ProfileId!=node.ProfileId)))throw new InvalidDataException("知识包操作引用无效。");
        }
    }
    private static bool MigrationDigestMatches(string sql,object? stored)
    {
        var lf=sql.Replace("\r\n","\n");
        // M1 used the source file's line endings in its digest. Accept only the two exact historical encodings.
        return new[]{lf,lf.Replace("\n","\r\n")}.Any(text=>Equals(stored,Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))));
    }
}
