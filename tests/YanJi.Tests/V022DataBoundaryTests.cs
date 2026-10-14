using Microsoft.Data.Sqlite;
using YanJi.Core;
using YanJi.Data;
namespace YanJi.Tests;
public sealed class V022DataBoundaryTests : IDisposable
{
 readonly string root=Path.Combine(AppContext.BaseDirectory,"scratch","v022-"+Guid.NewGuid().ToString("N"));readonly YanJiDatabase db;
 public V022DataBoundaryTests(){db=new(root);}
 [Fact] public void ReviewWriteAndRevisionRollbackTogether()
 {
  using(var c=new SqliteConnection($"Data Source={db.DatabasePath};Pooling=False")){c.Open();using var cmd=c.CreateCommand();cmd.CommandText="CREATE TRIGGER fail_review BEFORE INSERT ON revisions WHEN NEW.kind='period-review' BEGIN SELECT RAISE(ABORT,'expected'); END";cmd.ExecuteNonQuery();}
  var d=new DateOnly(2026,10,14);Assert.Throws<SqliteException>(()=>db.SavePeriodReview(new("rollback",d,d,null,"正文")));
  Assert.Empty(db.PeriodReviews());Assert.Empty(db.History("rollback"));
 }
 [Fact] public void CsvRetainsOneMillisecondWhileDisplayFloorsAndNoFakeInterval()
 {
  var d=new DateOnly(2026,10,14);db.SaveRecord(new("tiny","math",null,SessionSource.Manual,"",[new("tiny-part",d,1,"UTC",PartKind.DurationOnly)]),"sample");
  var path=Path.Combine(root,"raw.csv");db.ExportCsv(path);var csv=File.ReadAllText(path);
  Assert.Contains("精确毫秒",csv);Assert.Contains("显示整数秒",csv);Assert.Contains("\"1\",\"0\"",csv);Assert.Contains("DurationOnly",csv);
  Assert.Equal(1,Assert.Single(db.Records()).DurationMs);
 }
 [Fact] public void InvalidReviewRevisionRejectsFullBackup()
 {
  var d=new DateOnly(2026,10,14);var review=db.SavePeriodReview(new("revision",d,d,null,"原文"));
  using(var c=new SqliteConnection($"Data Source={db.DatabasePath};Pooling=False")){c.Open();using var cmd=c.CreateCommand();cmd.CommandText="UPDATE revisions SET after_json=$json WHERE kind='period-review'";cmd.Parameters.AddWithValue("$json",System.Text.Json.JsonSerializer.Serialize(review with{Format=99}));cmd.ExecuteNonQuery();}
  Assert.Throws<InvalidDataException>(()=>db.Backup(Path.Combine(root,"invalid-review-revision.yanjibak")));
 }
 public void Dispose(){db.Dispose();Directory.Delete(root,true);}
}
