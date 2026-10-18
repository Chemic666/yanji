using YanJi.App;
namespace YanJi.Tests;

public sealed class ProductCopyTests
{
    [Theory]
    [InlineData("0.4.0-rc.quiet.2+ui-copy.1","v0.4.0 · 预发布")]
    [InlineData("0.4.0-beta.1+abcdef","v0.4.0 · 预发布")]
    [InlineData("0.4.0","v0.4.0")]
    [InlineData("0.4.0.0","v0.4.0")]
    [InlineData("","版本信息不可用")]
    public void VersionLabelsDoNotInventStableOrExposeBuildIds(string full,string expected)
    {
        Assert.Equal(expected,ProductCopy.ShortVersion(full));
        Assert.Equal(expected,ProductCopy.Footer(false,full));
        Assert.Equal("隔离测试",ProductCopy.Footer(true,full));
    }
    [Fact]
    public void RestoreWarningStillExplainsReplacementNoMergeAndSafetyBackup()
    {
        Assert.Contains("替换当前全部数据",ProductCopy.RestoreWarning);
        Assert.Contains("不会合并",ProductCopy.RestoreWarning);
        Assert.Contains("完整备份",ProductCopy.RestoreWarning);
    }
}
