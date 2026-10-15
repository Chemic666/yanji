# V0.4 M1–M4 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task **only after a new M1 authorization**. 下列复选框均为未来任务，不是已执行记录。

**Goal:** 在不丢失历史学习事实的前提下，实现考试档案、多级知识目录、纯数据知识包及进度/任务/计时/薄弱点关联。

**Architecture:** 保留 Core/Data/App 模块化单体；扩展 chapters、保留 ID，Schema2 阴影升级；目录与包操作用既有单写事务；学习时长仍唯一来自有效 StudyPart。

**Tech Stack:** 现有 C#、WPF、.NET10、CommunityToolkit.Mvvm、Microsoft.Data.Sqlite、xUnit2/VSTest；暂不增添树控件或 ORM 依赖。

**Spec:** [技术设计](V04-TECHNICAL-DESIGN.md)；[事实与源码索引](V04-CURRENT-ARCHITECTURE.md)；[需求差异](V04-GAP-ANALYSIS.md)；[风险登记](V04-RISK-REGISTER.md)。

## 全局约束与验收方法

- M0 到此停止；M1～M4 未实现、未执行，状态全部 **NOT TESTED**。普通内部决策不设逐步用户批准关卡，但不能把本计划当作开发授权。
- 所有数据库测试使用新建隔离目录与合成 Schema1 数据。不得扫描用户数据目录、自动选取用户库或使用其备份。失败证据保留；不得修改失败结果。
- 每项变更先用最小有意义的失败测试锁定风险，再实现，再跑受影响测试；每个里程碑形成一次证据摘要。发布前跑一次完整核心回归，不重复运行无变更的整套视觉矩阵。
- 测试以断言/夹具/场景为单位，不以“凑足多少项”或覆盖率作为安全结论。模拟时间、隔离进程故障、真实窗口与真实电源实验分开记录。
- 原始时长、暂停排除、跨日、重叠、未知时间核对、修订、软删/恢复、备份全替换规则保留。学习、任务完成、自评掌握、薄弱点验收分别显示。
- 不创建未经核实的官方大纲，不开发 V0.5 错题/模考/AI，不开展六页面视觉重构。

## 依赖与开发顺序

`M1 数据兼容与节点基础 → M2 知识包机制 → M3 工作区与日常联动 → M4 集成验收与交付`。

M1 定义完整 Schema2，包括暂为空的知识包表。M2 若需要改结构，必须显式新迁移，不保持版本号偷偷加表。M1 的图规则与查询接口可供 M3 先做隔离原型；M2、M3 合并前均须通过 M1 安全门槛。每阶段合理独立提交，不改写已有标签/历史。

## M1：兼容边界、档案与知识节点基础

### 1.1 固定基线与合成旧库夹具

- [ ] 核对产品标签 v0.3.1、当前 HEAD 和未提交文件；保留可运行回退 ZIP及来源哈希，遵循 ARTIFACT-RETENTION，不重复保留多份发布解压目录。
- [ ] 创建隔离 Schema1 夹具：四科、归档章节、纯时长记录、跨日计时、多片段及片段排除/恢复、软删记录、Focus 元数据、Task/WeakPoint/Review JSON、修订、每日感想、设置。另建暂停/待核对活动检查点夹具。
- [ ] 定义旧事实指纹：sessions/parts 原字段、Chapter旧6字段、settings/revisions原 JSON；区分有效统计和物理库存。对异常旧引用必须拒绝升级，不“修复”为新虚假关系。

**涉及文件（未来）：** 新建 `tests/YanJi.Tests/Fixtures/LegacySchemaFixture.cs`、`MigrationTests.cs`；读取现有 `YanJiDatabase.cs`、`Models.cs`、备份测试与 V0.3.1 证据，不改历史材料。

### 1.2 Core 模型与图规则

- [ ] 新增 ExamProfile/KnowledgeNode/NodeKind/LearningContext，保留 Chapter 兼容入口。旧 Chapter.Id 原样作为新节点 ID。
- [ ] 图规则覆盖：根/多层、同科目同档案、父存在、环/自己/后代移动、深度限制、兄弟排序、归档与软删继承、恢复路径；所有持久化删除仅软删。
- [ ] 明确四种语义及当前树统计口径；自评父进度不由子任务/时长自动写入，WeakPoint.Verified 不等于整个节点验收通过。

**涉及文件（未来）：** 新建 Core/Knowledge 相关类型与 `KnowledgeTreeTests.cs`；修改 `Models.cs` 的兼容可选上下文。对构造/序列化/API使用点先列全再改。

### 1.3 版本感知存储与安全升级

- [ ] 分拆 YanJiDatabase 为 partial，保持同一 `_gate`/事务辅助；SaveChapter 改显式列及保留新字段；增加带版本令牌的节点写入与 scope.catalog_version。
- [ ] 固定完整 Schema2、迁移摘要及严格结构校验；新建与升级库用同一重放路径，验证 sqlite_master 定义一致。
- [ ] 升级 Bootstrap 位于普通 VM/自动备份之前；验证旧库、阻止活动检查点、备份、阴影迁移、事实指纹、WAL 检查点、文件提升与阶段恢复。未知结构/占用/磁盘失败保持原库可恢复。
- [ ] 通过注入失败及隔离子进程异常退出覆盖各阶段；所有替换后异常均进入回退保护。不能仅用回调异常代表进程崩溃或真实断电。
- [ ] 扩展 Format2 备份与 CheckAllData2，兼容 Format1 候选升级后完整恢复；未知版本拒绝。旧 v0.3.1 二进制必须拒开 Schema2 且未写入。

**涉及文件（未来）：** 现有 Data `YanJiDatabase.cs`、App `App.xaml.cs` / 启动边界；新增 Data Schema/Migrations/Knowledge/Context 分文件、App 升级协调器；测试 `MigrationTests.cs`、`BackupCompatibilityTests.cs`。实际文件命名须沿用项目风格，不创建第二个数据库事务入口。

**M1 必过：** 零旧事实变化、全部旧引用保留、新建/升级结构一致、失败可恢复、旧程序拒开2、旧备份可迁移恢复。任何失败阻止 M2/M3正式集成。数据真实性不靠页面总数字相同代替逐字段核对。

## M2：纯数据知识包、预览、更新与范围回滚

- [ ] 实现尺寸/格式/字段/版本/来源许可/稳定ID/父图/范围验证；文档演示包作为测试样本，不冒充内置大纲。
- [ ] 实现显式科目映射、本地 ID 分配、幂等导入与同版本不同摘要拒绝；跨档案同包分离节点，共享包快照。
- [ ] 建立 B/L/U 三方更新计划：改名、描述、移动、排序、新增、上游删、本地删、解绑和本地子节点。学习状态/事实不参与覆盖；合并后的最终图再校验。
- [ ] 预览保存 scope/安装版本和摘要；应用前再验证陈旧计划、引用、活动计时、可用备份；节点/绑定/版本/审计/operation 单事务。
- [ ] 实现包范围逆向回滚与冲突预览；保留包更新后新增的学习、任务、薄弱点和用户修改。被使用的新增节点保留为本地节点，不以整库恢复冒充包回滚。
- [ ] 新增纯数据导入/预览/冲突对话框，取消不写库，逐项和安全批量操作有明确影响说明；许可证不明的内容不得随产品发行。

**涉及文件（未来）：** Core/Knowledge 包 DTO/Validator/MergePlanner；Data KnowledgePackages 分文件；App PackageImportViewModel/对话框；`KnowledgePackageValidationTests.cs`、`KnowledgePackageMergeTests.cs`、`KnowledgePackageTransactionTests.cs`。

**M2 必过：** 重复导入零新写、故障零半成品、三方冲突不静默覆盖、失效预览拒绝、包回滚不丢后续学习、跨档案不串联、导入无脚本/网络依赖。Schema1恢复是全库替换，与包回滚分别测试。

## M3：知识工作区、选择器与学习闭环

- [ ] “计划与进度”改为有界目录/详情双栏；900×600切单栏返回；原生树默认折叠、虚拟化/复用、后台搜索/取消/定位。KnowledgeBusy 不遮断主计时 Tick。
- [ ] 档案/科目组合/节点详情支持显式新增、编辑、移动、排序、归档、软删与恢复；结构动作显示影响范围，历史仍能访问旧ID。
- [ ] 搜索式 NodePicker 复用计时、补录、任务、薄弱点编辑；科目切换清理非法节点，空状态有管理入口，未指定保持可选，不增加日常必填步骤。
- [ ] Task一键开始捕获档案/节点，TimerSnapshot、StudyRecord、SaveTimer、durable恢复、Completed幂等、备份校验贯通 Context。旧编辑器不能丢新增字段。
- [ ] 详情下钻到原记录，传递日期/档案/科目/节点范围/记录状态；自身与子树时长分开，去重后聚合原始毫秒；编辑/删除/恢复统一刷新。
- [ ] 保留 Quiet Focus 两主题、自然时长与原生确认/历史组件；进度、任务、自评掌握、薄弱点验收标签明确，旧路径未知如实说明。

**涉及文件（未来）：** App MainViewModel及Focus/Presentation相关使用点、MainWindow 编辑器、ProgressView、公共选择器；新 KnowledgeWorkspaceViewModel/NodePickerViewModel/节点详情；Data 查询与Context读取；`KnowledgeLinkTests.cs`、`KnowledgeAggregationTests.cs`、`FocusContextCompatibilityTests.cs`。

**M3 必过：** 父30分+子20分→科目50分；多选祖先/后代不叠加；跨日按part.Date；改名/移动/归档不改科目/每日事实；任务开始后目标修改不改变进行中归属；旧/新 Context修订恢复与备份一致。活动计时期间大树搜索不出现显示冻结。

## M4：最小充分回归、真实 WPF 与发布

### 自动化集成

- [ ] 核心回归一次：原始计时/暂停/异常gap/跨日/已知重叠/纯时长/软删修订/片段排除恢复/备份，以及 M1–M3新增风险测试。
- [ ] 隔离往返：Schema1夹具→Schema2→操作知识包和学习→重启→Format2备份/恢复→事实及关系对账；旧Format1恢复独立验证，不合并。
- [ ] 故障点只针对新关键事务/文件提升：锁占用、备份/写入失败、陈旧预览、注入错误、隔离子进程异常退出；不主动改系统时间/电源、不影响用户程序。

### 真实 WPF 场景矩阵（全部待执行）

| 场景 | 验证与证据 | 通过条件 |
|---|---|---|
| 1200×800 / 900×600，浅/深 | 知识页有数据/空数据，长中文路径与详情；真实窗口截图 | 可滚动与返回，主要操作可见，无遮挡/截断/错位 |
| 真实 DPI | 当前实际 DPI记值；可用的100/125/150%设备/环境 | 不以设置Width假充DPI，未具备的倍率NOT TESTED |
| 键盘/中文输入 | Tab、方向、F2、Enter、Escape、移动/排序无拖拽；中文IME实际输入 | 焦点可见、输入不中断，不误触计时快捷键 |
| 包预览/冲突/取消 | 更新用户改名与本地子节点，验证影响与取消 | 无误导默认项，无半完成，冲突可解释 |
| 联动下钻 | Task→计时→结束→节点记录；改/删/恢复后详情/统计 | 条件正确传递、即时刷新、事实对账 |
| 计时器P1防复发 | 各模式采样1/2/3秒及>65秒；主/迷你、暂停3秒后继续；页面切换/最小化恢复 | 时间来自可信源，HH:mm:ss秒级更新；大树操作中也采样，不只测首尾 |
| 1000/10000节点 | 冷载、搜索p95、展开/选择、UI停顿；记录机器、构建、样本形状 | 满足设计待验证预算，未达标先修测量/布局再决定替代控件 |
| 发布与离线 | 自包含win-x64在隔离数据目录启动，版本/二进制hash/重启/备份核查 | 无开发运行时依赖，正式用户库完全未访问 |

P1计时回归在最终版本执行一次有代表性的真实采样；只有新增缺陷/代码修改影响它时重跑，不重复整批旧截图。真实休眠/重启/断电必须另获授权，否则明确NOT TESTED，不能由模拟通过替代。

### 命令、证据与交付纪律

以下为未来示例（测试类尚未创建，M0未执行）：

```powershell
& .\tools\Invoke-Build.ps1 -Arguments @('test','tests/YanJi.Tests/YanJi.Tests.csproj','-c','Release','--filter','FullyQualifiedName~KnowledgeTreeTests','--logger','trx;LogFileName=m1-tree.trx','--results-directory','artifacts/v04/m1-tests')
```

当前 bin/obj 经过治理，不默认使用 --no-restore/--no-build。沿用 xUnit2/VSTest；不因测试 Skill 的通用建议强行切 MTP 或安装覆盖工具。所有输出位于隔离 artifacts/v04，保留 TRX、失败日志、实际窗口文本采样、必要截图、版本/环境/哈希。

- [ ] 更新设计/使用/升级/回退文档；发布包、源码、证据包分离，不夹带用户DB、WAL/SHM、缓存或未核实内容。
- [ ] 从最终ZIP重新计算文件哈希与截图清单，校验 comparison/索引/来源对应最终代码和程序；按保留规则仅留必要ZIP和证据。
- [ ] 报告 PASS / FAIL / NOT TESTED；明确已验证本机条件，不声称所有Windows设备/电源场景通过。发布不等于自动启动V0.5。

## M0已完成的证据边界

本轮仅源码与历史证据审查、6项Python内存SQLite探针和WPF指导查询。没有运行新.NET测试、WPF窗口、Schema2迁移或知识包引擎。V0.3.1的41项历史绿色测试及30次真实窗口采样属于历史基线，不是V0.4验收结果。
