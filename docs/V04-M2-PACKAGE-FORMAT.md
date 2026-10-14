# 知识包 JSON 格式1

实现：`src/YanJi.Core/KnowledgePackage.cs`。示例：`resources/knowledge-packages/org.yanji.reference.408/1.0.0/package.yanji-knowledge.json`；7个内置包在同目录的 index.json 中索引。

顶层字段：PackageId、PackageVersion、SchemaVersion=1、Name、Description、Maintainer、ExamKey、AdmissionYears、Sources、License、Subjects、Nodes、ContentHash。大小16 MiB，严格UTF-8（允许BOM），JSON深度64；拒绝未知字段与重复属性。

- PackageId/科目Key/节点Id/来源Id：最多128字符，ASCII字母数字及 `._:-`，ordinal身份；不由本地显示标题识别。同名节点互不合并。
- PackageVersion：非负整数三段，禁止前导零、预发布/构建后缀。SchemaVersion 是文件格式，与数据库Schema3不同。
- AdmissionYears：入学年度2000～2200，无重复；空数组表示通用，不代表2028官方范围。
- Sources：1～100条 `{Id,Title,Url?}`；Id稳定，URL可空或http/https、无凭证，永不自动访问。License `{Id,Text}` 为声明，不认证许可合法性。
- Subjects：1～32条 `{Key,Name,Variant?}`；每个键明确绑定选定档案内的独立目标科目。
- Nodes：最多10000条 `{Id,SubjectKey,ParentId?,Kind,Name,Description,Order}`；Kind为Chapter/KnowledgePoint字符串；父必须同科目，禁止环和超过32层；同级Order非负且不重复；名字≤200、说明≤20000。禁止学习时长、掌握或任务字段。只有说明/许可文本允许换行/tab；拒绝格式控制字符、双向控制和替代字符。
- 稳定外部Id在新版本中保留，即使重命名、移动或排序。发布者不得将删除的Id重用于不同概念。生成工具只创建固定1.0.0数据且拒绝覆盖；后续维护版本必须显式保留现有Id，不能按新位置重新编号。

## 内容哈希

使用模型的所有字段（包括可空字段），移除顶层ContentHash；递归按属性名ordinal排序，数组顺序保留，UTF-8无BOM、无空白，枚举字符串、整数十进制，字符串采用System.Text.Json UnsafeRelaxedJsonEscaping转义。SHA-256大写64位hex。`KnowledgePackageFile.Encode`是权威生成器；`Parse`重新生成摘要并校验。哈希不是签名，不认证作者。

导入先打开只读文件并检查长度，再在后台读取解析；失败没有数据库写入。相同PackageId/版本不同内容拒绝；同PackageId维护方及来源Id/Url集合变化拒绝，须核查来源或使用新的PackageId。来源标题可以在新版本更正，不改变来源身份。
