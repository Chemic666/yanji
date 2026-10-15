using Microsoft.Data.Sqlite;
using System.Security.Cryptography;
using System.Text;
using YanJi.Core;
namespace YanJi.Data;

public sealed partial class YanJiDatabase
{
    internal Action<string>? PackageCheckpoint {get;set;}
    private static List<PackageInstall> ReadInstalls(SqliteConnection c,SqliteTransaction? tx=null)
    {
        using var cmd=Command(c,tx,"SELECT id,profile_id,package_id,version,targets,row_version FROM knowledge_package_installs ORDER BY id");using var r=cmd.ExecuteReader();List<PackageInstall> list=[];
        while(r.Read())list.Add(new(r.GetString(0),r.GetString(1),r.GetString(2),r.GetString(3),Parse<Dictionary<string,string>>(r.GetString(4)),r.GetInt64(5)));return list;
    }
    private static List<PackageBinding> ReadBindings(SqliteConnection c,string install,SqliteTransaction? tx=null)
    {
        using var cmd=Command(c,tx,"SELECT source_id,node_id,active FROM knowledge_package_nodes WHERE install_id=$id ORDER BY source_id",("$id",install));using var r=cmd.ExecuteReader();List<PackageBinding> list=[];
        while(r.Read())list.Add(new(r.GetString(0),r.GetString(1),r.GetBoolean(2)));return list;
    }
    public List<PackageInstall> InstalledPackages() {lock(_gate){using var c=Open();return ReadInstalls(c);}}
    public List<PackageOperation> PackageHistory(string installId)
    {
        lock(_gate){using var c=Open();using var cmd=Command(c,null,"SELECT payload FROM knowledge_package_operations WHERE install_id=$id ORDER BY at DESC,rowid DESC",("$id",installId));using var r=cmd.ExecuteReader();List<PackageOperation> list=[];while(r.Read())list.Add(Parse<PackageOperation>(r.GetString(0)));return list;}
    }
    private static void ValidateTargets(SqliteConnection c,SqliteTransaction? tx,KnowledgePackage p,string profileId,IReadOnlyDictionary<string,string> targets,bool allowArchived=false)
    {
        using var cmd=Command(c,tx,"SELECT exam_key,admission_year FROM exam_profiles WHERE id=$id",("$id",profileId));using var r=cmd.ExecuteReader();
        if(!r.Read()||r.GetString(0)!=p.ExamKey||p.AdmissionYears.Count>0&&!p.AdmissionYears.Contains(r.GetInt32(1)))throw new InvalidDataException("知识包不适用于所选考试档案或入学年度。");r.Close();
        if(!p.Subjects.Select(s=>s.Key).Order().SequenceEqual(targets.Keys.Order())||targets.Values.Distinct().Count()!=targets.Count)throw new InvalidDataException("请为每个包科目选择不同的目标科目。");
        foreach(var id in targets.Values)if(Scalar(c,tx,"SELECT 1 FROM exam_subjects e JOIN subjects s ON e.subject_id=s.id WHERE e.profile_id=$p AND e.subject_id=$s AND ($archived=1 OR s.archived=0)",("$p",profileId),("$s",id),("$archived",allowArchived))==null)throw new InvalidDataException("目标科目不属于档案或已归档。");
    }
    private string PackageToken(SqliteConnection c,SqliteTransaction? tx=null)
    {
        using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);hash.AppendData(Encoding.UTF8.GetBytes(_knowledgeEpoch.ToString()));
        // Covers all independent edits/references, not a second mutable generation store.
        foreach(var table in new[]{"chapters","subjects","exam_profiles","exam_subjects","knowledge_package_installs","knowledge_package_nodes","knowledge_package_operations","sessions","parts","tasks","weakpoints"})
        {
            using var cmd=Command(c,tx,$"SELECT * FROM {table} ORDER BY rowid");using var r=cmd.ExecuteReader();hash.AppendData(Encoding.UTF8.GetBytes(table));
            while(r.Read())hash.AppendData(Encoding.UTF8.GetBytes(Json(Enumerable.Range(0,r.FieldCount).Select(i=>r.IsDBNull(i)?null:r.GetValue(i)).ToArray())));
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
    private static string StableId(string prefix,string value)=>prefix+Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..32].ToLowerInvariant();
    private static bool SameMetadata(KnowledgeNode a,KnowledgeNode b)=>a.Name==b.Name&&a.Description==b.Description&&a.ParentId==b.ParentId&&a.Kind==b.Kind&&a.SortOrder==b.SortOrder&&a.Archived==b.Archived;
    private static readonly string[] PackageFields=["Name","Description","ParentId","Kind","SortOrder"];
    private static string? Field(KnowledgeNode n,string field)=>field switch{"Name"=>n.Name,"Description"=>n.Description,"ParentId"=>n.ParentId,"Kind"=>n.Kind.ToString(),"SortOrder"=>n.SortOrder.ToString(System.Globalization.CultureInfo.InvariantCulture),"Archived"=>n.Archived.ToString(),_=>throw new ArgumentException("字段无效")};
    private static KnowledgeNode WithField(KnowledgeNode n,string field,string? value)=>field switch{"Name"=>n with{Name=value!},"Description"=>n with{Description=value!},"ParentId"=>n with{ParentId=value},"Kind"=>n with{Kind=Enum.Parse<KnowledgeNodeKind>(value!)},"SortOrder"=>n with{SortOrder=int.Parse(value!,System.Globalization.CultureInfo.InvariantCulture)},"Archived"=>n with{Archived=bool.Parse(value!)},_=>n};
    private static HashSet<string> UsedNodes(SqliteConnection c)
    {
        HashSet<string> used=[];
        using(var cmd=Command(c,null,"SELECT chapter FROM sessions WHERE chapter IS NOT NULL"))using(var r=cmd.ExecuteReader())while(r.Read())used.Add(r.GetString(0));
        using(var cmd=Command(c,null,"SELECT payload FROM weakpoints"))using(var r=cmd.ExecuteReader())while(r.Read()){var weak=Parse<WeakPoint>(r.GetString(0));if(weak.ChapterId!=null)used.Add(weak.ChapterId);}
        using(var cmd=Command(c,null,"SELECT payload FROM tasks"))using(var r=cmd.ExecuteReader())while(r.Read()){var task=Parse<LearningTask>(r.GetString(0));if(task.ChapterId!=null)used.Add(task.ChapterId);}
        return used;
    }
    private static PackagePreview WithReferenceCounts(SqliteConnection c,PackagePreview preview)
    {
        Dictionary<string,int> counts=[];
        using(var cmd=Command(c,null,"SELECT chapter,COUNT(*) FROM sessions WHERE chapter IS NOT NULL GROUP BY chapter"))using(var r=cmd.ExecuteReader())while(r.Read())counts[r.GetString(0)]=r.GetInt32(1);
        using(var cmd=Command(c,null,"SELECT payload FROM weakpoints"))using(var r=cmd.ExecuteReader())while(r.Read()){var weak=Parse<WeakPoint>(r.GetString(0));if(weak.ChapterId!=null)counts[weak.ChapterId]=counts.GetValueOrDefault(weak.ChapterId)+1;}
        using(var cmd=Command(c,null,"SELECT payload FROM tasks"))using(var r=cmd.ExecuteReader())while(r.Read()){var task=Parse<LearningTask>(r.GetString(0));if(task.ChapterId!=null)counts[task.ChapterId]=counts.GetValueOrDefault(task.ChapterId)+1;}
        return preview with{Changes=preview.Changes.Select(change=>change with{HistoricalReferences=counts.GetValueOrDefault(change.After.Id)}).ToList()};
    }
    private static KnowledgeNode TemplateNode(PackageNode source,string profile,IReadOnlyDictionary<string,string> targets,IReadOnlyDictionary<string,PackageBinding> bindings)
        =>new(bindings[source.Id].NodeId,targets[source.SubjectKey],source.Name,source.ParentId==null?null:bindings[source.ParentId].NodeId,source.Kind,source.Order,Description:source.Description,ProfileId:profile);
    private static KnowledgeNode MergeNode(KnowledgeNode baseline,KnowledgeNode local,KnowledgeNode incoming,string sourceId,List<PackageConflict> conflicts,IReadOnlyDictionary<string,PackageChoice>? choices,bool rollback=false)
    {
        var result=local;
        foreach(var field in rollback?PackageFields.Append("Archived"):PackageFields)
        {
            var b=Field(baseline,field);var l=Field(local,field);var u=Field(incoming,field);
            if(l==b)result=WithField(result,field,u);
            else if(u!=b&&l!=u)
            {
                var key=sourceId+":"+field;conflicts.Add(new(key,local.Name,field,b,l,u));
                // Later personal notes are never reversed by directory undo.
                if(!(rollback&&field=="Description")&&choices?.GetValueOrDefault(key)==PackageChoice.UseTemplate)result=WithField(result,field,u);
            }
        }
        return result;
    }
    public PackagePreview PreviewPackage(KnowledgePackage package,string profileId,IReadOnlyDictionary<string,string> targets,IReadOnlyDictionary<string,PackageChoice>? choices=null)
    {
        lock(_gate)
        {
            // Encode validates even callers that did not import a file; parse fixes the trusted digest.
            var payload=KnowledgePackageFile.Encode(package);var p=KnowledgePackageFile.Parse(Encoding.UTF8.GetBytes(payload));
            using var c=Open();ValidateTargets(c,null,p,profileId,targets);
            var known=Scalar(c,null,"SELECT digest FROM knowledge_package_versions WHERE package_id=$p AND version=$v",("$p",p.PackageId),("$v",p.PackageVersion));
            if(known!=null&&!Equals(known,p.ContentHash))throw new InvalidDataException("同一包版本对应不同内容；请提升版本号或使用独立PackageId。");
            if(Scalar(c,null,"SELECT source_identity FROM knowledge_package_versions WHERE package_id=$p LIMIT 1",("$p",p.PackageId)) is string source&&source!=SourceIdentity(p))throw new InvalidDataException("包来源身份与已安装版本冲突，请核查维护方和来源标识。");
            var install=ReadInstalls(c).SingleOrDefault(i=>i.ProfileId==profileId&&i.PackageId==p.PackageId);
            var id=install?.Id??StableId("kp-",profileId+"/"+p.PackageId);var token=PackageToken(c);
            if(install!=null&&!install.Targets.OrderBy(x=>x.Key).SequenceEqual(targets.OrderBy(x=>x.Key)))throw new InvalidDataException("更新不能改变已安装的科目绑定。");
            var nodes=ReadKnowledgeNodes(c);KnowledgeTree.Validate(nodes);var map=nodes.ToDictionary(n=>n.Id);var bindings=ReadBindings(c,id).ToDictionary(b=>b.SourceId);
            KnowledgePackage? baseline=null;
            if(install!=null)
            {
                baseline=KnowledgePackageFile.Parse(Encoding.UTF8.GetBytes((string)Scalar(c,null,"SELECT payload FROM knowledge_package_versions WHERE package_id=$p AND version=$v",("$p",install.PackageId),("$v",install.Version))!));
                var cmp=KnowledgePackageFile.VersionOf(p.PackageVersion).CompareTo(KnowledgePackageFile.VersionOf(install.Version));
                if(cmp<0)throw new InvalidDataException("不能通过导入降级；请使用目录回滚预览。");
                if(cmp==0)return new(payload,profileId,new(targets),token,id,null,[],[],bindings.Values.ToList(),true);
            }
            foreach(var n in p.Nodes)if(!bindings.ContainsKey(n.Id))
            {
                var local=StableId("kn-",id+"/"+n.Id);if(map.ContainsKey(local))throw new InvalidDataException("目标节点身份已存在但未绑定，不能自动合并。");bindings.Add(n.Id,new(n.Id,local,true));
            }
            List<PackageNodeChange> changes=[];List<PackageConflict> conflicts=[];var used=UsedNodes(c);var incomingIds=p.Nodes.Select(n=>n.Id).ToHashSet();var oldSources=baseline?.Nodes.ToDictionary(n=>n.Id)??[];
            Dictionary<string,PackageNode> previousSources=[];
            if(p.Nodes.Any(n=>bindings.TryGetValue(n.Id,out var binding)&&map.ContainsKey(binding.NodeId)&&!oldSources.ContainsKey(n.Id)))
                foreach(var op in PackageHistory(id))foreach(var snapshot in new[]{op.AfterPayload,op.BeforePayload}.Where(text=>text!=null))
                    foreach(var sourceNode in KnowledgePackageFile.Parse(Encoding.UTF8.GetBytes(snapshot!)).Nodes)previousSources.TryAdd(sourceNode.Id,sourceNode);
            foreach(var n in p.Nodes)
            {
                var b=bindings[n.Id];var incoming=TemplateNode(n,profileId,targets,bindings);
                if(!map.TryGetValue(b.NodeId,out var local)){changes.Add(new(null,incoming,"新增"));continue;}
                if(targets[n.SubjectKey]!=local.SubjectId)throw new InvalidDataException("稳定外部节点不能跨科目迁移。");
                if(!b.Active||local.Archived)
                {
                    conflicts.Add(new(n.Id+":Binding",local.Name,"Binding",b.Active?"模板绑定":"保留解绑",local.Archived?"本地归档":"本地保留", "重新纳入模板"));
                    // Preserve explicit archive/detach intention. Rebinding requires a separate explicit choice.
                    if(choices?.GetValueOrDefault(n.Id+":Binding")!=PackageChoice.UseTemplate)continue;
                }
                if(oldSources.TryGetValue(n.Id,out var old))
                {
                    if(targets[n.SubjectKey]!=local.SubjectId)throw new InvalidDataException("稳定外部节点不能跨科目迁移。");
                    var merged=MergeNode(TemplateNode(old,profileId,targets,bindings),local,incoming,n.Id,conflicts,choices);
                    if(!SameMetadata(local,merged))changes.Add(new(local,merged,"更新目录"));
                }
                else
                {
                    // Rebinding is not consent to replace personal metadata. Recover the last template baseline.
                    KnowledgeNode merged;
                    if(previousSources.TryGetValue(n.Id,out var previous))merged=MergeNode(TemplateNode(previous,profileId,targets,bindings),local,incoming,n.Id,conflicts,choices);
                    else
                    {
                        merged=local;
                        foreach(var field in PackageFields)if(Field(local,field)!=Field(incoming,field))
                        {var key=n.Id+":"+field;conflicts.Add(new(key,local.Name,field,"基线不可用",Field(local,field),Field(incoming,field)));if(choices?.GetValueOrDefault(key)==PackageChoice.UseTemplate)merged=WithField(merged,field,Field(incoming,field));}
                    }
                    if(!SameMetadata(local,merged))changes.Add(new(local,merged,"重新纳入"));
                }
                bindings[n.Id]=b with{Active=true};
            }
            var removed=bindings.Values.Where(b=>!incomingIds.Contains(b.SourceId)).ToList();
            var protectedIds=new HashSet<string>(used);
            foreach(var b in removed)
            {
                var local=map[b.NodeId];var old=oldSources.GetValueOrDefault(b.SourceId);
                if(old==null||!SameMetadata(local,TemplateNode(old,profileId,targets,bindings))||local.Progress!=0||local.State!=LearningState.NotStarted||local.Mastery!=Mastery.Unknown)protectedIds.Add(local.Id);
            }
            var removedIds=removed.Select(b=>b.NodeId).ToHashSet();
            foreach(var local in nodes.Where(n=>!removedIds.Contains(n.Id)||protectedIds.Contains(n.Id)))
                for(var parent=local.ParentId;parent!=null;parent=map[parent].ParentId)protectedIds.Add(parent);
            foreach(var b in removed)
            {
                var local=map[b.NodeId];bindings[b.SourceId]=b with{Active=false};
                if(!protectedIds.Contains(local.Id)&&!local.Archived)changes.Add(new(local,local with{Archived=true},"上游移除：归档"));
                else conflicts.Add(new(b.SourceId+":Removal",local.Name,"Removal","模板节点","保留历史/本地内容","上游移除；保留并解绑"));
            }
            return WithReferenceCounts(c,FinalPreview(payload,profileId,targets,token,id,null,changes,conflicts,bindings.Values.ToList(),nodes));
        }
    }
    private static PackagePreview FinalPreview(string payload,string profile,IReadOnlyDictionary<string,string> targets,string token,string id,string? rollback,List<PackageNodeChange> changes,List<PackageConflict> conflicts,List<PackageBinding> bindings,List<KnowledgeNode> nodes)
    {
        var map=nodes.ToDictionary(n=>n.Id);foreach(var change in changes)map[change.After.Id]=change.After;string? error=null;
        try{KnowledgeTree.Validate(map.Values.ToList());}catch(ArgumentException e){error="合并后的目录不可用："+e.Message;}
        return new(payload,profile,new(targets),token,id,rollback,changes,conflicts,bindings,false,error);
    }
    public PackagePreview PreviewPackageRollback(string installId,IReadOnlyDictionary<string,PackageChoice>? choices=null)
    {
        lock(_gate)
        {
            using var c=Open();var install=ReadInstalls(c).SingleOrDefault(i=>i.Id==installId)??throw new InvalidDataException("未找到已安装知识包。");
            var operation=PackageHistory(installId).FirstOrDefault(o=>!o.RolledBack&&o.Kind!="rollback")??throw new InvalidOperationException("没有可回滚的目录操作。");
            if(operation.ToVersion!=install.Version)throw new InvalidOperationException("当前版本与可回滚操作不一致。");
            var nodes=ReadKnowledgeNodes(c);var map=nodes.ToDictionary(n=>n.Id);var used=UsedNodes(c);List<PackageNodeChange> changes=[];List<PackageConflict> conflicts=[];
            var bindings=ReadBindings(c,installId).ToDictionary(b=>b.SourceId);var beforeBinding=operation.BeforeBindings.ToDictionary(b=>b.SourceId);
            var added=operation.Changes.Where(ch=>ch.Before==null).Select(ch=>ch.After.Id).ToHashSet();var protectedIds=new HashSet<string>(used);
            foreach(var ch in operation.Changes.Where(ch=>ch.Before==null))
            {var local=map[ch.After.Id];if(!SameMetadata(local,ch.After)||local.Progress!=0||local.State!=LearningState.NotStarted||local.Mastery!=Mastery.Unknown)protectedIds.Add(local.Id);}
            foreach(var local in nodes.Where(n=>!added.Contains(n.Id)||protectedIds.Contains(n.Id)))for(var parent=local.ParentId;parent!=null;parent=map[parent].ParentId)protectedIds.Add(parent);
            foreach(var ch in operation.Changes)
            {
                var local=map[ch.After.Id];var source=bindings.Values.Single(b=>b.NodeId==local.Id).SourceId;
                if(ch.Before==null)
                {
                    bindings[source]=bindings[source] with{Active=false};
                    if(!protectedIds.Contains(local.Id)&&!local.Archived)changes.Add(new(local,local with{Archived=true},"回滚新增：归档"));
                    else conflicts.Add(new(source+":RollbackAdded",local.Name,"Removal","新增","保留后来学习/用户内容","解绑并保留"));
                }
                else
                {
                    var merged=MergeNode(ch.After,local,ch.Before,source,conflicts,choices,true);
                    if(!SameMetadata(local,merged))changes.Add(new(local,merged,"目录逆向修改"));
                }
            }
            foreach(var b in beforeBinding.Values)bindings[b.SourceId]=b;
            // Undo of the initial directory installation keeps version/source history and tombstones.
            // Importing identical content is still a no-op: it must not silently resurrect archived nodes.
            return WithReferenceCounts(c,FinalPreview(operation.BeforePayload??operation.AfterPayload,install.ProfileId,install.Targets,PackageToken(c),installId,operation.Id,changes,conflicts,bindings.Values.ToList(),nodes));
        }
    }
    public PackageApplyResult ApplyPackage(PackagePreview preview,IReadOnlyDictionary<string,PackageChoice>? choices=null)
    {
        lock(_gate)
        {
            using(var check=Open())if(PackageToken(check)!=preview.Token)throw new InvalidOperationException("目录或学习数据已变化；请重新生成预览。");
            var p=KnowledgePackageFile.Parse(Encoding.UTF8.GetBytes(preview.Payload));
            var current=preview.RollbackOperationId==null?PreviewPackage(p,preview.ProfileId,preview.Targets,choices):PreviewPackageRollback(preview.InstallId,choices);
            if(current.RollbackOperationId!=preview.RollbackOperationId||current.InstallId!=preview.InstallId||current.Token!=preview.Token)throw new InvalidOperationException("预览已失效。");
            p=KnowledgePackageFile.Parse(Encoding.UTF8.GetBytes(current.Payload));
            if(current.Error!=null)throw new InvalidDataException(current.Error);
            if(current.NoOp)return new(current.InstallId,null,null,true);
            if(LoadTimer()!=null)throw new InvalidOperationException("请先结束或核对活动计时再安装或更新知识包。");
            var folder=Path.Combine(DirectoryPath,"backups");Directory.CreateDirectory(folder);var backup=Path.Combine(folder,"before-package-"+Guid.NewGuid().ToString("N")+".yanjibak");Backup(backup);ValidateBackup(backup);
            using var c=Open();using var tx=c.BeginTransaction();
            if(PackageToken(c,tx)!=preview.Token)throw new InvalidOperationException("备份期间数据已变化，请重新预览。");
            if(Scalar(c,tx,"SELECT payload FROM active_timer")!=null)throw new InvalidOperationException("计时活动期间不能应用目录。");
            var old=ReadInstalls(c,tx).SingleOrDefault(i=>i.Id==current.InstallId);var oldBindings=ReadBindings(c,current.InstallId,tx);
            var beforePayload=old==null?null:(string)Scalar(c,tx,"SELECT payload FROM knowledge_package_versions WHERE package_id=$id AND version=$v",("$id",old.PackageId),("$v",old.Version))!;
            var catalog=ReadKnowledgeNodes(c,tx).ToDictionary(n=>n.Id);
            Exec(c,tx,"PRAGMA defer_foreign_keys=ON");List<PackageNodeChange> saved=[];
            foreach(var change in current.Changes)
            {
                var n=change.After with{Version=(change.Before?.Version??0)+1};WriteKnowledgeNode(c,tx,n);catalog[n.Id]=n;saved.Add(change with{After=n});
                Audit(c,tx,"knowledge-node",n.Id,n.Version,"知识包目录变更",change.Before==null?null:Json(change.Before),Json(n));
            }
            KnowledgeTree.Validate(catalog.Values.ToList());PackageCheckpoint?.Invoke("after-nodes");
            Exec(c,tx,"INSERT OR IGNORE INTO knowledge_package_versions VALUES($id,$v,$hash,$source,$payload)",("$id",p.PackageId),("$v",p.PackageVersion),("$hash",p.ContentHash),("$source",SourceIdentity(p)),("$payload",KnowledgePackageFile.Encode(p)));
            Exec(c,tx,"INSERT INTO knowledge_package_installs VALUES($id,$profile,$package,$v,$targets,$stamp) ON CONFLICT(id) DO UPDATE SET version=excluded.version,row_version=excluded.row_version",
                ("$id",current.InstallId),("$profile",current.ProfileId),("$package",p.PackageId),("$v",p.PackageVersion),("$targets",Json(current.Targets)),("$stamp",(old?.VersionStamp??0)+1));
            foreach(var b in current.Bindings)Exec(c,tx,"INSERT INTO knowledge_package_nodes VALUES($id,$source,$node,$active) ON CONFLICT(install_id,source_id) DO UPDATE SET active=excluded.active",("$id",current.InstallId),("$source",b.SourceId),("$node",b.NodeId),("$active",b.Active));
            if(current.RollbackOperationId!=null)
            {
                var raw=(string)Scalar(c,tx,"SELECT payload FROM knowledge_package_operations WHERE id=$id",("$id",current.RollbackOperationId))!;
                var reversed=Parse<PackageOperation>(raw) with{RolledBack=true};Exec(c,tx,"UPDATE knowledge_package_operations SET rolled_back=1,payload=$payload WHERE id=$id",("$id",reversed.Id),("$payload",Json(reversed)));
            }
            var operation=new PackageOperation(Guid.NewGuid().ToString("N"),current.InstallId,current.RollbackOperationId!=null?"rollback":old==null?"install":"update",old?.Version,p.PackageVersion,beforePayload,KnowledgePackageFile.Encode(p),oldBindings,current.Bindings,saved,DateTimeOffset.UtcNow);
            Exec(c,tx,"INSERT INTO knowledge_package_operations VALUES($id,$install,$at,0,$payload)",("$id",operation.Id),("$install",operation.InstallId),("$at",operation.At.ToString("O")),("$payload",Json(operation)));
            Audit(c,tx,"knowledge-package",current.InstallId,(old?.VersionStamp??0)+1,"知识包"+operation.Kind,beforePayload,operation.AfterPayload);
            PackageCheckpoint?.Invoke("before-commit");tx.Commit();return new(current.InstallId,operation.Id,backup,false);
        }
    }
    private static void WriteKnowledgeNode(SqliteConnection c,SqliteTransaction tx,KnowledgeNode n)
    {
        Exec(c,tx,"""
            INSERT INTO chapters(id,subject,name,progress,mastery,archived,profile_id,parent_id,node_kind,description,sort_order,learning_state,row_version)
            VALUES($id,$s,$name,$progress,$mastery,$archived,$profile,$parent,$kind,$description,$order,$state,$version)
            ON CONFLICT(id) DO UPDATE SET name=excluded.name,progress=excluded.progress,mastery=excluded.mastery,archived=excluded.archived,
                parent_id=excluded.parent_id,node_kind=excluded.node_kind,description=excluded.description,sort_order=excluded.sort_order,learning_state=excluded.learning_state,row_version=excluded.row_version
            """,("$id",n.Id),("$s",n.SubjectId),("$name",n.Name),("$progress",n.Progress),("$mastery",(int)n.Mastery),("$archived",n.Archived),("$profile",n.ProfileId),("$parent",n.ParentId),("$kind",(int)n.Kind),("$description",n.Description),("$order",n.SortOrder),("$state",(int)n.State),("$version",n.Version));
    }
}
