using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using YanJi.Core;
namespace YanJi.App;
internal static class M3ReviewValidation
{
    internal static async Task RunAsync(MainWindow owner,string output)
    {
        Directory.CreateDirectory(output);List<object> results=[];bool failed=false;
        void Check(bool ok,string name){results.Add(new{Name=name,Status=ok?"PASS":"FAIL"});failed|=!ok;File.WriteAllText(Path.Combine(output,"checks.json"),JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));}
        try
        {
            var m=owner.Model;var d=m.Database;d.SaveKnowledgeNodes([new("review-node","math","审查回归节点")]);m.Refresh();m.Page=2;await Task.Delay(150);var k=owner.KnowledgeView.Model!;k.Subject=k.Subjects.First(s=>s.Id=="math");await k.ReloadAsync("review-node");await Task.Delay(150);
            await m.StartKnowledgeAsync("review-node");await Task.Delay(2200);await m.FinishCommand.ExecuteAsync(null);var record=d.Records().Single();m.Page=2;await Task.Delay(400);
            Check(k.LinkedRecords.Any(r=>r.Record.Id==record.Id)&&k.StudySummary.StartsWith("1 次"),"ordinary navigation refreshes saved study without manual reload");
            Check(m.RecentLearningItems.Any(r=>r.ChapterId=="review-node"),"latest completed study is present in recent shortcuts");
            d.DeleteRecord(record.Id,record.Version);m.Refresh();Check(!m.HasCompletedStudy,"deleting completed study removes stale optional completion prompt");
            m.Page=0;m.Page=2;await Task.Delay(400);Check(k.LinkedRecords.Count==0&&k.StudySummary.StartsWith("0 次"),"ordinary navigation refreshes deletion");
            File.WriteAllText(Path.Combine(output,"result.json"),JsonSerializer.Serialize(new{Status=failed?"FAIL":"PASS",Checks=results.Count}));((App)Application.Current).ValidationFailed=failed;
        }
        catch(Exception e){File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());((App)Application.Current).ValidationFailed=true;}
        finally{owner.SmokeClose();}
    }
}
