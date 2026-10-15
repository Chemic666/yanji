using YanJi.Core;
using YanJi.Data;
namespace YanJi.Tests;
public sealed class ReleaseProfileTests
{
    [Fact] public void FirstProfileSubjectChoicePreservesHistoricalFactsAndBackup()
    {
        var root=M1TestDirectories.NewDirectory();using var d=new YanJiDatabase(root);d.SaveRecord(new("old","politics",null,SessionSource.Manual,"",[TimeParts.Duration(new(2026,10,15),TimeSpan.FromMilliseconds(12345),"UTC")]),"synthetic");
        d.ConfigureDefaultProfile("2028 考研 · 我的档案",null,["408","math","english"]);Assert.DoesNotContain(d.Subjects(),s=>s.Id=="politics");Assert.Equal(12345,d.Records().Single().DurationMs);Assert.Equal(2028,d.ExamProfiles().Single().AdmissionYear);
        Assert.Throws<ArgumentException>(()=>d.ConfigureDefaultProfile("无效",null,[]));Assert.Equal("2028 考研 · 我的档案",d.ExamProfiles().Single().Name);
        var backup=Path.Combine(root,"profile.yanjibak");d.Backup(backup);d.ValidateBackup(backup);d.RestoreBackup(backup);Assert.Equal(12345,d.Records().Single().DurationMs);
    }
}
