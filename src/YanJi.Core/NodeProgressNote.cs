namespace YanJi.Core;

/// <summary>Authored learning progress; independent of template descriptions and study-time facts.</summary>
public sealed record NodeProgressNote(string NodeId, string Content, string Next,
    long Version = 0, DateTimeOffset? UpdatedAt = null);
