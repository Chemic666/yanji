using System;
using System.Reflection;

namespace YanJi.App;

/// <summary>Presentation only: derive the short label from the actual assembly, never invent a release channel.</summary>
internal static class ProductCopy
{
    public static string FullVersion => typeof(ProductCopy).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(ProductCopy).Assembly.GetName().Version?.ToString() ?? "";
    public static string ShortVersion(string informationalVersion)
    {
        var release=informationalVersion.Split('+')[0];var number=release.Split('-')[0];
        if(!Version.TryParse(number,out var version))return "版本信息不可用";
        var label="v"+version.ToString(version.Build>=0?3:2);
        return release.Contains('-')?label+" · 预发布":label;
    }
    public static string Footer(bool testMode,string informationalVersion)=>testMode?"隔离测试":ShortVersion(informationalVersion);
    public const string RestoreWarning="恢复备份将替换当前全部数据，不会合并。恢复前会为当前数据创建完整备份。请确认已选择正确的备份文件，是否继续？";
}
