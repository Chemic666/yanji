using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using YanJi.Core;
namespace YanJi.App;
internal static class M2WindowValidation
{
    private static IEnumerable<DependencyObject> Children(DependencyObject root)
    {for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var c=VisualTreeHelper.GetChild(root,i);yield return c;foreach(var child in Children(c))yield return child;}}
    private static string TimerText(Window window)=>Children(window).OfType<TextBlock>().Single(t=>BindingOperations.GetBinding(t,TextBlock.TextProperty)?.Path.Path=="TimerText").Text;
    internal static async Task RunAsync(MainWindow owner,string output)
    {
        Directory.CreateDirectory(output);var app=(App)Application.Current;List<object> checks=[],images=[],samples=[];bool failed=false;KnowledgePackageWindow? window=null;
        void Write(string file,object data)=>File.WriteAllText(Path.Combine(output,file),JsonSerializer.Serialize(data,new JsonSerializerOptions{WriteIndented=true}));
        void Check(bool value,string name){checks.Add(new{Name=name,Status=value?"PASS":"FAIL"});failed|=!value;Write("checks.json",checks);}
        async Task Shot(Window target,string name)
        {
            target.UpdateLayout();await Task.Delay(160);var file=name+".png";var path=Path.Combine(output,file);NativeCapture.Save(target,path);
            images.Add(new{File=file,Bytes=new FileInfo(path).Length,Sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),Width=target.ActualWidth,Height=target.ActualHeight,Dark=ThemeService.Dark,Dpi=VisualTreeHelper.GetDpi(target).PixelsPerInchX});Write("screenshots.json",images);
        }
        try
        {
            Write("provenance.json",new{Synthetic=true,Source=typeof(App).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,Exe=Environment.ProcessPath,
                AssemblySha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(App).Assembly.Location))),OS=System.Runtime.InteropServices.RuntimeInformation.OSDescription,Runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                Input="scripted WPF commands, routed click, bound combo selections; not physical keyboard/IME"});
            owner.Model.Page=2;await Task.Delay(200);window=new(owner);window.Show();await Task.Delay(150);await window.Initialization;var vm=window.Model;var db=owner.Model.Database;
            Check(db.InstalledPackages().Count==0,"isolated fresh package empty state");await Shot(window,"packages-empty-light-1200");
            vm.FilterSubject=vm.Subjects.Single(s=>s.Id=="408");Check(vm.Catalog.Count==2&&vm.Catalog.Any(p=>p.Package.PackageId.EndsWith(".408")),"subject filtering includes matching template and blank");
            vm.SelectedCatalog=vm.Catalog.Single(p=>p.Package.PackageId.EndsWith(".408"));var package=vm.SelectedCatalog.Package;vm.Roots[0].Expanded=true;
            Check(vm.Roots.Count==4&&vm.Roots[0].Children.Count==7,"WPF preview lazily exposes four subjects and correct chapter tree");
            await Shot(window,"packages-browse-light-1200");owner.Model.DarkTheme=true;await Shot(window,"packages-browse-dark-1200");window.Width=900;window.Height=600;await Shot(window,"packages-browse-dark-900");owner.Model.DarkTheme=false;
            bool captureConfirm=true;
            vm.Confirm=message=>
            {
                var dialog=new ConfirmDialog(window,"确认知识目录变更",message,"确认应用",false);
                async Task Accept(){await Task.Delay(180);if(captureConfirm){await Shot(dialog,"packages-confirm-light-900");captureConfirm=false;}Children(dialog).OfType<Button>().Single(b=>b.Content?.ToString()=="确认应用").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));}
                _=Accept();return Task.FromResult(dialog.ShowDialog()==true);
            };
            await vm.PreviewCommand.ExecuteAsync(null);Check(vm.CanApply&&vm.Summary.Contains("新增 145"),"preview is read-only and shows actual addition count");await vm.ApplyCommand.ExecuteAsync(null);
            Check(db.KnowledgeNodes().Count==145&&db.InstalledPackages().Count==1,"confirmed WPF install writes 145 stable nodes");
            await vm.PreviewCommand.ExecuteAsync(null);Check(!vm.CanApply&&vm.Summary.Contains("无需重复"),"WPF identical install reports no-op");
            var root=db.KnowledgeNodes().Single(n=>n.Name=="数据结构");db.SaveKnowledgeNodes([root with{Name="我的数据结构目录",Description="中文个人备注，模板更新不可静默覆盖。"}]);
            var v2=package with{PackageVersion="2.0.0",Nodes=package.Nodes.Select(n=>n.Id==package.Nodes[0].Id?n with{Name="新版数据结构",Description="新模板说明"}:n).Append(new("m2-added","408",package.Nodes[0].Id,KnowledgeNodeKind.KnowledgePoint,"新增参考点","",100)).ToList()};
            var sourceFile=Path.Combine(output,"synthetic-v2.yanji-knowledge.json");File.WriteAllText(sourceFile,KnowledgePackageFile.Encode(v2),new UTF8Encoding(false));await vm.LoadFileAsync(sourceFile);await vm.PreviewCommand.ExecuteAsync(null);
            Check(vm.Conflicts.Count==2,"file import produces name and note three-way conflicts");window.Width=1200;window.Height=800;await Shot(window,"packages-conflict-light-1200");owner.Model.DarkTheme=true;window.Width=900;window.Height=600;await Shot(window,"packages-conflict-dark-900");
            window.UpdateLayout();var row=vm.Conflicts.Single(c=>c.Conflict.Field=="Name");var combo=Children(window).OfType<ComboBox>().Single(c=>ReferenceEquals(c.DataContext,row));combo.SelectedIndex=1;await vm.ResolveCommand.ExecuteAsync(null);await vm.ApplyCommand.ExecuteAsync(null);
            Check(db.KnowledgeNodes().Single(n=>n.Id==root.Id) is {Name:"新版数据结构",Description:"中文个人备注，模板更新不可静默覆盖。"},"actual combo selection accepts template name and keeps personal note");
            var added=db.KnowledgeNodes().Single(n=>n.Name=="新增参考点");db.SetLearningState(added.Id,added.Version,LearningState.Learning);
            db.SaveRecord(new("m2-later","408",added.Id,SessionSource.Manual,"合成后来学习",[new("m2-part",owner.Model.LocalToday(),12345,"Asia/Shanghai",PartKind.DurationOnly)]),"窗口验证");
            vm.SelectedInstall=vm.Installed.Single();await Task.Delay(150);await vm.RollbackCommand.ExecuteAsync(null);await vm.ApplyCommand.ExecuteAsync(null);
            Check(db.KnowledgeNodes().Single(n=>n.Id==root.Id).Name=="我的数据结构目录"&&db.KnowledgeNodes().Single(n=>n.Id==added.Id).State==LearningState.Learning&&db.Records().Single().DurationMs==12345,"WPF safe rollback preserves later learning and user changes");
            vm.TabIndex=1;owner.Model.DarkTheme=false;window.Width=1200;window.Height=800;vm.SelectedInstall=null;vm.SelectedInstall=vm.Installed.Single();await Task.Delay(200);await Shot(window,"packages-history-light-1200");
            var malformed=Path.Combine(output,"malformed.json");File.WriteAllText(malformed,"{\"SchemaVersion\":999}");await vm.LoadFileAsync(malformed);window.Width=900;window.Height=600;await Shot(window,"packages-error-light-900");Check(vm.Feedback.StartsWith("未完成：")&&db.InstalledPackages().Single().Version=="1.0.0","invalid import presents understandable error without database changes");
            Check(window.PreviewTree.Focus()&&window.PreviewTree.MoveFocus(new System.Windows.Input.TraversalRequest(System.Windows.Input.FocusNavigationDirection.Next)),"package controls support scripted focus and Tab navigation");
            var large=package with{PackageId="org.yanji.synthetic.scale",Nodes=Enumerable.Range(0,10000).Select(i=>new PackageNode("scale-"+i,"408",i%100==0?null:"scale-"+(i/100*100),i%100==0?KnowledgeNodeKind.Chapter:KnowledgeNodeKind.KnowledgePoint,"合成节点 "+i,"",i%100==0?i/100:i%100-1)).ToList()};
            var watch=Stopwatch.StartNew();vm.SelectPackage(large);vm.Roots[0].Expanded=true;window.UpdateLayout();var layoutMs=watch.Elapsed.TotalMilliseconds;Check(vm.Roots.Count==100&&vm.Roots[0].Children.Count==99&&layoutMs<2000,"10k native preview uses lazy roots and layout below 2 seconds");
            Write("ui-performance.json",new{Nodes=10000,InitialRoots=vm.Roots.Count,ExpandedChildren=vm.Roots[0].Children.Count,LayoutMs=layoutMs,Dpi=VisualTreeHelper.GetDpi(window).PixelsPerInchX});
            window.Close();window=null;owner.Model.Page=2;owner.Width=900;owner.Height=600;owner.Model.Refresh();owner.KnowledgeView.Model!.Subject=owner.KnowledgeView.Model.Subjects.Single(s=>s.Id=="408");await owner.KnowledgeView.Model.ReloadAsync();await Shot(owner,"knowledge-installed-light-900");
            owner.Model.Page=0;owner.ShowMiniTimer();owner.Model.FocusMinutes="2";
            foreach(var mode in new[]{0,1,2})
            {
                owner.Model.FocusModeIndex=mode;await owner.Model.StartCommand.ExecuteAsync(null);
                for(var second=1;second<=3;second++)
                {
                    await Task.Delay(1100);await Task.Run(()=>db.PreviewPackage(large,"legacy-default",new Dictionary<string,string>{{"408","408"}}));owner.UpdateLayout();owner.MiniTimer!.UpdateLayout();
                    var main=TimerText(owner);var mini=TimerText(owner.MiniTimer);samples.Add(new{Mode=mode,Second=second,Utc=DateTimeOffset.UtcNow,Main=main,Mini=mini,Vm=owner.Model.TimerText,RawMs=owner.Model.Timer.DurationMs,Ticks=owner.ValidationTickCount});Write("timer-samples.json",samples);
                    Check(main==mini&&main==owner.Model.TimerText&&main!="00:00:00",$"mode {mode} second {second}: main/mini render synchronized trusted time during package preview");
                }
                await owner.Model.PauseResumeCommand.ExecuteAsync(null);var frozen=owner.Model.TimerText;var ms=owner.Model.Timer.DurationMs;await Task.Delay(2100);Check(owner.Model.TimerText==frozen&&owner.Model.Timer.DurationMs==ms,"pause excludes actual wait from digits and facts");
                await owner.Model.PauseResumeCommand.ExecuteAsync(null);await Task.Delay(1100);await owner.Model.FinishCommand.ExecuteAsync(null);
                var saved=db.Records().Single(r=>r.Id==owner.Model.Timer.SessionId);Check(saved.DurationMs==owner.Model.Timer.DurationMs,"timer finish preserves exact raw record milliseconds");
            }
            Write("result.json",new{Status=failed?"FAIL":"PASS",Checks=checks.Count,Images=images.Count,TimerSamples=samples.Count,NotTested=new[]{"physical IME and keyboard","other actual DPI scales","real power changes","real user schema upgrade"}});app.ValidationFailed=failed;
        }
        catch(Exception e){Write("failure.json",new{Error=e.ToString()});app.ValidationFailed=true;}
        finally{window?.Close();owner.SmokeClose();}
    }
}
