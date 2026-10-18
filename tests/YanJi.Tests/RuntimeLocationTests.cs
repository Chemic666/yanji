using System.Security.Cryptography;
using Microsoft.Data.Sqlite;
using YanJi.App;
using YanJi.Core;
using YanJi.Data;

namespace YanJi.Tests;

public sealed class RuntimeLocationTests
{
    private static readonly string Root = Path.GetFullPath("artifacts/runtime-location-tests");
    private static string Fresh() => Path.Combine(Root, Guid.NewGuid().ToString("N"));
    [Fact]
    public void FormalResolutionIsPureAndIndependentOfProgramLocation()
    {
        var fake = Fresh();
        var a = RuntimeLocation.Resolve(false, null, false, fake);
        Assert.Equal(Path.Combine(fake,"YanJi"), a.DataDirectory);
        Assert.Equal(a, RuntimeLocation.Resolve(false,null,false,fake));
        Assert.Equal("研迹 YanJi", a.WindowTitle);
        Assert.False(Directory.Exists(fake));
    }
    [Theory]
    [InlineData(false,null,true)]
    [InlineData(false,"D:\\sandbox",false)]
    [InlineData(true,null,false)]
    [InlineData(true,"",false)]
    [InlineData(true,"relative",false)]
    [InlineData(true,"D:relative",false)]
    [InlineData(true,"\\\\localhost\\share\\test",false)]
    public void InvalidModesNeverFallBack(bool mode,string? path,bool diagnostics)
        => Assert.Throws<ArgumentException>(()=>RuntimeLocation.Resolve(mode,path,diagnostics,Fresh()));
    [Fact]
    public void TestRejectsFormalDirectoryAncestorsAndDescendants()
    {
        var local = Fresh();var formal = Path.Combine(local,"YanJi");
        foreach(var path in new[]{formal,formal.ToUpperInvariant(),Path.Combine(formal,"nested"),local})
            Assert.Throws<ArgumentException>(()=>RuntimeLocation.Resolve(true,path,false,local));
        var test = RuntimeLocation.Resolve(true,Fresh(),true,local);
        Assert.True(test.TestMode);Assert.Contains("隔离测试",test.WindowTitle);
        Assert.False(Directory.Exists(local));
    }
    [Fact]
    public void PathValidationRejectsFileAndJunctionWithoutFollowingIt()
    {
        var local=Fresh();var root=Fresh();Directory.CreateDirectory(root);
        var file=Path.Combine(root,"file");File.WriteAllText(file,"untouched");
        Assert.Throws<ArgumentException>(()=>RuntimeLocation.ValidateTestPath(Path.Combine(file,"child"),local));
        var target=Fresh();Directory.CreateDirectory(target);var link=Path.Combine(root,"junction");
        // Junctions do not require developer-mode symlink privileges.
        var start=new System.Diagnostics.ProcessStartInfo("cmd.exe") { UseShellExecute=false,CreateNoWindow=true };
        start.ArgumentList.Add("/c");start.ArgumentList.Add("mklink");start.ArgumentList.Add("/J");start.ArgumentList.Add(link);start.ArgumentList.Add(target);
        using var process=System.Diagnostics.Process.Start(start)!;process.WaitForExit();Assert.Equal(0,process.ExitCode);
        Assert.Throws<ArgumentException>(()=>RuntimeLocation.ValidateTestPath(Path.Combine(link,"child"),local));
        Assert.Empty(Directory.EnumerateFileSystemEntries(target));
    }
    [Fact]
    public void Schema4CanReopenWithoutMigrationAndKeepsExactMilliseconds()
    {
        var dir=Fresh();YanJiDatabase.ValidateStartupDirectory(dir);Assert.False(Directory.Exists(dir));
        using(var db=new YanJiDatabase(dir))db.SaveRecord(new("runtime-fact","math",null,SessionSource.Manual,"isolated",[TimeParts.Duration(new(2026,10,18),TimeSpan.FromMilliseconds(12345),"Asia/Shanghai")]),"fixture");
        var path=Path.Combine(dir,"yanji.sqlite");var before=SHA256.HashData(File.ReadAllBytes(path));
        YanJiDatabase.ValidateStartupDirectory(dir);Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(path)));
        using(var db=new YanJiDatabase(dir))Assert.Equal(12345,db.Records().Single().DurationMs);
    }
    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(5)]
    public void IncompatibleSchemaIsRejectedWithoutWriting(int schema)
    {
        var dir=Fresh();using(var db=new YanJiDatabase(dir)){}
        var path=Path.Combine(dir,"yanji.sqlite");
        using(var c=new SqliteConnection($"Data Source={path};Pooling=False")){c.Open();using var cmd=c.CreateCommand();cmd.CommandText=$"PRAGMA user_version={schema}";cmd.ExecuteNonQuery();}
        var before=SHA256.HashData(File.ReadAllBytes(path));
        Assert.Throws<InvalidDataException>(()=>YanJiDatabase.ValidateStartupDirectory(dir));
        Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(path)));
    }
    [Fact]
    public void FrozenOldSchemaFilesAreRejectedWithoutMigration()
    {
        foreach(var dir in new[]{LegacySchemaFixture.Create(),Schema2Fixture.Create(),Schema3Fixture.Create()})
        {
            var path=Path.Combine(dir,"yanji.sqlite");var before=SHA256.HashData(File.ReadAllBytes(path));
            Assert.Throws<InvalidDataException>(()=>YanJiDatabase.ValidateStartupDirectory(dir));
            Assert.Equal(before,SHA256.HashData(File.ReadAllBytes(path)));
        }
    }
    [Fact]
    public void CorruptEmptyAndUnknownFilesAreNeverRebuilt()
    {
        foreach(var content in new[]{Array.Empty<byte>(),new byte[]{1,2,3,4}})
        {
            var dir=Fresh();Directory.CreateDirectory(dir);var path=Path.Combine(dir,"yanji.sqlite");File.WriteAllBytes(path,content);
            Assert.ThrowsAny<Exception>(()=>YanJiDatabase.ValidateStartupDirectory(dir));Assert.Equal(content,File.ReadAllBytes(path));
        }
        var occupied=Fresh();Directory.CreateDirectory(occupied);File.WriteAllText(Path.Combine(occupied,"keep.txt"),"keep");
        Assert.Throws<InvalidDataException>(()=>YanJiDatabase.ValidateStartupDirectory(occupied));Assert.False(File.Exists(Path.Combine(occupied,"yanji.sqlite")));
    }
}
