using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using System.Text.RegularExpressions;
namespace YanJi.Core;

public sealed record PackageSource(string Id, string Title, string? Url = null);
public sealed record PackageLicense(string Id, string Text);
public sealed record PackageSubject(string Key, string Name, string? Variant = null);
public sealed record PackageNode(string Id, string SubjectKey, string? ParentId, KnowledgeNodeKind Kind, string Name, string Description, int Order);
public sealed record KnowledgePackage(string PackageId, string PackageVersion, int SchemaVersion, string Name, string Description,
    string Maintainer, string ExamKey, List<int> AdmissionYears, List<PackageSource> Sources, PackageLicense License,
    List<PackageSubject> Subjects, List<PackageNode> Nodes, string ContentHash = "");

public static class KnowledgePackageFile
{
    public const int MaximumBytes = 16 * 1024 * 1024;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, Converters = { new JsonStringEnumConverter(allowIntegerValues: false) } };
    public static KnowledgePackage Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > MaximumBytes) throw new InvalidDataException("知识包超过16 MiB，请缩小文件。");
        try
        {
            var text = new UTF8Encoding(false, true).GetString(bytes);
            if (text.StartsWith('\ufeff')) text = text[1..];
            using var json = JsonDocument.Parse(text, new JsonDocumentOptions { MaxDepth = 64 });
            CheckProperties(json.RootElement);
            var package = JsonSerializer.Deserialize<KnowledgePackage>(text, Options) ?? throw new InvalidDataException("知识包内容为空。");
            Validate(package);
            if (package.ContentHash != Digest(package)) throw new InvalidDataException("知识包内容哈希不匹配，文件可能已损坏或未重新签制。");
            return package;
        }
        catch (Exception e) when (e is JsonException or DecoderFallbackException or ArgumentException or NullReferenceException or OverflowException)
        { throw new InvalidDataException("知识包字段、类型或UTF-8编码无效。", e); }
    }
    private static void CheckProperties(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            HashSet<string> names = new(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject()) { if (!names.Add(property.Name)) throw new InvalidDataException("知识包含重复JSON字段：" + property.Name); CheckProperties(property.Value); }
        }
        else if (value.ValueKind == JsonValueKind.Array) foreach (var item in value.EnumerateArray()) CheckProperties(item);
    }
    public static string Encode(KnowledgePackage package)
    {
        Validate(package); var text = JsonSerializer.Serialize(package with { ContentHash = Digest(package) }, Options);
        if (Encoding.UTF8.GetByteCount(text) > MaximumBytes) throw new InvalidDataException("知识包超过16 MiB。");
        return text;
    }
    private static string Digest(KnowledgePackage package)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(package with { ContentHash = "" }, Options));
        using var buffer = new MemoryStream(); using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping })) Canonical(writer, doc.RootElement, true);
        return Convert.ToHexString(SHA256.HashData(buffer.ToArray()));
    }
    private static void Canonical(Utf8JsonWriter writer, JsonElement value, bool root = false)
    {
        if (value.ValueKind == JsonValueKind.Object) { writer.WriteStartObject(); foreach (var p in value.EnumerateObject().Where(p => !root || p.Name != "ContentHash").OrderBy(p => p.Name, StringComparer.Ordinal)) { writer.WritePropertyName(p.Name); Canonical(writer, p.Value); } writer.WriteEndObject(); }
        else if (value.ValueKind == JsonValueKind.Array) { writer.WriteStartArray(); foreach (var item in value.EnumerateArray()) Canonical(writer, item); writer.WriteEndArray(); }
        else value.WriteTo(writer);
    }
    public static Version VersionOf(string value)
    {
        if (value == null || !Regex.IsMatch(value, @"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$", RegexOptions.CultureInvariant)) throw new InvalidDataException("包版本须为数字三段，例如1.0.0。");
        if (!Version.TryParse(value, out var version)) throw new InvalidDataException("包版本数值过大。"); return version;
    }
    public static void Validate(KnowledgePackage p)
    {
        void Text(string? value, int length, string field, bool empty = false, bool multiline = false)
        {
            if (value == null || value.Length > length || !empty && string.IsNullOrWhiteSpace(value)) throw new InvalidDataException(field + "缺失或过长。");
            foreach (var rune in value.EnumerateRunes())
                if (rune.Value == 0xfffd || Rune.GetUnicodeCategory(rune) is UnicodeCategory.Format || Rune.GetUnicodeCategory(rune) == UnicodeCategory.Control && !(multiline && rune.Value is 9 or 10 or 13))
                    throw new InvalidDataException(field + "包含不可见控制字符。");
        }
        void Id(string? value, string field) { Text(value,128,field); if (!Regex.IsMatch(value!, @"^[A-Za-z0-9][A-Za-z0-9._:-]*$", RegexOptions.CultureInvariant)) throw new InvalidDataException(field + "只能使用字母、数字、点、横线、冒号或下划线。"); }
        if (p.SchemaVersion != 1) throw new InvalidDataException("不支持的知识包格式版本。");
        Id(p.PackageId,"PackageId"); VersionOf(p.PackageVersion); Text(p.Name,200,"名称"); Text(p.Description,20000,"简介",true,true); Text(p.Maintainer,200,"维护方"); Id(p.ExamKey,"考试类型");
        if (p.AdmissionYears == null || p.AdmissionYears.Count>201 || p.AdmissionYears.Any(y=>y is <2000 or >2200) || p.AdmissionYears.Distinct().Count()!=p.AdmissionYears.Count) throw new InvalidDataException("适用入学年度无效；空数组表示通用。");
        if (p.License == null) throw new InvalidDataException("缺少许可声明。"); Text(p.License.Id,128,"许可证"); Text(p.License.Text,20000,"许可说明",false,true);
        if(p.Sources==null||p.Sources.Count is <1 or >100||p.Sources.Select(s=>s?.Id).Distinct().Count()!=p.Sources.Count) throw new InvalidDataException("来源标识缺失或重复。");
        foreach(var s in p.Sources) { if(s==null)throw new InvalidDataException("来源为空。");Id(s.Id,"来源ID");Text(s.Title,500,"来源标题");if(s.Url!=null && (!Uri.TryCreate(s.Url,UriKind.Absolute,out var uri)||uri.Scheme is not("http" or "https")||!string.IsNullOrEmpty(uri.UserInfo)||s.Url.Length>2048)) throw new InvalidDataException("来源网址无效。"); }
        if(p.Subjects==null||p.Subjects.Count is <1 or >32||p.Subjects.Select(s=>s?.Key).Distinct().Count()!=p.Subjects.Count) throw new InvalidDataException("科目标识缺失或重复。");
        foreach(var s in p.Subjects) { if(s==null)throw new InvalidDataException("科目为空。");Id(s.Key,"科目键");Text(s.Name,200,"科目名称");if(s.Variant!=null)Text(s.Variant,128,"科目变体"); }
        if(p.Nodes==null||p.Nodes.Count>10000) throw new InvalidDataException("节点最多10000个。");
        Dictionary<string,PackageNode> map=new(StringComparer.Ordinal);
        foreach(var n in p.Nodes) { if(n==null)throw new InvalidDataException("节点为空。");Id(n.Id,"节点ID");Id(n.SubjectKey,"节点科目");if(!map.TryAdd(n.Id,n))throw new InvalidDataException("节点ID重复："+n.Id);Text(n.Name,200,"节点名称");Text(n.Description,20000,"节点说明",true,true);if(!Enum.IsDefined(n.Kind)||n.Order<0||!p.Subjects.Any(s=>s.Key==n.SubjectKey))throw new InvalidDataException("节点类型、顺序或科目无效。"); }
        if(p.Nodes.GroupBy(n=>(n.SubjectKey,n.ParentId,n.Order)).Any(g=>g.Count()>1))throw new InvalidDataException("同级节点顺序重复。");
        foreach(var n in p.Nodes) { HashSet<string> seen=[];var current=n;while(true) { if(!seen.Add(current.Id))throw new InvalidDataException("目录包含循环。");if(seen.Count>32)throw new InvalidDataException("目录深度最多32层。");if(current.ParentId==null)break;if(!map.TryGetValue(current.ParentId,out var parent)||parent.SubjectKey!=current.SubjectKey)throw new InvalidDataException("父节点缺失或跨越科目。");current=parent; } }
    }
}
