using Microsoft.Data.Sqlite;
using YanJi.Core;
using YanJi.Data;

namespace YanJi.Tests;

/// <summary>Independent fault test: the entity write succeeds, then SQLite rejects its audit.</summary>
public sealed class PeriodReviewAtomicityTests : IDisposable
{
    private readonly string root = Path.Combine(AppContext.BaseDirectory, "scratch", "review-atomic-" + Guid.NewGuid().ToString("N"));
    private readonly YanJiDatabase db;
    public PeriodReviewAtomicityTests() => db = new(Path.Combine(root, "data"));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RevisionWriteFailureRollsBackCreationOrEdit(bool editing)
    {
        var day = new DateOnly(2026, 10, 14);
        var draft = new PeriodReview("atomic-review", day.AddDays(-6), day, "math", "原始复盘");
        PeriodReview? before = editing ? db.SavePeriodReview(draft) : null;
        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder
               { DataSource = db.DatabasePath, Pooling = false }.ToString()))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TRIGGER reject_review_audit BEFORE INSERT ON revisions
                WHEN NEW.kind = 'period-review'
                BEGIN SELECT RAISE(ABORT, 'isolated audit-write fault'); END;
                """;
            command.ExecuteNonQuery();
        }
        Assert.Throws<SqliteException>(() => db.SavePeriodReview((before ?? draft) with { Body = "不应留下的修改" }));
        if (before is null)
        {
            Assert.Empty(db.PeriodReviews(true));
            Assert.Empty(db.History(draft.Id));
        }
        else
        {
            Assert.Equal(before, Assert.Single(db.PeriodReviews(true)));
            Assert.Single(db.History(draft.Id));
        }
        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder
               { DataSource = db.DatabasePath, Pooling = false }.ToString()))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "DROP TRIGGER reject_review_audit";
            command.ExecuteNonQuery();
        }
        var retry = db.SavePeriodReview((before ?? draft) with { Body = "恢复正常后保存" });
        Assert.Equal((before?.Version ?? 0) + 1, retry.Version);
        Assert.Equal("ok", db.Integrity());
    }

    [Fact]
    public void EditingAfterAClockOffsetDoesNotWriteAnUnreadableReview()
    {
        var day = new DateOnly(2026, 10, 14);
        var saved = db.SavePeriodReview(new("clock-review", day.AddDays(-6), day, "math", "恢复的复盘"));
        // Simulate a backup authored by an ahead-of-time machine, without changing OS time.
        var ahead = DateTimeOffset.UtcNow.AddDays(1);
        var restored = saved with { CreatedAt = ahead, UpdatedAt = ahead };
        db.SaveSetting("period-review:v1:" + saved.Id, System.Text.Json.JsonSerializer.Serialize(restored));
        var loaded = Assert.Single(db.PeriodReviews());
        db.SavePeriodReview(loaded with { Body = "时钟偏移后仍可编辑" });
        var edited = Assert.Single(db.PeriodReviews());
        Assert.Equal(saved.Version + 1, edited.Version);
        Assert.Equal("时钟偏移后仍可编辑", edited.Body);
        var backup = Path.Combine(root, "clock-offset.yanjibak");
        db.Backup(backup);
        db.ValidateBackup(backup);
    }

    public void Dispose()
    {
        db.Dispose();
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }
}
