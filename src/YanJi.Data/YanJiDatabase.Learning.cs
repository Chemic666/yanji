using YanJi.Core;
using Microsoft.Data.Sqlite;
namespace YanJi.Data;
public sealed partial class YanJiDatabase
{
    internal Action<string>? LearningCheckpoint {get;set;}
    public StudyRecord SetStudyLinks(string sessionId,long version,string profileId,string? nodeId,string? taskId,IReadOnlyList<string> weakPointIds)
    {
        lock(_gate)
        {
            var record=Records(null,true).Single(r=>r.Id==sessionId);
            return SaveRecord(record with{Version=version,ChapterId=nodeId,Context=new(profileId,taskId,WeakPoints:weakPointIds.Select(id=>new StudyWeakPointRef(id,"")).ToArray())},"修正学习关联（可追溯）");
        }
    }
    public WeakPoint AddWeakPointToStudy(string sessionId,long version,string title,string detail="")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        lock(_gate)
        {
            using var c=Open();using var tx=c.BeginTransaction();
            if(Scalar(c,tx,"SELECT payload FROM active_timer") is string active&&Parse<TimerSnapshot>(active).SessionId==sessionId)throw new InvalidOperationException("请先结束或核对活动计时，再补充薄弱点。");
            var before=ReadRecords(c,tx).Single(r=>r.Id==sessionId);
            if(before.Version!=version||before.Deleted)throw new InvalidOperationException("记录已变化或已删除，请重新打开。");
            var weak=new WeakPoint(Guid.NewGuid().ToString("N"),title.Trim(),detail,SubjectId:before.SubjectId,ChapterId:before.ChapterId,ProfileId:before.Context!.ProfileId);
            Exec(c,tx,"INSERT INTO weakpoints VALUES($id,$payload)",("$id",weak.Id),("$payload",Json(weak)));Audit(c,tx,"weakpoints",weak.Id,0,"学习后新增薄弱点",null,Json(weak));LearningCheckpoint?.Invoke("after-weak");
            var refs=(before.Context.WeakPoints??[]).Append(new StudyWeakPointRef(weak.Id,weak.Title)).OrderBy(w=>w.Id,StringComparer.Ordinal).ToArray();
            var after=before with{Version=before.Version+1,Context=before.Context with{WeakPoints=refs}};
            WriteRecord(c,tx,after);LearningCheckpoint?.Invoke("after-context");Audit(c,tx,"session",after.Id,after.Version,"关联新增薄弱点",Json(before),Json(after));tx.Commit();return weak;
        }
    }
    public KnowledgeStudySummary KnowledgeStudy(string nodeId,DateOnly from,DateOnly to)
    {
        if(from>to)throw new ArgumentException("日期范围无效。");
        lock(_gate)
        {
            using var c=Open();var nodes=CheckKnowledgeData(c);var node=nodes.Single(n=>n.Id==nodeId);var ids=KnowledgeTree.Descendants(nodes,[nodeId]);
            var records=ReadRecords(c).Where(r=>!r.Deleted&&r.SubjectId==node.SubjectId&&r.Context?.ProfileId==node.ProfileId).ToList();
            var selected=records.Where(r=>r.ChapterId!=null&&ids.Contains(r.ChapterId)&&r.Parts.Any(p=>p.Date>=from&&p.Date<=to)).ToArray();
            var parts=selected.SelectMany(r=>r.Parts).Where(p=>p.Date>=from&&p.Date<=to).DistinctBy(p=>p.Id).ToArray();
            var own=selected.Where(r=>r.ChapterId==nodeId).SelectMany(r=>r.Parts).Where(p=>p.Date>=from&&p.Date<=to).DistinctBy(p=>p.Id).Sum(p=>p.DurationMs);
            var unlinked=records.Where(r=>r.ChapterId==null).SelectMany(r=>r.Parts).Where(p=>p.Date>=from&&p.Date<=to).DistinctBy(p=>p.Id).Sum(p=>p.DurationMs);
            var tasks=Tasks().Where(t=>t.ProfileId==node.ProfileId&&t.ChapterId!=null&&ids.Contains(t.ChapterId)).ToArray();
            var linkedWeakIds=selected.SelectMany(r=>r.Context?.WeakPoints??[]).Select(w=>w.Id).ToHashSet();
            var weak=WeakPoints().Where(w=>w.ProfileId==node.ProfileId&&(w.ChapterId!=null&&ids.Contains(w.ChapterId)||linkedWeakIds.Contains(w.Id))).ToArray();
            return new(own,parts.Sum(p=>p.DurationMs),selected.Select(r=>r.Id).Distinct().Count(),parts.Length==0?null:parts.Max(p=>p.Date),unlinked,selected,tasks,weak);
        }
    }
}
