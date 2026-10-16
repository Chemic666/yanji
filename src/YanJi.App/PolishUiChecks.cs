using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YanJi.Core;
namespace YanJi.App;
internal static class PolishUiChecks
{
    private static IEnumerable<DependencyObject> Children(DependencyObject root)
    {for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var c=VisualTreeHelper.GetChild(root,i);yield return c;foreach(var d in Children(c))yield return d;}}
    internal static async Task<bool> RunAsync(MainWindow owner,string output)
    {
        Directory.CreateDirectory(output);
        List<object> checks=[],shots=[];bool passed=true;var d=owner.Model.Database;
        void Write(string file,object value)=>File.WriteAllText(Path.Combine(output,file),JsonSerializer.Serialize(value,new JsonSerializerOptions{WriteIndented=true}));
        void Check(bool ok,string name){passed&=ok;checks.Add(new{Name=name,Status=ok?"PASS":"FAIL"});Write("shortcut-checks.json",checks);}
        async Task Wait(Func<bool> condition){var clock=Stopwatch.StartNew();while(!condition()&&clock.ElapsedMilliseconds<10000)await Task.Delay(20);if(!condition())throw new TimeoutException("Polish test wait");}
        void Capture(Window w,string name)
        {w.UpdateLayout();var file=Path.Combine(output,name+".png");NativeCapture.Save(w,file);shots.Add(new{File=Path.GetFileName(file),Bytes=new FileInfo(file).Length,Sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))),Dpi=VisualTreeHelper.GetDpi(w).PixelsPerInchX});Write("shortcut-screenshots.json",shots);}
        try
        {
            var raw=d.Records().Sum(r=>r.DurationMs);d.SaveKnowledgeNodes([new("polish-save-node","math","快捷保存隔离节点")]);var node=d.KnowledgeNodes().Single(n=>n.Id=="polish-save-node");
            var gate=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);bool accept=false;int calls=0;string? expectedFailure=null;
            var editor=new EditorDialog(owner,"快捷保存 · 隔离验证",[new("name","章节名称 *"),new("content","学习内容","第一行\n第二行",Multiline:true)],validate:v=>string.IsNullOrWhiteSpace(v["name"])?"请填写章节名称。":null);
            editor.CommitAsync=async v=>
            {
                calls++;await gate.Task;
                if(!accept){try{d.SaveProgressNote(node.Id,node.Version-1,LearningState.Learning,v["content"],"");}catch(Exception ex){expectedFailure=ex.Message;Write("expected-save-failure.json",new{Type=ex.GetType().Name,Message=ex.Message,Kind="real SQLite stale-version rejection, intentionally injected"});return false;}throw new InvalidOperationException("Stale write unexpectedly accepted");}
                await Task.Run(()=>d.SaveProgressNote(node.Id,node.Version,LearningState.Learning,v["content"],v["name"]));return true;
            };
            async Task Exercise()
            {
                await Task.Delay(120);var inputs=Children(editor).OfType<TextBox>().ToArray();var single=inputs.First(t=>!t.AcceptsReturn);var multi=inputs.Single(t=>t.AcceptsReturn);var save=Children(editor).OfType<Button>().Single(b=>b.Content?.ToString()=="保存");
                single.Focus();Check(editor.Shortcuts!.TrySave(Key.Return,ModifierKeys.None,single),"single-line Enter routes through shared save command");await Task.Delay(60);Check(editor.IsVisible&&calls==0&&single.Text=="","invalid input remains, no persistence");single.Text="极限边界";
                var composition=new TextComposition(InputManager.Current,single,"中");single.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice,composition){RoutedEvent=TextCompositionManager.PreviewTextInputStartEvent});
                Check(!editor.Shortcuts.TrySave(Key.Return,ModifierKeys.None,single)&&!editor.Shortcuts.TrySave(Key.Return,ModifierKeys.Control,multi)&&calls==0,"composition blocks Enter and Ctrl+Enter without submitting");
                single.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice,composition){RoutedEvent=TextCompositionManager.PreviewTextInputEvent});Check(!editor.Shortcuts.TrySave(Key.Return,ModifierKeys.None,single),"completion-event Enter does not leak into default save");await Task.Delay(60);
                Check(!editor.Shortcuts.TrySave(Key.ImeProcessed,ModifierKeys.None,single),"IME-processed keys remain with WPF input system");
                Check(!editor.Shortcuts.TrySave(Key.Return,ModifierKeys.None,multi)&&multi.AcceptsReturn&&calls==0,"plain Enter in multiline stays newline input");
                Check(editor.Shortcuts.TrySave(Key.Return,ModifierKeys.None,single),"single-line save begins once");Check(!editor.Shortcuts.TrySave(Key.Return,ModifierKeys.Control,multi),"inflight shortcut cannot duplicate submission");await Wait(()=>calls==1&&save.Content?.ToString()=="保存中…");await Task.Delay(100);Check(!save.IsEnabled&&!single.IsEnabled,"saving disables submit and editing with visible feedback");Capture(editor,"save-pending-light");gate.TrySetResult(true);
                await Wait(()=>expectedFailure!=null&&save.IsEnabled);Check(editor.IsVisible&&single.Text=="极限边界"&&multi.Text=="第一行\n第二行","real failed persistence leaves both input fields open and intact");
                accept=true;multi.Focus();Check(editor.Shortcuts.TrySave(Key.Return,ModifierKeys.Control,multi),"Ctrl+Enter commits multiline content");await Wait(()=>save.Content?.ToString()=="已保存");await Task.Delay(60);Capture(editor,"save-success-light");
            }
            var exercise=Exercise();_=exercise.ContinueWith(_=>editor.Dispatcher.BeginInvoke(new Action(()=>{if(editor.IsVisible)editor.Close();})),TaskContinuationOptions.OnlyOnFaulted);var confirmed=editor.ShowDialog();await exercise;
            Check(confirmed==true&&calls==2&&d.ProgressNotes().Single(n=>n.NodeId==node.Id).Content=="第一行\n第二行","one failed attempt and one successful commit preserve newlines");Check(d.Records().Sum(r=>r.DurationMs)==raw,"progress shortcut does not invent study time");
            await owner.OpenKnowledgeAsync(node.Id);await Wait(()=>owner.KnowledgeView.Model?.Selected?.Node.Id==node.Id&&!owner.KnowledgeView.Model.KnowledgeBusy);
            var name=owner.KnowledgeView.NameBox;name.SetCurrentValue(TextBox.TextProperty,"快捷保存 · 节点已更新");name.Focus();
            var key=new KeyEventArgs(Keyboard.PrimaryDevice,PresentationSource.FromVisual(owner),Environment.TickCount,Key.Return){RoutedEvent=Keyboard.PreviewKeyDownEvent};name.RaiseEvent(key);
            await Wait(()=>d.KnowledgeNodes().Single(n=>n.Id==node.Id).Name=="快捷保存 · 节点已更新");Check(key.Handled,"actual routed Enter saves inline knowledge name");Check(d.KnowledgeNodes().Single(n=>n.Id==node.Id).Id==node.Id,"inline name update preserves stable node identity");
            var searchEditor=new EditorDialog(owner,"搜索不提交",[new("node","章节",node.Id,[new(node.Id,"快捷保存节点")])]);
            async Task SearchCheck(){await Task.Delay(100);var search=Children(searchEditor).OfType<TextBox>().Single();search.Focus();Check(!searchEditor.Shortcuts!.TrySave(Key.Return,ModifierKeys.None,search),"chapter search Enter does not submit the editor");searchEditor.Close();}
            var searching=SearchCheck();searchEditor.ShowDialog();await searching;
            // Exercise the actual quick-add dialog and existing persistence path.
            owner.Model.Page=0;owner.TestDialogShown=w=>{async Task Fill(){await Task.Delay(120);var e=(EditorDialog)w;e.SetField("subject","english");e.SetField("minutes","0.5");e.SetField("note","快捷补录隔离验收");var input=Children(e).OfType<TextBox>().Single(t=>t.AcceptsReturn);input.Focus();e.Shortcuts!.TrySave(Key.Return,ModifierKeys.Control,input);} _=Fill();};
            Children(owner).OfType<Button>().Single(b=>b.IsVisible&&b.Content?.ToString()=="＋ 快速补录").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));await Wait(()=>d.Records().Any(r=>r.Note=="快捷补录隔离验收"));owner.TestDialogShown=null;
            var manual=d.Records().Single(r=>r.Note=="快捷补录隔离验收");Check(manual.DurationMs==30000&&manual.Parts.All(p=>p.Kind==PartKind.DurationOnly&&p.StartUtcMs==null),"actual quick-add Ctrl+Enter saves one duration-only record");await owner.Model.RunAsync(()=>d.DeleteRecord(manual.Id,manual.Version),"清理本次合成记录的有效贡献");Check(d.Records().Sum(r=>r.DurationMs)==raw,"test-only soft delete restores initial live total");
            foreach(var dark in new[]{false,true})
            {
                owner.Model.DarkTheme=dark;await Task.Delay(120);var panel=new StackPanel{Margin=new Thickness(24)};panel.Children.Add(new TextBlock{Text="Quiet Focus · 按钮状态",FontSize=22,Margin=new Thickness(0,0,0,20)});
                var row=new WrapPanel();Button? focus=null;
                foreach(var (label,style) in new[]{("主要操作","Primary"),("次要操作","SecondaryButton"),("更多操作","GhostButton"),("删除…","DangerButton"),("↗","IconButton")}){var b=new Button{Content=label,Style=(Style)owner.FindResource(style)};row.Children.Add(b);focus??=b;}
                panel.Children.Add(row);var states=new WrapPanel{Margin=new Thickness(0,20,0,0)};
                foreach(var state in new[]{"Disabled","Saving","Success"}){var b=new Button{Content=state=="Disabled"?"不可用":state,Style=(Style)owner.FindResource("Primary"),IsEnabled=false};if(state!="Disabled")ButtonFeedback.SetState(b,state);states.Children.Add(b);}panel.Children.Add(states);
                panel.Children.Add(new TextBlock{Text="实机焦点 / 禁用 / 保存中 / 已保存；悬停与按压采用140ms透明度反馈。",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,20,0,0)});
                var window=new Window{Owner=owner,Title="按钮状态 · 隔离验证",Width=800,Height=330,Content=panel,WindowStartupLocation=WindowStartupLocation.CenterOwner};window.Show();focus!.Focus();await Task.Delay(120);Capture(window,dark?"buttons-dark":"buttons-light");Check(focus.IsKeyboardFocused,"actual keyboard focus ring "+dark);window.Close();
            }
            owner.Model.DarkTheme=false;await Wait(()=>!owner.Model.Busy);Write("shortcut-result.json",new{Status=passed?"PASS":"FAIL",Checks=checks.Count,Images=shots.Count,PhysicalIME="NOT TESTED",Method="real WPF controls and routed commands with synthetic composition/key arguments; not physical IME/keystrokes"});return passed;
        }
        catch(Exception ex){Write("shortcut-failure.json",new{Error=ex.ToString()});return false;}
        finally{owner.TestDialogShown=null;}
    }
}
