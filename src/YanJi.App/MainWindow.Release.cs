using System;
using System.Linq;
using System.Windows;
using YanJi.Data;
namespace YanJi.App;
public partial class MainWindow
{
    private async void ConfigureExamProfile_Click(object sender,RoutedEventArgs e)=>await ConfigureExamProfileAsync();
    internal async System.Threading.Tasks.Task ConfigureExamProfileAsync()
    {
        var profile=Model.Database.ExamProfiles().Single(p=>p.Id=="legacy-default");var subjects=Model.Database.Subjects(true);
        var first=Model.Database.Setting("onboarding-complete","")!="true"&&Model.Database.KnowledgeNodes().Count==0&&Model.Database.Records(null,true).Count==0;
        var fields=new[]{new Field("name","考试档案名称 *",profile.Name),new Field("year","入学年份（本版本）","2028",ReadOnly:true),new Field("date","考试日期（可空，yyyy-MM-dd）",profile.ExamDate?.ToString("yyyy-MM-dd")??""),new Field("presets","数学、英语、408参考目录",first?"1":"0",[new("1","应用至空白科目（先显示共同基础）"),new("0","暂不应用 / 保留已有目录")])}
            .Concat(subjects.Select(s=>new Field("subject:"+s.Id,s.Name,(!s.Archived&&(!first||s.Id is "math" or "english" or "408"))?"1":"0",[new("1","使用中"),new("0","暂不使用（历史仍保留）")]))).ToArray();
        var v=Edit("开始使用研迹 · 考试档案",fields,"2028表示入学年份；日期可以暂空。选择日常科目，不会删除历史记录、知识节点或知识包。",values=>{Date(values["date"]);return Required(values,"name")??(!subjects.Any(s=>values["subject:"+s.Id]=="1")?"至少选择一个科目。":null);},commit:v=>Model.RunAsync(()=>Model.Database.ConfigureDefaultProfile(v["name"],Date(v["date"]),subjects.Where(s=>v["subject:"+s.Id]=="1").Select(s=>s.Id).ToArray()),"考试档案与使用科目已保存"));
        if(v!=null&&v["presets"]=="1")await ApplySimplePresetsAsync(false);
    }
    private async void UpgradeCopy_Click(object sender,RoutedEventArgs e)
    {
        var v=Edit("旧版升级 · 生成独立新目录",[new("source","旧版数据目录（含yanji.sqlite）*"),new("target","全新目标目录 *")],"此升级流程目前仅完成隔离数据验证，请勿用于真实学习数据。请先关闭旧版及托盘程序。原目录保持不变；系统会先备份并校验，再生成新版数据副本。",values=>Required(values,"source","target"));
        if(v==null||!Confirm("确认生成升级副本","旧目录："+v["source"]+"\n新目录："+v["target"]+"\n\n不覆盖旧源或已有目标。升级后新学习不会自动合并回旧版。","生成新副本"))return;
        UpgradeCopyResult? result=null;if(await Model.RunAsync(()=>result=YanJiDatabase.PrepareUpgradeCopy(v["source"],v["target"]),"升级副本已生成并通过数据核对，当前窗口仍使用原目录",false))ShowText("升级副本已准备",$"原数据格式：{result!.SourceSchema}，已生成新版格式副本。\n新目录：{result.TargetDirectory}\n升级前完整备份：{result.BackupPath}\n有效学习时长：{MainViewModel.Duration(result.OriginalDurationMs)}\n升级前后学习数据核对一致。当前窗口仍使用原目录。本流程仅供隔离数据验证，请勿用于真实学习数据。");
    }
    private async void OriginalRollback_Click(object sender,RoutedEventArgs e)
    {
        var backup=BackupFile();if(backup==null)return;var v=Edit("恢复旧格式到新目录",[new("target","全新回退目录 *")],"只解包校验后的原格式备份，不升级、不覆盖、不合并。保留升级后新学习的完整备份，再决定是否使用旧程序。",values=>Required(values,"target"));
        if(v==null||!Confirm("确认恢复原格式",backup+"\n目标："+v["target"]+"\n\n旧程序只能打开兼容的旧格式，不能直接使用新版数据。","恢复至新目录"))return;
        await Model.RunAsync(()=>YanJiDatabase.RestoreOriginalToNewDirectory(backup,v["target"]),"原格式已恢复到新目录，当前数据未被替换",false);
    }
}
