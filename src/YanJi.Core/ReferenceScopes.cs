using System.Text.Json;
using System.Text.Json.Serialization;

namespace YanJi.Core;

/// <summary>Presentation membership of a package external node; never a database identity.</summary>
public sealed record ReferenceScopeEntry(string PackageId, string ExternalNodeId, string? ParentId, IReadOnlyList<string> Variants);

/// <summary>Optional, external display scopes. Unknown or locally created nodes stay visible.</summary>
public sealed class ReferenceScopes
{
    private static readonly HashSet<string> Variants = new(StringComparer.Ordinal) { "common", "math1", "math2", "english1", "english2", "all" };
    private readonly Dictionary<(string Package, string Node), HashSet<string>> _members;
    public IReadOnlyList<ReferenceScopeEntry> Entries { get; }

    private ReferenceScopes(IReadOnlyList<ReferenceScopeEntry> entries, Dictionary<(string Package, string Node), HashSet<string>> members)
    { Entries = entries; _members = members; }

    /// <param name="directory">Root containing reference-scopes.json (normally knowledge-packages).</param>
    public static ReferenceScopes Load(string directory)
    {
        var path = Path.Combine(directory, "reference-scopes.json");
        if (!File.Exists(path)) return new(Array.Empty<ReferenceScopeEntry>(), []);
        if (new FileInfo(path).Length > 2 * 1024 * 1024) throw new InvalidDataException("参考范围清单超过2 MiB。");
        try
        {
            var data = JsonSerializer.Deserialize<ScopeDocument>(File.ReadAllBytes(path), new JsonSerializerOptions
            { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow }) ?? throw new InvalidDataException("参考范围清单为空。");
            if (data.SchemaVersion != 1 || data.Entries == null || data.Entries.Count > 10000) throw new InvalidDataException("参考范围清单版本或数量无效。");
            Dictionary<(string Package, string Node), ReferenceScopeEntry> map = [];
            Dictionary<(string Package, string Node), HashSet<string>> members = [];
            foreach (var entry in data.Entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.PackageId) || string.IsNullOrWhiteSpace(entry.ExternalNodeId)
                    || entry.Variants == null || entry.Variants.Count == 0 || entry.Variants.Any(v => v == null || !Variants.Contains(v))
                    || !map.TryAdd((entry.PackageId, entry.ExternalNodeId), entry)) throw new InvalidDataException("参考范围节点缺失、重复或类别无效。");
                members[(entry.PackageId, entry.ExternalNodeId)] = new(entry.Variants, StringComparer.Ordinal);
            }
            // Ensure each selected descendant retains its ancestors, even in a manually edited manifest.
            foreach (var entry in data.Entries)
            {
                HashSet<string> seen = new(StringComparer.Ordinal) { entry.ExternalNodeId };
                var current = entry;
                while (current.ParentId != null)
                {
                    if (!seen.Add(current.ParentId) || seen.Count > 32 || !map.TryGetValue((entry.PackageId, current.ParentId), out var parent))
                        throw new InvalidDataException("参考范围父节点缺失、成环或超过32层。");
                    members[(entry.PackageId, parent.ExternalNodeId)].UnionWith(members[(entry.PackageId, entry.ExternalNodeId)]);
                    current = parent;
                }
            }
            var result = data.Entries.Select(e => e with { Variants = Array.AsReadOnly(members[(e.PackageId, e.ExternalNodeId)].Order(StringComparer.Ordinal).ToArray()) }).ToList().AsReadOnly();
            return new(result, members);
        }
        catch (Exception e) when (e is JsonException or ArgumentException or NotSupportedException)
        { throw new InvalidDataException("参考范围清单格式无效。", e); }
    }

    /// <summary>Common is shared; each variant includes common plus its own members. all/blank/unknown variants display everything.</summary>
    public bool Includes(string packageId, string externalNodeId, string variant)
    {
        if (string.IsNullOrWhiteSpace(variant) || variant == "all" || !Variants.Contains(variant)) return true;
        if (!_members.TryGetValue((packageId, externalNodeId), out var members)) return true;
        return members.Contains("all") || members.Contains("common") || members.Contains(variant);
    }

    private sealed record ScopeDocument(int SchemaVersion, List<ReferenceScopeEntry> Entries);
}
