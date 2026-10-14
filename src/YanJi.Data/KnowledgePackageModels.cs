using YanJi.Core;
namespace YanJi.Data;
public enum PackageChoice { KeepLocal, UseTemplate }
public sealed record PackageBinding(string SourceId, string NodeId, bool Active);
public sealed record PackageInstall(string Id, string ProfileId, string PackageId, string Version, Dictionary<string,string> Targets, long VersionStamp);
public sealed record PackageConflict(string Key, string NodeName, string Field, string? Baseline, string? Local, string? Incoming);
public sealed record PackageNodeChange(KnowledgeNode? Before, KnowledgeNode After, string Summary, int HistoricalReferences = 0);
public sealed record PackagePreview(string Payload, string ProfileId, Dictionary<string,string> Targets, string Token, string InstallId,
    string? RollbackOperationId, List<PackageNodeChange> Changes, List<PackageConflict> Conflicts, List<PackageBinding> Bindings,
    bool NoOp, string? Error = null)
{
    public int Added => Changes.Count(c=>c.Before==null);
    public int Archived => Changes.Count(c=>c.After.Archived && c.Before?.Archived!=true);
    public int Changed => Changes.Count(c=>c.Before!=null);
}
public sealed record PackageOperation(string Id,string InstallId,string Kind,string? FromVersion,string ToVersion,string? BeforePayload,string AfterPayload,
    List<PackageBinding> BeforeBindings,List<PackageBinding> AfterBindings,List<PackageNodeChange> Changes,DateTimeOffset At,bool RolledBack=false);
public sealed record PackageApplyResult(string InstallId,string? OperationId,string? BackupPath,bool NoOp);
