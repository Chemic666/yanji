using YanJi.Core;
using YanJi.Data;
namespace YanJi.Tests;
public sealed class PeriodReviewTests : IDisposable
{
    private readonly string root=Path.Combine(AppContext.BaseDirectory,"scratch","review-"+Guid.NewGuid().ToString("N"));
    private readonly YanJiDatabase db;
    private static readonly DateOnly Day=new(2026,10,14);
    public PeriodReviewTests(){db=new(Path.Combine(root,"data"));}
    private static PeriodReview Draft()=>new("review",Day.AddDays(-6),Day,"math","推导边界；下阶段练习反例。");
    [Fact] public void PersistenceVersionsDeleteRestoreAndBackupRoundTrip()
    {
        var saved=db.SavePeriodReview(Draft());Assert.Equal(1,saved.Version);
        saved=db.SavePeriodReview(saved with{Body="已补充反例"});Assert.Equal(2,saved.Version);
        Assert.Throws<InvalidOperationException>(()=>db.SavePeriodReview(saved with{Version=1,Body="过期"}));
        Assert.Equal(2,db.History(saved.Id).Count);Assert.Equal("period-review",db.History(saved.Id)[0].EntityKind);
        db.SetPeriodReviewDeleted(saved.Id,saved.Version,true);Assert.Empty(db.PeriodReviews());
        var deleted=Assert.Single(db.PeriodReviews(true));Assert.Equal(3,deleted.Version);
        db.SetPeriodReviewDeleted(deleted.Id,deleted.Version,false);
        var backup=Path.Combine(root,"review.yanjibak");db.Backup(backup);db.ValidateBackup(backup);
        using var restored=new YanJiDatabase(Path.Combine(root,"restored"));restored.RestoreBackup(backup);
        var actual=Assert.Single(restored.PeriodReviews());Assert.Equal("已补充反例",actual.Body);Assert.Equal(4,actual.Version);
        Assert.Equal(4,restored.History(actual.Id).Count);Assert.Empty(restored.Records());Assert.Equal("",restored.Reflection(Day));
        using var restarted=new YanJiDatabase(Path.Combine(root,"data"));Assert.Equal(actual,Assert.Single(restarted.PeriodReviews()));
    }
    [Fact] public void InvalidReviewNeverWritesEntityOrRevision()
    {
        foreach(var bad in new[]{Draft() with{Body=" "},Draft() with{To=Day.AddDays(-9)},Draft() with{SubjectId="missing"},Draft() with{Format=9},Draft() with{Id="bad:id"}})
            Assert.Throws<ArgumentException>(()=>db.SavePeriodReview(bad));
        Assert.Empty(db.PeriodReviews());Assert.Empty(db.History("review"));
    }
    [Fact] public void CorruptReviewNamespaceFailsBackupValidation()
    {
        db.SaveSetting("period-review:v1:corrupt","{bad");
        Assert.Throws<System.Text.Json.JsonException>(()=>db.PeriodReviews());
        Assert.Throws<InvalidDataException>(()=>db.Backup(Path.Combine(root,"invalid.yanjibak")));
    }
    public void Dispose(){db.Dispose();if(Directory.Exists(root))Directory.Delete(root,true);}
}
