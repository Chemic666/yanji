using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using YanJi.Core;
namespace YanJi.Data;
public sealed partial class YanJiDatabase
{
    private const string Migration4Sql="""
        CREATE TABLE session_context(session_id TEXT PRIMARY KEY REFERENCES sessions(id) ON DELETE CASCADE,profile_id TEXT NOT NULL REFERENCES exam_profiles(id),task_id TEXT REFERENCES tasks(id),task_title TEXT NOT NULL,node_path TEXT,captured_at TEXT,capture_kind INTEGER NOT NULL CHECK(capture_kind BETWEEN 0 AND 3));
        CREATE TABLE session_weakpoints(session_id TEXT NOT NULL REFERENCES sessions(id) ON DELETE CASCADE,weakpoint_id TEXT NOT NULL REFERENCES weakpoints(id),title_snapshot TEXT NOT NULL,PRIMARY KEY(session_id,weakpoint_id));
        CREATE INDEX session_context_profile_task ON session_context(profile_id,task_id,session_id);
        CREATE INDEX session_weakpoints_weak ON session_weakpoints(weakpoint_id,session_id);
        """;
    private static void ApplySchema4(SqliteConnection c,SqliteTransaction tx)
    {
        var records=ReadRecords(c,tx);Exec(c,tx,Migration4Sql);
        foreach(var record in records)
        {
            var profile=record.ChapterId==null?"legacy-default":(string)Scalar(c,tx,"SELECT profile_id FROM chapters WHERE id=$id",("$id",record.ChapterId))!;
            var context=new LearningContext(profile,record.Focus?.TaskId,record.Focus?.TaskId==null?"":record.Focus.TaskTitle,Capture:ContextCapture.Legacy,WeakPoints:[]);
            WriteStudyContext(c,tx,record.Id,context);
        }
        if(Scalar(c,tx,"SELECT payload FROM active_timer") is string raw)
        {
            var timer=Parse<TimerSnapshot>(raw);var contexts=ReadStudyContexts(c,tx);
            Exec(c,tx,"UPDATE active_timer SET payload=$payload",("$payload",Json(timer with{Context=contexts[timer.SessionId]})));
        }
        Exec(c,tx,"INSERT INTO schema_migrations VALUES(4,$hash,$at)",("$hash",Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Migration4Sql.Replace("\r\n","\n"))))),("$at",DateTimeOffset.UtcNow.ToString("O")));
        Exec(c,tx,"PRAGMA user_version=4");
    }
    private static Dictionary<string,LearningContext> ReadStudyContexts(SqliteConnection c,SqliteTransaction? tx=null)
    {
        Dictionary<string,List<StudyWeakPointRef>> weak=[];
        using(var cmd=Command(c,tx,"SELECT session_id,weakpoint_id,title_snapshot FROM session_weakpoints ORDER BY weakpoint_id"))using(var r=cmd.ExecuteReader())while(r.Read())
        {var id=r.GetString(0);if(!weak.TryGetValue(id,out var refs))weak[id]=refs=[];refs.Add(new(r.GetString(1),r.GetString(2)));}
        Dictionary<string,LearningContext> contexts=[];
        using(var cmd=Command(c,tx,"SELECT session_id,profile_id,task_id,task_title,node_path,captured_at,capture_kind FROM session_context"))using(var r=cmd.ExecuteReader())while(r.Read())
            contexts.Add(r.GetString(0),new(r.GetString(1),r.IsDBNull(2)?null:r.GetString(2),r.GetString(3),r.IsDBNull(4)?null:r.GetString(4),r.IsDBNull(5)?null:DateTimeOffset.Parse(r.GetString(5),CultureInfo.InvariantCulture),(ContextCapture)r.GetInt32(6),weak.GetValueOrDefault(r.GetString(0))??[]));
        return contexts;
    }
    private static void WriteStudyContext(SqliteConnection c,SqliteTransaction tx,string session,LearningContext context)
    {
        Exec(c,tx,"INSERT INTO session_context VALUES($id,$profile,$task,$title,$path,$at,$kind) ON CONFLICT(session_id) DO UPDATE SET profile_id=excluded.profile_id,task_id=excluded.task_id,task_title=excluded.task_title,node_path=excluded.node_path,captured_at=excluded.captured_at,capture_kind=excluded.capture_kind",
            ("$id",session),("$profile",context.ProfileId),("$task",context.TaskId),("$title",context.TaskTitle),("$path",context.NodePath),("$at",context.CapturedAt?.ToString("O")),("$kind",(int)context.Capture));
        Exec(c,tx,"DELETE FROM session_weakpoints WHERE session_id=$id",("$id",session));
        foreach(var reference in context.WeakPoints??[])Exec(c,tx,"INSERT INTO session_weakpoints VALUES($id,$weak,$title)",("$id",session),("$weak",reference.Id),("$title",reference.Title));
    }
    private static void ValidateLearningTarget(SqliteConnection c,SqliteTransaction? tx,string profile,string? subject,string? node,bool requireActive=false)
    {
        if(Scalar(c,tx,"SELECT 1 FROM exam_profiles WHERE id=$id",("$id",profile))==null)throw new ArgumentException("考试档案不存在。");
        if(subject!=null&&Scalar(c,tx,"SELECT 1 FROM exam_subjects WHERE profile_id=$p AND subject_id=$s",("$p",profile),("$s",subject))==null)throw new ArgumentException("科目不属于所选考试档案。");
        if(node!=null)
        {
            using var cmd=Command(c,tx,"SELECT subject,profile_id FROM chapters WHERE id=$id",("$id",node));using var r=cmd.ExecuteReader();
            if(!r.Read()||subject!=r.GetString(0)||profile!=r.GetString(1))throw new ArgumentException("知识节点不属于所选科目或考试档案。");r.Close();
            if(requireActive)
            {var map=ReadKnowledgeNodes(c,tx).ToDictionary(n=>n.Id);for(var current=map[node];;current=map[current.ParentId!]){if(current.Archived)throw new ArgumentException("知识节点或祖先已归档，请明确启用或重新选择。");if(current.ParentId==null)break;}}
        }
    }
    private static LearningContext CaptureStudyContext(SqliteConnection c,SqliteTransaction tx,string subject,string? node,LearningContext? request,FocusContext? oldFocus,ContextCapture kind,DateTimeOffset at,bool requireActive,LearningContext? preserve=null)
    {
        var profile=request?.ProfileId??(node==null?"legacy-default":(string?)Scalar(c,tx,"SELECT profile_id FROM chapters WHERE id=$id",("$id",node))??"legacy-default");
        ValidateLearningTarget(c,tx,profile,subject,node,requireActive);
        var task=request==null?oldFocus?.TaskId:request.TaskId;var title="";
        if(task!=null&&preserve?.TaskId==task)title=preserve.TaskTitle;
        else if(task!=null)
        {
            var raw=Scalar(c,tx,"SELECT payload FROM tasks WHERE id=$id",("$id",task)) as string??throw new ArgumentException("关联任务不存在。");var t=Parse<LearningTask>(raw);
            if(t.ProfileId!=profile||t.SubjectId!=null&&t.SubjectId!=subject)throw new ArgumentException("任务不属于本次学习的科目或考试档案。");title=t.Title;
        }
        var refs=request?.WeakPoints??[];if(refs.Select(w=>w.Id).Distinct().Count()!=refs.Count)throw new ArgumentException("重复的薄弱点关联。");List<StudyWeakPointRef> weak=[];
        foreach(var reference in refs.OrderBy(r=>r.Id,StringComparer.Ordinal))
        {
            var historical=preserve?.WeakPoints?.FirstOrDefault(w=>w.Id==reference.Id);if(historical!=null){weak.Add(historical);continue;}
            var raw=Scalar(c,tx,"SELECT payload FROM weakpoints WHERE id=$id",("$id",reference.Id)) as string??throw new ArgumentException("关联薄弱点不存在。");var point=Parse<WeakPoint>(raw);
            if(point.ProfileId!=profile||point.SubjectId!=null&&point.SubjectId!=subject)throw new ArgumentException("薄弱点不属于本次学习的科目或考试档案。");weak.Add(new(point.Id,point.Title));
        }
        var path=node==null?null:string.Join(" / ",KnowledgeTree.Path(ReadKnowledgeNodes(c,tx),node).Select(n=>n.Name));
        return new(profile,task,title,path,at,kind,weak);
    }
    private static StudyRecord PrepareStudyContext(SqliteConnection c,SqliteTransaction tx,StudyRecord input,StudyRecord? before,ContextCapture kind,DateTimeOffset? at=null)
    {
        var sameLocation=before!=null&&before.SubjectId==input.SubjectId&&before.ChapterId==input.ChapterId;
        var requested=input.Context??(sameLocation?before?.Context:null);
        if(sameLocation&&before?.Context!=null&&Json(requested)==Json(before.Context))return input with{Context=before.Context};
        var preserve=before?.SubjectId==input.SubjectId&&requested?.ProfileId==before.Context?.ProfileId?before.Context:null;
        var context=CaptureStudyContext(c,tx,input.SubjectId,input.ChapterId,requested,input.Focus,before==null?kind:ContextCapture.Corrected,at??DateTimeOffset.UtcNow,before==null,preserve);
        if(sameLocation&&before?.Context is {} old&&old.ProfileId==context.ProfileId&&old.TaskId==context.TaskId)
            context=context with{NodePath=old.NodePath,TaskTitle=old.TaskTitle,CapturedAt=old.CapturedAt,Capture=old.Capture,
                WeakPoints=context.WeakPoints!.Select(w=>old.WeakPoints?.FirstOrDefault(prior=>prior.Id==w.Id)??w).ToArray()};
        return input with{Context=context};
    }
    private static void CheckLearningData(SqliteConnection c)
    {
        if(!MigrationDigestMatches(Migration4Sql,Scalar(c,null,"SELECT script_digest FROM schema_migrations WHERE target_version=4")))throw new InvalidDataException("Schema4迁移登记无效。");
        var records=ReadRecords(c);var contexts=ReadStudyContexts(c);
        if(records.Count!=contexts.Count||records.Any(r=>r.Context==null))throw new InvalidDataException("学习上下文缺失。");
        foreach(var record in records)
        {
            var context=record.Context!;ValidateLearningTarget(c,null,context.ProfileId,record.SubjectId,record.ChapterId);
            if(!Enum.IsDefined(context.Capture)||context.TaskId==null&&context.TaskTitle!=""||context.Capture!=ContextCapture.Legacy&&context.CapturedAt==null||record.ChapterId==null&&context.NodePath!=null||context.Capture!=ContextCapture.Legacy&&record.ChapterId!=null&&string.IsNullOrWhiteSpace(context.NodePath))throw new InvalidDataException("学习上下文证据无效。");
        }
        using(var cmd=Command(c,null,"SELECT payload FROM tasks"))using(var r=cmd.ExecuteReader())while(r.Read()){var task=Parse<LearningTask>(r.GetString(0));ValidateLearningTarget(c,null,task.ProfileId,task.SubjectId,task.ChapterId);}
        using(var cmd=Command(c,null,"SELECT payload FROM weakpoints"))using(var r=cmd.ExecuteReader())while(r.Read()){var point=Parse<WeakPoint>(r.GetString(0));ValidateLearningTarget(c,null,point.ProfileId,point.SubjectId,point.ChapterId);}
    }
}
