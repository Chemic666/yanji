# V0.4 M0 — 当前架构核查

核查日期：2026-10-14。范围：源码、Schema 定义、Git、既有隔离证据；未打开真实学习数据库/备份，未启动客户端，未运行正式迁移或产品构建。

标注约定：**[事实]**=已读源码/证据或用户明确范围；**[建议]**=V0.4 推荐方案，尚未实现；**[待验证]**=假设/未来验收目标，不能当作 PASS。其余四份文档沿用此约定。

## 1. 基线与范围

[事实] HEAD `77f036d20844f529c9f19b7ee9558ec2eed108f2`；产品 Tag `v0.3.1` 指向 `40022179b05161fee5c0300c546a670082025ae5`。二者差异严格只有 `docs/ARTIFACT-RETENTION.md`、`tools/Audit-ProjectSpace.py`，均已在上轮独立提交。M0 开始时工作区干净，不存在用户提及的两项未提交治理文件。两项文件原样保留，本轮不重新提交。

10 个 Tag 均可解析：v0.1.0、v0.1.1、v0.2-visual-preview.1、v0.2-visual-preview.1-delivery、v0.2.0、recovery/v0.2.1-before-v022、v0.2.2、v0.2.3、v0.3.0、v0.3.1。关键提交：v0.2.2→81eea673；v0.3.0→869e410c；v0.3.1→40022179。注解 Tag 对象号不等于目标提交号。

[事实] 六个业务模块之外还有独立记录明细页。V0.4 本轮仅设计考试档案/知识目录/知识包/关联与统计，不设计完整错题、模考、AI、社交、云同步、移动端。

## 2. 项目与依赖

| 项目 | 职责与依赖 [事实] |
|---|---|
| YanJi.Core | net10.0；records、TimerMachine、TimeParts、StudyAnalytics、NonStudyCountdown；无 UI/SQLite 依赖 |
| YanJi.Data | 引用 Core；Microsoft.Data.Sqlite 10.0.12；一个 YanJiDatabase，手写参数化 SQL |
| YanJi.App | net10.0-windows、WPF；WindowsForms 仅用于托盘；引用 Data、CommunityToolkit.Mvvm 8.4.2 |
| YanJi.Tests | 引用 Core/Data；xUnit 2.9.3、runner.visualstudio 4.0.1、Test.Sdk 18.10.1；既有 VSTest/TRX 流程 |
| YanJi.DesignLab | 隔离视觉样板，不是正式知识管理模块 |

[事实] global.json SDK 10.0.401 / latestFeature；Directory.Build.props Version=0.3.1、nullable、确定性构建和依赖锁文件。中央版本见 Directory.Packages.props。没有 EF Core、WPF UI、LiveCharts/ScottPlot 等产品依赖；图表是原生自绘 StudyChart。M0 不增加依赖。

## 3. 存储结构（Schema 定义，非真实用户库扫描）

[事实] `YanJiDatabase.SchemaVersion=1`、application_id=0x594A4931、文件名 yanji.sqlite。所有以下结构来自 SchemaSql；本轮不声称已检查用户库的实际版本或数据行数。

| 表 | 现有字段/约束与关联 |
|---|---|
| subjects | id PK、name、archived 0/1；没有名字唯一约束 |
| chapters | id PK、subject FK subjects、name、progress 0..100、mastery 0..3、archived 0/1；没有 parent/type/order/profile |
| sessions | id PK、subject FK、chapter 可空 FK chapters、source 0/1、note、version>0、deleted 0/1、created |
| parts | id PK、session FK sessions ON DELETE CASCADE、date、duration>0、zone、kind、start/end；纯时长没有起止；区间 duration=end-start；date/interval 索引 |
| revisions | id PK、kind/entity/version/at/reason/before_json/after_json；entity+at 索引；不是跨实体 FK |
| active_timer | slot PK CHECK slot=1、payload JSON；最多一个检查点槽位 |
| tasks、weakpoints | id PK、payload JSON；科目/章节引用靠应用与备份语义检查，非 SQL FK |
| reflections | id PK、payload JSON；按日期保存感想 |
| settings | id PK、payload JSON；普通偏好、focus-session:*、period-review:v1:* 共用 |

[事实] 启动已有库时先 CheckIdentity（application_id 与 user_version 必须匹配），再 CheckSchema：在内存构造 SchemaSql，并比较 sqlite_master 的全部表/索引/定义，归一化空白但不放松结构检查。加列/加表也会被旧程序拒绝。当前没有增量迁移框架或迁移历史表。Open 使用 Pooling=false、外键启用、同步 FULL；初始化启用 WAL。不能通过保持 user_version=1 偷加知识表。

## 4. 实体关系与边界

| 实体/类型 | 现有能力 [事实] | V0.4 影响 |
|---|---|---|
| Subject | Id/Name/Archived；默认 math、english、408、politics | 可复用 ID；目前全局科目，没有档案组合/数学一二等变体 |
| Chapter | Id/SubjectId/Name/Progress/Mastery/Archived | 扁平目录；旧 ID 是 session/weakpoint 的关键引用 |
| LearningTask | Id/Title/DueDate/Done/SubjectId? | 无节点、档案、完成时间；Done 是显式当前状态 |
| StudyRecord | Id/SubjectId/ChapterId?/Source/Note/Parts/Version/Deleted/CreatedAt/Focus? | 即 StudySession 的现有代码名；一科目、一主要章节，没有多节点时长分配 |
| StudyPart | 日期、原始毫秒、时区、区间/纯时长 | 统计事实必须保留，不搬迁/重写时长 |
| FocusContext | 模式、目标、休息、TaskId/TaskTitle 快照 | 任务关联持久化，不表示任务已完成 |
| TimerSnapshot | 章节、同一身份、Parts、可信采样、版本、Focus | 复用状态机；未来只增加学习上下文元数据 |
| WeakPoint | SubjectId?/ChapterId?/Open/Reviewing/Verified/ReviewDate | 已有节点前身关联；Verified 是薄弱点验收，不等于所有知识掌握 |
| PeriodReview | 范围/科目/正文/版本/软删除 | 作者输入；不由时长推断成绩 |

[事实] SaveChapter 禁止已有章节跨科目移动。Chapter 没有硬删除 API。SaveSubject/SaveChapter 写入与审计在同一事务，但章节审计 Version=0，没有逐节点乐观版本。History(id) 目前按 entity 查询，没有 kind 参数；新知识节点历史须区分旧 chapter 与新 knowledge-node，防跨实体同 ID 混查。

## 5. 学习流程、统计与可靠性

[事实] TimerMachine.Advance 从 ClockSample 的 awake/boot/UTC/process 计算有效区间；暂停不增量；不连续进入 NeedsReview；跨午夜由 TimeParts.Interval 分日期；限时模式按剩余目标封顶。休息由 NonStudyCountdown 管理，不写学习片段。

[事实] SaveTimer 同事务写 sessions、parts、focus-session 元数据和 active_timer，检查单活动身份、版本、重试等价性。MainViewModel.PersistTimerAsync 保存失败回落到 durable 检查点/待核对。手动补录写同一 StudyRecord，只有日期/科目/时长必填；有明确区间才检测重叠，纯时长不伪造区间。

[事实] SaveRecord 修正、整次软删/恢复和片段排除/恢复都有 revision；最后片段排除被阻止；恢复片段重新校验重叠。活动记录不能经普通编辑入口覆盖。Focus 模式/任务关联不允许在历史修正中被替换。

[事实] DailyTotals SQL 汇总未软删 session 的有效 parts；StudyAnalytics 从 Parts.Date/原始毫秒做日/科目/周/月/章节统计。原始毫秒先聚合再展示整数秒；已确认的 NeedsReview 片段可统计，未知间隙没有片段。BuildAnalytics 的章节排行是直接 ChapterId 分组，没有祖先汇总。OpenRecords 当前章节筛选是 ID 相等，不含后代。

[事实] Progress 与 Mastery 手工维护。完成计时不自动 Done、Progress=100 或 Mastered；任务复盘是当前状态，不是有时间戳的周期完成数。科目/章节名称以当前目录解析，旧学习记录没有记录时目录路径快照。

## 6. 备份与恢复

[事实] BackupManifest Format=1/Schema=1，ZIP 恰好 database.sqlite+manifest.json；SHA-256、身份、完整性、Schema、跨日/重叠、JSON 引用/Focus、检查点与 record 一致性校验。数据库最多 512 MiB、manifest 最多 64 KiB。Inventory 是物理 sessions 行数及所有 parts 的毫秒总和（含软删记录对应片段），不能误换成仪表板有效总时长。

[事实] 使用 SQLite BackupDatabase 做 WAL 安全快照，再生成独立快照、校验、原子移动到目标文件。恢复是完整替换，活动计时阻止恢复；先备份当前库，wal_checkpoint(TRUNCATE)，File.Replace 生成 rollback；打开校验失败尝试回退。RestoreCheckpoint 是测试注入点，不是持久化的崩溃恢复日志。

[待验证] 当前进程终止恰在文件替换前后时的持久恢复机制不完整。源码中 after-replace 测试回调在随后 try 校验块之前；未来复用升级替换流程要把所有替换后异常纳入回退/恢复状态机，而不能仅复制现有 try 块。

## 7. WPF 与可复用组件

[事实] MainWindow 单壳，MainViewModel.cs + Focus/Presentation/Reviews partial。使用 ObservableObject、ObservableProperty、RelayCommand；部分编辑/导航由 code-behind+EditorDialog 调 Data，不是每页独立 VM。不要为了 V0.4 重写所有页。

[事实] 今日、日历、计划、薄弱点、统计、设置、记录明细已有功能。SubjectGroup→ChapterRow 仅两级展示；MainWindow 外层 MainScroll 包含多页 StackPanel，不能据此宣称已具备大树虚拟化。记录 ListBox 有 recycling 与高度限制；树 FocusTreeView 样式无显式虚拟化配置。

可复用：ThemeService/DynamicResource、Styles.xaml（Ink/Muted/Accent、SectionSurface、Primary/QuietButton）、MetricCard、NaturalDuration、FocusIcon、StudyChart（趋势/环形/热力/时间轴）、ConfirmDialog、EditorDialog、记录行/明细抽屉与 OpenRecords 下钻。新大树须独立有限视口，不能简单把 1 万节点塞入现有外层滚动。

## 8. Skill 与测试规范

[事实] 仓库及 D:/、D:/Projects 父级未发现磁盘 AGENTS.md；仍遵循当前对话提供的 AGENTS 指令（默认 Medium、必要时提醒 High）。项目 .agents 中有 run-tests、coverage-analysis、breakdown-feature-prd、winui-ui-testing、yanji-domain-rules 及设计 Skills。

本轮实际使用：显式读取 yanji-domain-rules；读取 brainstorming/writing-plans 组织设计与任务；breakdown-feature-prd 提取范围/验收结构；读取 WPF Copilot 参考文件；UI/UX Pro Max 2.15.0（本地来源固定 7246805992a42b7bba37d4cac83b01416f0c6c44）实际运行 search.py --stack wpf，得到键盘可达、TreeView opt-in 虚拟化、InputBindings 建议。采用原生控件/键盘/有限视口；不照搬 Web CSS、44px 等作为 WPF 硬编码，不重写旧快捷键处理。

[事实] 当前桌面 cwd 在 C 盘任务目录，D 盘项目 Skill 不在本会话自动技能清单。这里是明确文件读取/显式工具执行，不声称自动加载或运行全部设计引擎。Impeccable/其他设计引擎未运行；winapp CLI 的可用性在 M0 未重新验证。已有 dotnet-wpf.instructions.md 是 Copilot 指令参考，不是原生 SKILL。dotnet-test overlay 不存在；测试仍按本项目 xUnit v2/VSTest 决定。M0 没有重新跑 .NET 测试或安装依赖。

## 9. V0.3.1 计时显示证据

[事实] DurationText.Timer 已是 HH:mm:ss；FocusTimer 用可信 DurationMs 派生自由累计/活动限时剩余，结束显示有效时长。主窗和迷你窗绑定同一 TimerText；DispatcherTimer 每秒启动 TickAsync。不是 Tick += 1 秒。

已重新读取：红灯 10 FAIL；中间 39 PASS/2 旧格式断言 FAIL；最终 41 PASS。真实窗口 result=73 个相关断言、30 个不同时间点读 TextBlock、200 次属性通知；每种模式超过 65 秒，暂停、切页、最小化、主/迷你与保存一致。属于历史真实 WPF 证据，命令驱动不等于物理输入。

provenance.Source=0.3.1+40022179；当前保留运行副本 EXE/程序集 SHA 与原 provenance 完全一致。原 artifacts/v031/final-publish 已按批准清理，原路径不存在不等于证据失真。M0 不重新运行 WPF；新增上下文/树/搜索后必须再验收。其他实际 DPI、IME/物理键盘、睡眠/休眠/重启、真实用户库升级仍不据此宣布通过。

## 10. 追溯索引

其他四份文档引用以下 S 编号：

- S01 [Models.cs](D:/Projects/yanji/src/YanJi.Core/Models.cs)：所有当前实体、枚举。
- S02 [TimerMachine.cs](D:/Projects/yanji/src/YanJi.Core/TimerMachine.cs)、[TimeParts.cs](D:/Projects/yanji/src/YanJi.Core/TimeParts.cs)：时间事实/分日/异常。
- S03 [YanJiDatabase.cs](D:/Projects/yanji/src/YanJi.Data/YanJiDatabase.cs:14)：SchemaSql/CheckIdentity/CheckSchema/Open。
- S04 [YanJiDatabase.cs](D:/Projects/yanji/src/YanJi.Data/YanJiDatabase.cs:150)：ValidateRecord/SaveRecord/SaveTimer/ExcludePart/RestorePart。
- S05 [YanJiDatabase.cs](D:/Projects/yanji/src/YanJi.Data/YanJiDatabase.cs:102)：Chapters/SaveChapter/SaveTask/SaveWeakPoint/SaveEntity/History。
- S06 [YanJiDatabase.cs](D:/Projects/yanji/src/YanJi.Data/YanJiDatabase.cs:301)：BackupManifest/CheckAllData/Backup/ValidateBackup/RestoreBackup。
- S07 [MainViewModel.cs](D:/Projects/yanji/src/YanJi.App/MainViewModel.cs)：构造/Initialize/Refresh/Tick/Persist/联动选择器。
- S08 [MainViewModel.Focus.cs](D:/Projects/yanji/src/YanJi.App/MainViewModel.Focus.cs)：UpdateFocus/StartTask/显式完成。
- S09 [MainViewModel.Presentation.cs](D:/Projects/yanji/src/YanJi.App/MainViewModel.Presentation.cs)、[Reviews.cs](D:/Projects/yanji/src/YanJi.App/MainViewModel.Reviews.cs)：树投影/统计/筛选/复盘。
- S10 [MainWindow.xaml](D:/Projects/yanji/src/YanJi.App/MainWindow.xaml:503)、[MainWindow.xaml.cs](D:/Projects/yanji/src/YanJi.App/MainWindow.xaml.cs:107)、[Styles.xaml](D:/Projects/yanji/src/YanJi.App/Styles.xaml)：树/编辑/布局；StudyChart、ThemeService、MetricCard 同目录。
- S11 [MainWindow.xaml.cs](D:/Projects/yanji/src/YanJi.App/MainWindow.xaml.cs:24)、[MiniTimerWindow.xaml](D:/Projects/yanji/src/YanJi.App/MiniTimerWindow.xaml)：显示刷新链。
- S12 [Directory.Packages.props](D:/Projects/yanji/Directory.Packages.props)、global.json、三项目 csproj、tests/YanJi.Tests：依赖与测试。
- S13 [TimerDisplayTests.cs](D:/Projects/yanji/tests/YanJi.Tests/TimerDisplayTests.cs)、artifacts/v031/tests-green-final/green-targeted-final.trx、final-window/{result,samples,provenance}.json：既有证据。
- S14 [V03-SKILLS.md](D:/Projects/yanji/docs/V03-SKILLS.md)、.agents/skills/yanji-domain-rules/SKILL.md、docs/skills/dotnet-wpf.instructions.md、.agents/design-skill-sources.json：指导来源。
- S15 [ARTIFACT-RETENTION.md](D:/Projects/yanji/docs/ARTIFACT-RETENTION.md)：缓存清理后保留规则；不为 M0重建大产物。

M0 隔离证据：`<private-audit>/v04-m0/schema-probe.json`（本地私有证据索引，不随公开仓库提供）、timer-evidence-review.json、wpf-skill-search.txt。6 项 Python SQLite 3.45.3 内存探针为 SIMULATED PASS，验证追加字段保留引用/原始毫秒、旧严格 Schema 失配、旧 positional INSERT 失效、FK 不阻止环、DDL/数据失败回滚；不是 Microsoft.Data.Sqlite 迁移验收。
