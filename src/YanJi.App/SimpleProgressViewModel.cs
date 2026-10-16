using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using YanJi.Core;
namespace YanJi.App;

public sealed record SimpleChapterChoice(string Id,string Label);
public sealed record SimpleSubjectProgress(Subject Subject,string Current,string State,string Recent,string Content,string Next,string? NodeId)
{ public string Name=>Subject.Name; public bool HasNode=>NodeId!=null; }
public sealed partial class SimpleProgressViewModel : ObservableObject
{
    public MainViewModel Owner {get;}
    public ObservableCollection<SimpleSubjectProgress> Rows {get;}=[];
    public SimpleProgressViewModel(MainViewModel owner) { Owner=owner; }
    private int _generation;
    public async Task RefreshAsync()
    {
        var generation=++_generation;
        var rows=await Task.Run(()=>
        {
            var subjects=Owner.Database.Subjects().OrderBy(s=>s.Id switch{"math"=>0,"english"=>1,"408"=>2,_=>3}).ToList();
            var nodes=Owner.Database.KnowledgeNodes();var notes=Owner.Database.ProgressNotes();var records=Owner.Database.Records();
            var map=nodes.ToDictionary(n=>n.Id);var active=new Dictionary<string,bool>();
            bool Active(KnowledgeNode n)=>active.TryGetValue(n.Id,out var a)?a:active[n.Id]=!n.Archived&&(n.ParentId==null||Active(map[n.ParentId]));
            var activeIds=nodes.Where(Active).Select(n=>n.Id).ToHashSet();
            return subjects.Select(s=>
            {
                var visible=Owner.VisibleReferenceIds(s.Id);
                var note=notes.Where(n=>map.TryGetValue(n.NodeId,out var k)&&k.SubjectId==s.Id&&activeIds.Contains(n.NodeId)&&visible.Contains(n.NodeId)).OrderByDescending(n=>n.UpdatedAt).FirstOrDefault();
                var latest=records.Where(r=>r.SubjectId==s.Id).OrderByDescending(r=>r.CreatedAt).FirstOrDefault();
                var id=note?.NodeId??records.Where(r=>r.SubjectId==s.Id&&r.ChapterId!=null&&activeIds.Contains(r.ChapterId)&&visible.Contains(r.ChapterId)).OrderByDescending(r=>r.CreatedAt).Select(r=>r.ChapterId).FirstOrDefault();
                var node=nodes.FirstOrDefault(n=>n.Id==id&&!n.Archived);
                var current=node==null?"选择章节，记下学到哪里":string.Join(" / ",KnowledgeTree.Path(nodes,node.Id).Select(n=>n.Name));
                var recent=latest==null?"还没有学习记录":latest.Parts.Max(p=>p.Date).ToString("M月d日")+" · "+DurationText.Format(latest.DurationMs)+(latest.Note.Length>0?" · "+latest.Note:"");
                return new SimpleSubjectProgress(s,current,node==null?"未选择章节":KnowledgeWorkspaceViewModel.StateLabels[(int)node.State],recent,note?.Content??"",note?.Next??"",node?.Id);
            }).ToArray();
        });
        if(generation!=_generation)return;Rows.Clear();foreach(var row in rows)Rows.Add(row);
    }
    internal IReadOnlyList<SimpleChapterChoice> Choices(string subject,string query="")
    {
        var nodes=Owner.Database.KnowledgeNodes(subject);var visible=Owner.VisibleReferenceIds(subject);var byId=nodes.ToDictionary(n=>n.Id);var paths=new Dictionary<string,string>();
        string Path(KnowledgeNode n)=>paths.TryGetValue(n.Id,out var text)?text:paths[n.Id]=n.ParentId==null?n.Name:Path(byId[n.ParentId])+" / "+n.Name;
        bool Archived(KnowledgeNode n)=>n.Archived||n.ParentId!=null&&Archived(byId[n.ParentId]);
        var notes=Owner.Database.ProgressNotes().OrderByDescending(n=>n.UpdatedAt).Select(n=>n.NodeId);
        var recent=notes.Concat(Owner.Database.Records().Where(r=>r.SubjectId==subject).OrderByDescending(r=>r.CreatedAt).Select(r=>r.ChapterId??"")).Distinct().ToList();
        return nodes.Where(n=>visible.Contains(n.Id)&&!n.Archived&&!Archived(n))
            .Select(n=>new SimpleChapterChoice(n.Id,Path(n)))
            .Where(n=>n.Label.Contains(query,StringComparison.CurrentCultureIgnoreCase)).OrderBy(n=>recent.IndexOf(n.Id)<0?int.MaxValue:recent.IndexOf(n.Id)).ThenBy(n=>n.Label).ToList();
    }
}
