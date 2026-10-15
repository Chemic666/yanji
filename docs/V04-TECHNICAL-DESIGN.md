# V0.4 M0 — 推荐技术设计

**状态：设计建议，未实现。** 事实索引见 [当前架构](V04-CURRENT-ARCHITECTURE.md)，需求差异见 [差异分析](V04-GAP-ANALYSIS.md)。M0 不生成正式 SQL 迁移文件、不改 C#、不打开真实库。

## 1. 决策摘要与方案比较

### D01：原 chapters 存储扩展，KnowledgeNode 领域统一

| 方案 | 收益 | 代价/风险 | 决策 |
|---|---|---|---|
| 扩展 chapters，同 ID 表示任意知识节点 | 旧 sessions.chapter FK、WeakPoint.ChapterId、修订快照不换 ID；最少搬迁 | 旧 SaveChapter 的 positional INSERT 必改，名称需兼容适配；Schema升级 | **推荐** |
| 新 knowledge_nodes 表替代 chapters | 命名清楚、字段自由 | sessions FK重建/双向映射、JSON历史解码和删除恢复更复杂 | 本项目收益不足，不选 |
| 两套长期并存并双写 | 旧API表面少改 | 章节/节点状态双事实、同步与备份不一致 | 拒绝 |

[事实依据] S01/S03/S04/S05。领域引入 KnowledgeNode；`Chapter` 及 `ChapterId` 先作兼容边界，物理表仍叫 chapters。**旧 Chapter.Id=x → KnowledgeNode.Id=x，原地一一对应，不改号、不按名称匹配、不新建第二份时长。** 对新记录 UI 称“主要知识节点”，存储仍 sessions.chapter。

### D02：邻接表，原生 WPF 树，保持模块化单体

parent_id 邻接表：移动只改父指针，规则清楚；祖先/后代用有界 DFS 或递归查询。物化路径改父要更新后代；closure table 写入/校验与备份复杂、可能膨胀，当前不采用。不要把标题拼成持久路径身份。

原生 TreeView + 虚拟化/懒展开优先；平铺可见行 ListBox 仅作 M4 性能未达标的后备，需要自行补完整树键盘/自动化语义。现有 RecordList 是可复用样板，不是大树已验收证明。无需 WPF UI/第三方树/图表或 EF/MediatR。

### D03：数据库 Schema2；旧程序拒绝打开

application_id 保持0x594A4931；user_version 1→2。新创建库与升级库都重放同一受版本控制的 Schema1+迁移2过程，生成相同预期 Schema 定义；继续严格校验表/索引/约束，不因为支持扩展就容忍未知表。

[事实] V0.3.1 的 CheckIdentity 只接受1，CheckSchema 也拒绝新增结构（S03）。因此 Schema2 不能交给旧程序读写；不降低版本号骗过校验。回退V0.3.1须完整恢复升级前Schema1快照，升级后新学习不会自动合并回去，UI必须说明。

## 2. 模块边界 [建议]

- **Core/Knowledge**：KnowledgeNode、NodeKind、ExamProfile、图规则/祖先集合、进度语义、LearningContext、KnowledgePackage 结构/校验/三方合并计划；无文件/DB/UI。
- **Data**：同一个 YanJiDatabase/同一 `_gate`；将类声明改 partial，新增 Schema、Migrations、Knowledge、KnowledgePackages、Context 分文件，复用参数化 Command/Exec/Audit。图校验、引用检查、节点/映射/上下文/审计/版本写入同事务。保留旧公开 API，逐步由适配器调用新 API，不同时重写原时间/记录事务。
- **App**：KnowledgeWorkspaceViewModel、NodePickerViewModel、PackageImportViewModel；显式注入现有 Data/轻量服务，MainViewModel协调刷新。新 UserControl 和 Dialog 放入已有壳，不重构其他六页。
- **升级/恢复边界**：App 单实例取得后、构造普通 MainViewModel/自动备份之前，由版本感知 Bootstrap 只读识别库和执行获用户确认的升级流程；不让新旧 YanJiDatabase 构造器偷偷改库。

一次成功写提交后刷新目录、关联记录与图表；失败不发布刷新事件。不引入泛化命令框架、分布式锁、事件溯源系统。

## 3. 推荐 Schema2（字段/约束设计，不是可执行迁移）

| 结构 | 字段与关键约束 |
|---|---|
| exam_profiles 新表 | id PK、name、exam_key、admission_year、exam_date?、archived、row_version；已被引用档案不改考试类别/入学年，日期/名字可审计修正 |
| exam_subjects 新表 | (profile_id,subject_id) PK/FK；variant?、display_name?、sort_order、archived、catalog_version；组合变体和别名不改全局Subject身份 |
| chapters 扩展 | 保留旧6字段；追加 profile_id 可空SQL FK（领域必须有效）、parent_id? FK chapters、node_kind(Chapter/KnowledgePoint)、description、sort_order、deleted 0/1、row_version>0 |
| session_context 新表 | session_id PK/FK sessions；profile_id FK；node_label_snapshot_json?、capture_kind?、captured_at?；一个 session 恰一个上下文；主节点只以 sessions.chapter 为事实 |
| tasks / weakpoints JSON | 可选新增 ExamProfileId；Task另加 KnowledgeNodeId?；WeakPoint继续 ChapterId 指统一节点；缺字段旧payload按 legacy-default 兼容读取，不批量重写旧JSON |
| PeriodReview JSON | 新增可选 ExamProfileId，旧文本/范围/版本JSON不变；缺字段旧review按legacy解释，后续编辑须保留原作用域；不伪造学习评价 |
| schema_migrations 新表 | target_version PK、script_digest、applied_at；与迁移同事务记录 |
| knowledge_package_versions 新表 | (package_id,version) PK；digest、schema_version、validated_payload_json、source/license元数据；同版本不同内容拒绝；跨档案共享纯包快照 |
| knowledge_package_installs 新表 | install_id PK、profile_id FK、package_id/version FK versions、binding_json、state、row_version；unique(profile_id,package_id) |
| knowledge_package_nodes 新表 | (install_id,source_node_id) PK、node_id FK且唯一；binding_state、override_fields_json；来源/本地身份分离、解绑与删除意图持久化 |
| knowledge_package_operations 新表 | op_id PK、install_id FK、from/to版本、前后差异JSON、at、status/rollback_of；保留逆向计划依据，非时长事实 |

索引：chapters(profile_id,subject,parent_id,sort_order,id)、chapters(parent_id)、sessions(chapter)、session_context(profile_id,session_id)、revisions(kind,entity,at)、包节点/安装查找。无需新增每日总时长或知识投入事实表。

[约束边界] SQLite ADD COLUMN 的 FK 默认值须为 NULL；不要在启用外键时直接加引用列的非NULL常量默认值。迁移先加 nullable profile_id 再事务回填；模型/写入/CheckAllData 要求有效非空及属于 exam_subjects，不能只依赖 FK。parent FK仅证明存在，环/跨科目/跨档案/深度要语义验证。避免重命名/删除被引用 chapters 表带来的 FK重定向风险。[SQLite ALTER TABLE](https://www.sqlite.org/lang_altertable.html)、[Foreign keys](https://www.sqlite.org/foreignkeys.html)。

[建议] 两种 node_kind 均允许子节点，不限制将知识点细分成子知识点。进度0..100、Mastery旧枚举原样保留。建议最大深度32、每包最多10000节点，数值是防护起点，须M4实测，不是已证实容量。

## 4. 旧数据映射与学习上下文

- 建立 `legacy-default` 档案，入学2028（用户已批准旧语义）；日期取旧合法 exam-date，未知保持空。各旧Subject完整进入该组合，变体未知不推断“数学一/英语一”。原 archived 与 rowid顺序保留。
- 旧章节同 ID，parent=NULL、kind=Chapter、description空、deleted=false、profile=legacy-default；sort_order按旧科目内rowid顺序分配，Progress/Mastery/Archived不变，row_version从1起（不补造旧审计版本）。
- 为每条旧 session（含软删）建立 session_context→legacy-default；旧路径快照为空并注明“旧版未记录”。**sessions/parts自身字段、Focus settings、原 revisions/settings payload 不修改。** Task/WeakPoint/Review缺档案字段读取时归legacy；旧 ChapterId 保持，不按标题重新关联。
- 考试日期新唯一事实为 exam_profiles.exam_date；旧 exam-date 设置仅保留迁移来源，不再双写。统计时区/检查点/计时偏好继续全局；每日感想仍按自然日全局，UI标注不因切档案复制。
- Core 新增可选 LearningContext 元数据（ExamProfileId、路径快照/采集时点/采集类型），附于 StudyRecord/TimerSnapshot；旧序列化字段缺失用明确兼容解码。Task一键开始捕获节点→原ChapterId和档案，FocusContext任务ID/标题仍原语义。新增上下文不修改 TimerMachine 时间计算。
- SaveTimer新record构造、Completed幂等比较、durable恢复、ReadRecords、ValidateBackup检查点比较必须贯穿Context；与session/parts/focus/checkpoint同事务。不能只保存一个VM选择值。
- 新记录建立时捕获目录标签。修正主节点/档案时捕获“修正时目录”并审计；只改备注或节点本身改名，不刷新记录时快照。旧记录没有原路径，不通过当前名称冒充开始时名称。
- 新JSON字段必须在编辑器用旧值更新/显式保留，不沿用 S10 的旧六参数重建导致上下文掉落。旧 SaveChapter 只作兼容适配：保留新结构字段；新UI走带expectedVersion的节点API。

## 5. 目录操作与统计不变量

1. Node.Id永久稳定。Rename审计；Move限同档案/同科目，禁止自己/后代为父；所有结构变更在事务内对最终图验证，排序同级增量位置，必要时同事务重排。用Node.row_version与exam_subjects.catalog_version防陈旧编辑/导入计划，不建泛化并发框架。
2. 已落盘节点删除为软删，保留ID、名称和审计；整枝删除/归档先展示节点及关联记录数量，只改节点状态，不删学习/任务/薄弱点。父状态在新选择器中有效继承，历史仍显示；恢复子节点需恢复祖先或显式移到活动父节点。未保存草稿可直接取消。
3. 归档与软删分开；包上游移除默认产生可见预览，不自动恢复用户软删。来源节点ID不可重用为不同概念，同版本哈希也不能变。
4. 有活动检查点时阻止迁移/恢复/包应用及会影响当前路径的结构修改；只读浏览/预览不影响计时。档案与开始上下文冻结；任务目标变更不改已开始学习的归属。手工进度/掌握短事务可独立保存，性能须实测。
5. “自身时长”是 primary node==N 的有效片段；“含后代”是 primary node∈Descendants(N)∪{N} 的有效片段集合。按Part.Id去重（若查询重复JOIN必须修复，不能靠格式化掩盖）；筛选档案、科目、日期，再聚合原始毫秒。
6. 例：父自身30分钟、子20分钟，父子树50、子20，科目总计50而非70。父子多选先规约为无祖先重叠的节点集合；目录排行可分别显示包含指标，但明确不把每行相加当总计。根分区+未关联+已归档/删除类别才能对账。
7. 节点改名/同科目移动不改变科目与每日总时长；当前树子树归类可以变化，UI明确“按当前目录统计”。未关联节点记录保留科目/档案统计，单列未指定节点。
8. 计时已确认parts可统计；未知gap不进入分析。跨日按part.Date，绝不能按session.CreatedAt归整次记录。任务状态、掌握、薄弱点验收不用时长推出；父进度和点数统计分口径。

## 6. 纯数据知识包

[建议] 本地 UTF-8 JSON `.yanji-knowledge.json`，不使用可执行脚本、SQL、XAML、HTML、外部附件或在线依赖。不把学习备份ZIP当知识包。格式SchemaVersion=1与数据库SchemaVersion=2独立。

```json
{
  "PackageId": "org.yanji.example.demo",
  "PackageVersion": "1.0.0",
  "SchemaVersion": 1,
  "Title": "格式演示（不是考试大纲）",
  "ExamKey": "cn-postgraduate",
  "AdmissionYears": [2028],
  "Sources": [{"Title": "用户提供的示例说明", "Url": null, "AccessedAt": null}],
  "License": {"Id": "LicenseRef-UserDeclared", "Text": "需由实际发布者提供许可声明"},
  "Subjects": [{"Key": "demo-subject", "Name": "演示科目", "Variant": null}],
  "Nodes": [
    {"Id": "demo-root", "SubjectKey": "demo-subject", "ParentId": null, "Kind": "Chapter", "Name": "演示目录", "Description": "", "Order": 0},
    {"Id": "demo-point", "SubjectKey": "demo-subject", "ParentId": "demo-root", "Kind": "KnowledgePoint", "Name": "演示知识点", "Description": "", "Order": 0}
  ]
}
```

这是文档格式样例，不是内置内容，不授予真实资料许可，也不声明官方权威。PackageVersion V0.4使用严格数字三段版本；Prerelease/自动在线更新不在范围。AdmissionYears明指入学年度，不能据此造考试日期。

### 6.1 验证与组织

- 文件建议≤16MiB、JSON深度≤64、节点≤10000、目录深度≤32；ID≤128字符、名称≤200、描述≤20000；都是待验证边界。先检查文件大小，再后台解析；不在UI线程展开全部节点。
- 严格字段/类型/重复属性检查，未知主Schema拒绝；PackageId/版本、来源与非空许可必填。ID ordinal比较，稳定ID不由中文标题生成；节点唯一、父存在、同SubjectKey、无环、Order在兄弟范围唯一，枚举有效。
- 包不允许Progress/Mastery/Task/StudyPart/时间事实字段；所有新节点进度0、掌握Unknown。来源/许可是可追溯声明，不因SHA-256就认为来源权威。
- 内置资源将组织于 `resources/knowledge-packages/<PackageId>/<version>/...json`，带受核实的来源和许可声明清单；只随正式版本带已核实/自编明确标注的内容。M0不制作任何所谓2028官方大纲。
- 预览选择目标档案，将SubjectKey映射到该组合或显式新建科目；不同变体冲突提示。不同档案安装同包使用不同install_id/本地node GUID；source Id不直接作本地PK。不要按名称自动吸纳旧节点。

### 6.2 幂等与三方更新

幂等键：(ProfileId,PackageId,PackageVersion,Digest)。同安装同版本同digest→零写入、零重复审计；同版本不同digest→错误；升级从当前完整基线走计划，不要求用户有每个中间版本；低版本导入拒绝，回滚走独立入口。

保存 B=已安装上游包快照、L=本地当前节点、U=新包。parent字段先通过source→local映射转为同一ID空间再比较。仅合并包拥有的Name/Description/Kind/Parent/Order等内容字段；学习进度/掌握、学习记录、任务状态、薄弱点不属于包。

| 条件 | 默认处理 |
|---|---|
| L=B，U变化 | 预览后更新包字段 |
| U=B，L变化 | 保留本地 |
| L=U | 无冲突，维持 |
| 三者分别不同 | 冲突；默认保留本地，用户可用上游/编辑后采用 |
| 用户软删/解绑 | 保留意图，不因为上游重新出现而恢复 |
| 上游删有用户修改/本地子节点/历史关联的节点 | 保留或解绑本地，显示结构冲突；不得静默归档整枝隐藏用户内容 |
| 上游删未修改且无受影响内容 | 预览归档来源节点并保留映射墓碑，不物理删除 |

本地新增子节点不随包消失。未解决结构冲突不能提交；选择后对最终全图重新验证，防合法输入合并后产生环/孤儿。显式“恢复包默认值”解除对应覆盖意图，审计保留；不把它等同于恢复用户学习进度。

### 6.3 提交与回滚

预览纯读：保存包digest、安装版本/row_version和目标scope.catalog_version；计时检查点不增加目录代数，但节点编辑增加。应用在单个非延迟写事务中重新校验计划/引用，写节点、绑定、安装版本、operation和revision，最后commit；失败/取消全部rollback，缓存与UI只在提交后刷新。先保留可校验的完整备份；存在活动计时拒绝应用。复用SQLite事务，不引入两阶段外部服务。[Microsoft.Data.Sqlite transactions](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions)。

包版本回滚是**目录/绑定逆向计划**，不是恢复整库：后续学习记录绝不能丢。以operation.before/after对比当前包字段生成冲突预览；保留后来Progress/Mastery/任务/薄弱点和学习事实。新版本新增节点已被使用则保留为解绑本地节点，不删除；旧映射保留inactive墓碑，下次重引入需显式处理。之后同事务恢复安装指针及内容、写逆向operation。当前存在用户内容冲突时先解决，不强行“完全回滚”。完整备份恢复另走既有明确确认/全替换UI。

## 7. Schema1→2 的安全升级

[建议] 优先阴影副本升级：不直接对用户活库边迁移边显示新界面。

1. 单实例取得数据目录后，版本感知识别application_id/严格Schema1/完整语义；未知或损坏库退出。存在active_timer（含暂停/待核对）先在兼容程序结束/核对，本次不自动清零或确认未知时长。
2. 创建并验证独立的升级前Format1/Schema1备份；通过SQLite Backup API再建立工作副本，不能只复制打开中的主文件。保存操作ID、源/候选/rollback路径与校验信息的本地升级阶段记录；不是计时事实。
3. 在副本单事务创建完整Schema2表、追加节点列、回填默认档案/旧节点/上下文，校验旧事实指纹，再写schema_migrations/user_version=2、commit。M1确定全V0.4 Schema2（包表先为空）；M2不能偷偷加表仍叫2，若改变必须新版本迁移。
4. 新建库也用相同Schema1+Migration2生成，CheckSchema2预期在内存按同脚本构造；校验原有表/约束和新增定义。记录旧sessions/parts所有原字段、旧Chapter的6字段、settings/revisions原JSON等有序指纹，候选须相等；新增上下文/结构列单独验证。包/树语义也要检查，不能只看 integrity_check。
5. 验证候选2的完整性/foreign_key_check/语义/上下文。停止所有源连接、按原机制执行WAL检查点并核验busy和残余页，源快照指纹未变才提升；外部连接/占用不能证明已关闭则停止，不盲删WAL/SHM。
6. File.Replace提升候选并留旧库rollback，再以新校验器打开；全部替换后异常纳入回退范围，成功才标记完成。候选/旧库/备份不覆盖已有文件。
7. 启动遇到中断阶段记录，实际检查现有主库/候选/rollback身份与校验，不凭状态字串猜成功。完整Schema2可确认完成；不确定显示恢复选择，不能自动覆盖可能的新学习数据。跨进程异常/文件系统行为必须M1实际隔离验证；真实断电不由模拟通过代表。

旧程序不能继续打开Schema2。允许回退的路径是先保留当前2的完整备份，再由明确选择将升级前1完整恢复给V0.3.1。没有自动降级或1/2同时写库。

## 8. 备份/恢复扩展

[事实基础] S06。保留2文件容器；新manifest Format2、Schema2、DB SHA，保留原物理sessions/parts Inventory，再追加节点（含归档/删）、档案/组合/上下文、包安装/来源绑定/版本digest等清单；不把物理记录数量换成有效统计。

CheckAllData2必须验证父图/同scope/深度、有效profile、session_context一对一、Task/WeakPoint/Review JSON引用、包映射/当前版本/覆盖元数据、旧新revision和活动检查点。引用已软删/归档节点允许历史访问；它们不准成为新学习的默认选项。

恢复Format2：验证独立候选、先备份当前2、检查点与原子替换/rollback照旧加强。恢复Format1：旧校验器先严格读候选1，迁移候选到2，再验证并**完整替换**当前2；提示该恢复会覆盖现有知识目录与包，不能隐式合并。Manifest/Schema/文件SHA彼此一致，未知版本拒绝。

512MiB数据库限制先保留并做容量评估；包版本快照跨档案共享，操作只存必要差异，防知识导入使备份不可用。任何备份/空间校验失败都阻止升级或导入，不能降级为不备份继续。V0.4不自动清理旧数据/备份/证据。

## 9. WPF 与交互方案

- 复用现有“计划与进度”入口为知识工作区；大窗口有限高度 Grid 双栏（目录约280–360 DIP+详情），小窗口内容宽不足约880 DIP切列表/详情单栏返回，避免嵌套无限测量。断点是目标，M4实测后调整。
- 原生TreeView启用IsVirtualizing/Recycling，默认折叠、按需要加载子节点；不沿用全展开。独立ScrollViewer/有限视口，不能套现有MainScroll无限StackPanel后宣称已虚拟化。[TreeView performance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/how-to-improve-the-performance-of-a-treeview)、[WPF controls performance](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-performance-controls)。
- NodePicker为可搜索树弹窗，展示完整路径/科目/档案；候选仅当前合法scope，切科目清空旧选择并恢复该scope合法最近项，未指定始终可选，空状态有“管理目录”入口。编辑历史仍能看到归档/删节点。不能将弹窗做新必填步骤。
- 节点详情有进度、自评掌握、当前任务、薄弱点验收、时间投入、历史修改和原始记录入口；自身/子树下钻传完整scope/date/state，不把点击父节点误当ID相等筛选。
- 新导入窗口只展示差异/来源许可/目标组合与冲突；批量“保留本地”只用于可安全字段，结构冲突必须逐类解决；确认页包含新增/更新/归档/保留/冲突数量、备份结果和版本，不用大卡片堆砌。
- 搜索对内存不可变节点索引后台执行，150–250ms防抖/取消，最多先展示200条并可分页/定位祖先；中文包含/ID/路径匹配，不先加FTS/拼音依赖。索引结果绑定scope代数，丢弃旧响应；节点变更只刷新受影响scope。
- KnowledgeBusy与MainViewModel.Busy分开；SQLite/JSON耗时不在UI线程，也不能误以为Execute*Async就是非阻塞。继续用已验证后台适配和Dispatcher更新ObservableCollection。[Microsoft.Data.Sqlite async](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async)。
- 复用Ink/Muted/Accent/SectionSurface、FocusIcon、NaturalDuration、ConfirmDialog、StudyChart、MetricCard；状态用文字/标签，不仅颜色。F2/方向/Tab/Enter可达，编辑输入/IME期间不能触发学习快捷键，Move/Sort有按钮/对话框，不依赖拖拽。

[待验证预算] 1000/10000节点两档；10000冷载≤2秒、搜索结果（不含防抖）p95≤200ms、常规展开/选择≤100ms，交互无≥250ms UI连续停顿；在目标本机记录条件，不宣称现在达成。不做10k真实UI容器全展开，无声明100%覆盖率。

## 10. 能否进入 M1

**设计层面可以，执行仍需下一轮授权。** 首项是隔离Schema1→2/备份/旧程序拒开验证，再提供节点/档案基础；不能先铺知识包UI再补数据安全。模型在M1定版，迁移失败/原事实指纹变更/旧程序可写2/计时Context丢失任一出现都阻止后续集成。

本轮的6项内存探针、WPF Skill查询与历史41测试/30采样核查不代表Schema2或知识包已实现。真实用户升级、实际电源中断、大树性能、V0.4渲染仍NOT TESTED。
