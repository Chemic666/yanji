using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YanJi.Core;

namespace YanJi.App;
internal static class M1WindowValidation
{
    internal static async Task RunAsync(MainWindow w, string output)
    {
        Directory.CreateDirectory(output); var app = (App)Application.Current; var checks = new List<object>(); var images = new List<object>();
        bool failed = false;
        void Write(string file, object data) => File.WriteAllText(Path.Combine(output, file), JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        void Check(bool ok, string name) { checks.Add(new { Name = name, Status = ok ? "PASS" : "FAIL" }); failed |= !ok; Write("checks.json", checks); }
        async Task Shot(string name)
        {
            w.UpdateLayout(); await Task.Delay(180); var file = name + ".png"; var path = Path.Combine(output, file); NativeCapture.Save(w, path);
            images.Add(new { File = file, Bytes = new FileInfo(path).Length, Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))), Width = w.ActualWidth, Height = w.ActualHeight, Dark = ThemeService.Dark, Dpi = VisualTreeHelper.GetDpi(w).PixelsPerInchX }); Write("screenshots.json", images);
        }
        try
        {
            Write("provenance.json", new { Synthetic = true, Source = typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
                Exe = Environment.ProcessPath, AssemblySha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(App).Assembly.Location))),
                OS = System.Runtime.InteropServices.RuntimeInformation.OSDescription, Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription });
            w.Model.Page = 2; await Task.Delay(200); var vm = w.KnowledgeView.Model ?? throw new InvalidOperationException("Knowledge view not loaded");
            await vm.ReloadAsync(); await Shot("knowledge-empty-light-1200");
            w.Model.DarkTheme = true; w.Width = 900; w.Height = 600; await Shot("knowledge-empty-dark-900");
            var nodes = Enumerable.Range(0, 10000).Select(i => new KnowledgeNode("fixture-" + i, "math", i % 100 == 0 ? "数学主干 " + i / 100 : "核心知识点 " + i,
                ParentId: i % 100 == 0 ? null : i == 2 ? "fixture-1" : i == 3 ? "fixture-2" : "fixture-" + i / 100 * 100, Kind: i % 100 == 0 ? KnowledgeNodeKind.Chapter : KnowledgeNodeKind.KnowledgePoint,
                SortOrder: i, Description: "中文学习重点：分清学习进度、自评掌握、任务完成和薄弱点验收。这是一份合成目录，不是官方考试大纲。"));
            var clock = Stopwatch.StartNew(); await Task.Run(() => w.Model.Database.SaveKnowledgeNodes(nodes.ToArray())); var batchMs = clock.Elapsed.TotalMilliseconds;
            w.Model.Refresh(); clock.Restart(); await vm.ReloadAsync(); var loadMs = clock.Elapsed.TotalMilliseconds;
            Check(vm.Roots.Count == 100, "10k nodes lazily expose only 100 roots");
            clock.Restart(); vm.Roots[0].Expanded = true; w.UpdateLayout(); var expandMs = clock.Elapsed.TotalMilliseconds;
            Check(vm.Roots[0].Children.Count == 97, "lazy expansion exposes actual children without eagerly loading deeper descendants");
            vm.Locate("fixture-1"); await Task.Delay(150);
            vm.NodeName = "中文长标题：函数极限与连续性，整理自己的定义、方法及学习重点";
            vm.StateIndex = 1; vm.ProgressText = "35"; vm.MasteryIndex = 1; await vm.SaveCommand.ExecuteAsync(null);
            Check(vm.Selected?.Node.Id == "fixture-1" && vm.Selected.Node.Progress == 35 && vm.Selected.Node.Mastery == Mastery.Weak, "WPF edit binding persists Chinese name and independent states");
            var date=w.Model.LocalToday();w.Model.Database.SaveRecord(new("fixture-learning","math","fixture-1",SessionSource.Manual,"合成关联记录",[new("fixture-part",date,1500,"Asia/Shanghai",PartKind.DurationOnly)]),"窗口验证");
            vm.Parent=vm.Parents.Single(p=>p.Id=="fixture-100");await vm.SaveCommand.ExecuteAsync(null);
            Check(vm.Selected?.FullPath.StartsWith("数学主干 1 / ")==true,"move preserves identity and updates full path");
            await vm.ArchiveCommand.ExecuteAsync(null);Check(vm.Selected?.Node.Archived==true,"archive preserves selected node");
            await vm.ArchiveCommand.ExecuteAsync(null);Check(vm.Selected?.Node.Archived==false,"re-enable preserves identity");
            Check(w.Model.Database.Records().Single(r=>r.Id=="fixture-learning").ChapterId=="fixture-1"&&w.Model.Database.KnowledgeDuration(["fixture-100","fixture-1"],date,date)==1500,"move/archive retain historical link and no duplicate time");
            var restorePath=Path.Combine(output,"node-view-restore.yanjibak");w.Model.Database.Backup(restorePath);
            vm.NodeName="恢复前陈旧名称";await vm.SaveCommand.ExecuteAsync(null);await w.RestoreDataAsync(restorePath);
            Check(vm.Selected==null&&vm.Roots.Any(r=>r.Node.Id=="fixture-100"),"full restore invalidates cached selection and reloads knowledge roots");
            vm.Locate("fixture-1");Check(vm.NodeName!="恢复前陈旧名称","restored node edit buffer comes from backup");
            await Task.Delay(120);w.UpdateLayout();
            var focusMoved = w.KnowledgeView.NameBox.Focus();
            Check(focusMoved, "node editor can receive keyboard focus");
            Check(w.KnowledgeView.NameBox.MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next)), "Tab traversal moves from name editor");
            w.Model.DarkTheme = false; w.Width = 1200; w.Height = 800; await Shot("knowledge-light-1200");
            vm.Locate("fixture-3");Check(vm.Selected?.FullPath.Split(" / ").Length==4,"four-level path and lazy ancestor expansion work");await Shot("knowledge-deep-tree-1200");vm.Locate("fixture-1");
            w.Model.DarkTheme = true; await Shot("knowledge-dark-1200");
            w.Width = 900; w.Height = 600; await Shot("knowledge-dark-900");
            w.Model.DarkTheme = false; await Shot("knowledge-light-900");
            List<double> searchMs=[];
            foreach(var query in new[]{"核心知识点 99","数学主干","不存在的节点","核心知识点 19","核心知识点 99"})
            {vm.Search=query;await Task.Delay(450);searchMs.Add(vm.LastSearchMilliseconds);}
            Check(vm.SearchResults.Count > 0 && vm.SearchResults.All(r => r.FullPath.Contains("核心知识点 99")), "background search returns exact Chinese path matches");
            w.KnowledgeView.ResultsList.SelectedIndex=0;w.UpdateLayout();
            Check(w.KnowledgeView.DetailPanel.Visibility==Visibility.Visible&&vm.Selected!=null,"narrow search selection opens node detail");
            await Shot("knowledge-search-900"); vm.Search = "";
            Write("performance.json", new { Nodes = 10000, BatchMs = batchMs, ColdViewLoadMs = loadMs, ExpandWithLayoutMs = expandMs,
                SearchSamplesMs=searchMs, SearchP95Ms=searchMs.Order().Last(), RealWpf = true, Dpi = VisualTreeHelper.GetDpi(w).PixelsPerInchX, Notes = "UI commands and focus traversal scripted; physical keyboard/IME not validated" });
            Check(loadMs <= 2000, "10k directory cold view load within 2 seconds");
            Check(searchMs.Max()<=200,"10k search measured samples within 200ms excluding debounce");
            Write("result.json", new { Status = failed ? "FAIL" : "PASS", Checks = checks.Count, NotTested = new[] { "physical Chinese IME", "other actual DPI scales", "real power changes", "real user upgrade" } });
            if (failed) { app.ValidationFailed = true; w.SmokeClose(); return; }
            Directory.CreateDirectory(Path.Combine(output, "timer"));
            bool stress = true; int iterations = 0;
            async Task Stress()
            {
                while (stress)
                {
                    await vm.ReloadAsync(); vm.Search = iterations++ % 2 == 0 ? "核心知识点 9" : "数学主干";
                    Write("timer-stress.json", new { Iterations = iterations, Nodes = 10000, NativeDispatcher = true, At = DateTimeOffset.UtcNow });
                    await Task.Delay(300);
                }
            }
            var work = Stress();
            try { await TimerDisplayValidation.RunAsync(w, Path.Combine(output, "timer"), false); }
            finally { stress = false; await work; Write("timer-stress.json", new { Iterations = iterations, Nodes = 10000, NativeDispatcher = true }); }
        }
        catch (Exception e) { Write("failure.json", new { Error = e.ToString() }); app.ValidationFailed = true; w.SmokeClose(); }
    }
}
