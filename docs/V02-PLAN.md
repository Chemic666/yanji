# 研迹 V0.2 实施计划

> 执行方式：superpowers:executing-plans，当前会话逐任务实现，最后一次独立分支审查；用户已授权自主设计与实现，不设置逐阶段审批。

**Goal:** 将确认的Quiet Focus方向落实到六个正式模块及记录明细，交付真实WPF程序、Skills证据和视觉/回归报告。

**Architecture:** 共用语义资源与控件模板，替换视图组合；复用MainViewModel、StudyAnalytics及数据服务。只增纯展示投影/格式与原生视觉验证，不改schema、计时事实或备份事务。

**Tech Stack:** C#、WPF、.NET10；原有CommunityToolkit.Mvvm、Microsoft.Data.Sqlite；不增加运行依赖。

**Spec:** docs/V02-DESIGN.md

## Global Constraints

- Windows11 x64，900×600最小；浅深主题；使用原生XAML和矢量。
- 计时/补录、已知区间、跨日、待核对、软删除/片段修订及完整恢复规则不变。
- 旧版本与标签保留；使用独立分支和owned测试库；不修改系统DPI/电源/时间。
- 普通设计无需人工批准；没有公开发布、全球配置或管理员安装。

## Review Focus

- 多片段跨日记录：展示本日贡献和整次，修改/删除不丢原筛选；既有数据一致性测试。
- 深色/小窗/长中文：标签、选择器、按钮和操作可见，不能靠编译推定；实际捕获。
- Modal浮层：全局快捷键不丢输入，Tab不落到底层，关闭恢复焦点；运行断言。
- 空/单点/极值图表：不伪造时间或轴、精确下钻；原始毫秒断言。
- 用户库与升级：自动验收拒绝正式路径；旧发布夹具打开事实不变、完整备份实际恢复。

### Task 1：恢复核查与Skills

- [x] 核对干净Git、v0.1.1及视觉标签，创建codex/v0.2-quiet-focus。
- [x] 固定六源提交、审查许可证/Skill内容和可执行入口；官方安装器项目级安装优先Skills。
- [x] 保存来源/安装/哈希/加载证据；实际WPF检索并记录采纳/拒绝；Codex发现验证。
- [x] 原业务基线：59 PASS，artifacts/v02-official-baseline-tests/v02-baseline.trx。

### Task 2：设计系统与展示契约

Files：Styles.xaml、ThemeService.cs、PresentationConverters.cs、MainViewModel展示属性、纯时长展示辅助及测试。

- [x] 先写自然紧凑时长、≈与精度边界测试，确认RED；补最小纯展示实现，完整套件GREEN。
- [x] 落实语义颜色/字号/原生图标与输入模板，所有交互状态；不动统计聚合。
- [x] 新展示投影只消费有效原始毫秒，精确原文保留在Tooltip；记录修订服务不变。

### Task 3：正式视图与交互

Files：MainWindow.xaml、MainWindow.Presentation.cs、StudyChart.cs、EditorDialog.cs、ConfirmDialog.cs。

- [x] 优先今日、历程、明细、统计；再计划、薄弱点、设置。
- [x] 保留现有handler、命令、named control和测试入口；响应布局重排，新增浮层隔离及状态反馈。
- [x] 增强图表真实刻度、提示、命中与中文，保持StudyAnalytics事实。
- [x] 原63项WPF回归，新增真实窗口捕获/状态/键盘/图表断言；第一次批量目视。

### Task 4：收敛与可靠性

Files：V02VisualRunner及隔离调用工具、测试证据。

- [x] 集中修复首次截图和运行问题；第二轮确认，记录PASS/FAIL/NOT TESTED。
- [x] 运行核心完整回归、旧V0.1.1数据库兼容、备份校验/恢复、重开持久化。
- [x] 真实200%DPI；其他DPI只渲染，不修改系统。物理IME与系统电源实验留NOT TESTED。
- [ ] 保留性能样本并给出实际指标，不把截图过程当空闲性能。

### Task 5：独立审查与交付

- [ ] 一次独立整个分支审查，修复重要问题并相应回归。
- [ ] .NET自包含win-x64新目录，实际运行、ZIP逐文件哈希、源码恢复标签。
- [ ] Skill实际应用记录、更新规范、七视图深浅截图、新旧比较、使用/升级/验证说明与限制。

进度/裁决/失败保留在V02-LEDGER.md。自动化通过来自本轮文件，不引用旧计数充当本轮执行。
