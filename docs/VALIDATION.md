# 研迹 YanJi V0.1 验证与交付报告

日期：2026-10-13（Asia/Shanghai）。结论：已交付本机可运行 V0.1，核心自动化与隔离故障验证通过；真实电源、原生输入法、实际断网和干净 Windows 环境仍有未测项，不能宣称满足全部正式发布验收条件。

## 需求事实、技术决策与假设

需求事实按最新全流程授权执行：Windows 11 x64、本地离线、2028 入学、六模块、统一学习记录、未知时间待核对、历史修正、完整备份恢复。原计划逐阶段审批已被该授权替代。

技术决策：WPF/MVVM + Core/Data/App 分层，SQLite WAL/FULL/事务/约束，简单串行写入和必要去重；历史日期冻结，时区修改只影响新记录；约 15 秒可配置检查点；便携自包含 ZIP。原因见 ARCHITECTURE.md。

未经实测的假设：普通本地磁盘可写、用户不同时外部修改库；真实电源与所有目标机器表现仍需验收；日常记录 1～3 分钟是目标，尚未测用户操作耗时。无已批准资源硬预算。

## 实际环境与依赖

Windows 11 25H2 / build 26200.9457，进程 OS API 10.0.26200.0，x64，8 个逻辑处理器，PowerShell 7.6.5。注册表 ProductName 仍为 Windows 10 Home China 的旧标签。CPU 型号/物理内存未通过当前受限查询取得，不填假值。

任务内便携 SDK 10.0.401（官方来源及 SHA-512 已核验）、运行时 10.0.12、MSBuild 18.9.11。未做系统级安装、全局 PATH/注册表修改。原共享环境仅检出 .NET 6.0.25、无 SDK。

CommunityToolkit.Mvvm 8.4.2；Microsoft.Data.Sqlite 10.0.12；SQLitePCLRaw 2.1.12；实际 SQLite 3.53.3。测试 SDK 18.10.1，xunit 2.9.3，runner 4.0.1。精确版本及哈希记录于锁文件。

发布进程实际 RuntimeDirectory 为 `D:\Projects\yanji\releases\YanJi-v0.1.0-win-x64\`；DOTNET_ROOT 指向无共享运行时目录。真实时钟来源：precise unbiased / precise interrupt。SQLite 实际返回 wal / foreign_keys=1 / synchronous=2(FULL)。当前窗口实际 DPI=192（200%）。

## 执行结果与证据

- Release 全解决方案构建及自包含发布：0 warning / 0 error。
- 核心与隔离 SQLite：44 PASS、0 FAIL、0 skipped；TRX `tests/delivery-final.trx`。
- 最终发布程序 WPF/命令/窗口/编辑器集成：25 PASS、0 FAIL；`desktop/smoke/results.json`。真实进程和 WPF 焦点测试不等于物理键盘/IME 全流程。
- 自有进程 WAL 崩溃：待写 WAL 8272 bytes，已提交保留，未提交回滚，计时仅恢复 15000ms 并进入 NeedsReview，integrity=ok。
- 自有进程恢复置换前/后终止：分别保留有效旧库 600000ms / 新库 300000ms，先前完整备份均有效。这不是硬件断电或 File.Replace 内部指令中途断电测试。
- 六页面、最小窗口和缩放截图随证据包；已目视核查首页、日历和最小窗口。125/150/200% 是位图渲染，未切换系统 DPI。
- ZIP 解压后启动及文件哈希复核见交付清单/`package-validation.json`，与当前报告的文件夹验证分别留证。

证据包只包含选定日志、TRX、JSON、CSV 和界面截图，不包含实验数据库、缓存、个人数据或含完整环境变量的诊断日志。源码提交、发布文件数/大小/哈希见 delivery-manifest.json。

## 原阶段 1 的 41 项矩阵

严格按原场景完整性归类：27 PASS、14 NOT TESTED、0 当前 FAIL。部分通过的混合场景保留 NOT TESTED，并列出实际通过范围。未将旧计划或模拟试验改写成真实场景通过。

| ID | 状态 | 类型 | 证据及边界 |
|---|---|---|---|
| TIME-01 | PASS | SIM/AUTO | 暂停不计时，恢复后累计正确 |
| TIME-02 | PASS | SIM/AUTO | 重复暂停/结束无重复时长 |
| TIME-03 | PASS | SIM/AUTO | 跳时、连续计数倒退、身份变化冻结未知区间 |
| TIME-04 | PASS | SIM/AUTO/PROCESS | Running 重开待核对，仅保留已确认前缀 |
| TIME-05 | PASS | AUTO | Paused 重开仍暂停 |
| TIME-06 | PASS | SIM/AUTO | 跨午夜分日；另测夏令时 23/25 小时日 |
| TIME-07 | PASS | SIM/AUTO | 延迟、重复事件、计数变化不产生负数/重复时长 |
| DB-01 | PASS | SQLITE/REAL PROCESS | WAL、FK=1、synchronous=2(FULL) 实际值 |
| DB-02 | PASS | SQLITE | 事务第二步失败全部回滚 |
| DB-03 | PASS | SQLITE | 非法时长/引用约束拒绝 |
| DB-04 | PASS | SQLITE | 重复提交和快照重试去重，过期写入拒绝 |
| DB-05 | PASS | OWNED PROCESS | 有待写 WAL 的自有子进程终止，未提交回滚 |
| DB-06 | PASS | OWNED PROCESS | 已提交存活，完整性/引用校验通过 |
| DB-07 | PASS | SQLITE | 只读与页数上限失败，旧库完整；退出保存失败保持打开 |
| BACKUP-01 | PASS | SQLITE | 开放连接及待写 WAL 的在线完整快照 |
| BACKUP-02 | PASS | SQLITE NEGATIVE | 只拷主库不能作为可靠备份，负例实测 |
| BACKUP-03 | PASS | SQLITE | 完整性/引用/结构/内容/清单/哈希校验 |
| RESTORE-01 | PASS | SQLITE | 完整置换并保留当前完整备份及旧库副本 |
| RESTORE-02 | PASS | SQLITE NEGATIVE | 损坏/不兼容/结构异常拒绝，原库不变 |
| RESTORE-03 | PASS | OWNED PROCESS | 置换前后自有进程终止，旧/新库及先前备份有效 |
| RESTORE-04 | PASS | AUTO | 恢复活动计时不补算备份后未知时间 |
| UI-01 | NOT TESTED | PARTIAL PASS | 中文 WPF 显示/文本赋值及持久化 PASS；真实 IME 候选/组合未测 |
| UI-02 | NOT TESTED | PARTIAL PASS | WPF 实际焦点/遍历/默认保存 PASS；物理键盘完整计时流程未测 |
| WIN-01 | PASS | REAL PROCESS | 同路径同 Windows 会话第二进程激活首实例后退出 |
| WIN-02 | NOT TESTED | PARTIAL PASS | 运行时 X 隐藏且继续 PASS；暂停时 X 的独立完整场景未测 |
| WIN-03 | NOT TESTED | PARTIAL PASS | 程序化重显/托盘实现及退出准备有证据；物理托盘菜单和三选项未全测 |
| WIN-04 | NOT TESTED | PARTIAL PASS | 暂停持久化/结束去重/保存失败不退出 PASS；全选项手工流程未测 |
| DPI-01 | PASS | WPF/RENDER | 本机 200% 下 1200×800/900×600 逻辑布局，已查看截图 |
| DPI-02 | NOT TESTED | PARTIAL PASS | 实际 192 DPI；125/150/200% 位图 RENDER PASS；系统切换/多显示器未测 |
| REAL-LOCK | NOT TESTED | REAL | 未主动锁屏；不将代码或模拟视为真实通过 |
| REAL-S0 | NOT TESTED | REAL | 硬件支持，未主动进入 Modern Standby |
| REAL-HIBERNATE | NOT TESTED | REAL | 硬件支持，未主动休眠 |
| REAL-CLOCK | NOT TESTED | REAL | 未修改系统时间 |
| REAL-S3 | NOT TESTED | UNSUPPORTED | 本机不支持 S3，不用 S0 替代 |
| PUB-01 | PASS | REAL PROCESS | win-x64 自包含程序运行，运行时来自发布目录 |
| PUB-02 | PASS | REAL PROCESS | 未安装共享 .NET 10，且将 DOTNET_ROOT 指向无运行时目录仍实际运行 |
| PUB-03 | NOT TESTED | REAL | 未断开网络；离线设计及无上传代码不替代实际断网试验 |
| PUB-04 | NOT TESTED | REAL | 无干净 Windows 11 机器/VM 验收 |
| INSTALL-01 | NOT TESTED | OUT OF DELIVERY | 交付便携 ZIP；未提供/安装/卸载安装器 |
| PERF-01 | PASS | REAL PROCESS | 一次新库及三次同库启动、独立空闲/计时采样；大小见交付清单 |
| PERF-02 | PASS | REAL PROCESS | 2/15/60 秒各 >=60 秒；实际保存数及端到端保存耗时 |

## 启动与资源实测

同一机器的新进程：第 1 次新实验库，第 2～4 次同库；没有重启清空系统缓存，不称“冷启动”。UiReady 从 App 初始化计时开始到布局就绪；WholeProcess 包括启动及验证进程退出。

| 次数 | UiReady ms | WholeProcess ms | 结果 |
|---|---:|---:|---|
| 1 | 1899.2 | 2252.5 | PASS |
| 2 | 1599.7 | 1893 | PASS |
| 3 | 1849.7 | 2165.6 | PASS |
| 4 | 1622.1 | 1917 | PASS |

另起一个无截图的自包含进程，依次采样空闲、2/15/60 秒检查点，各至少 60 秒，60 秒项约 62 秒。CPU 单核占比 = 进程 CPU 秒/墙钟秒；整机占比再除 8。内存为各采样期间观察峰值，单位 MiB。

| 模式 / 检查点 | 采样秒 | 成功检查点数 | 平均保存 ms | CPU 单核% / 整机% | 工作集 / 私有 MiB |
|---|---:|---:|---:|---:|---:|
| idle / 0s | 60.69 | 0 | 0 | 3.76 / 0.47 | 220.5 / 140.8 |
| timer / 2s | 60.71 | 20 | 58.83 | 27.85 / 3.48 | 235.8 / 134.6 |
| timer / 15s | 60.52 | 3 | 56.56 | 2.27 / 0.28 | 233.8 / 132.4 |
| timer / 60s | 62.52 | 1 | 20.03 | 2.25 / 0.28 | 233.8 / 132.2 |

保存耗时为排队、数据库事务和返回界面的端到端 Stopwatch 耗时，未独立剥离 SQLite 提交延迟。统计不含开始/结束写入。短连接可能在瞬间移除 WAL，采样读不到不表示没有写入。15 秒是折中默认值，调度受系统负载影响，不能保证硬实时尾部上限。截图测试的额外内存不当作空闲值。

## 已修复与中断记录

已复现并修复：只读 WPF 属性双向绑定异常；Windows precise 接口导出位置；刷新导致章节选择/未存感想丢失；退出持久化失败仍关闭的风险；过期计时快照覆盖修正；备份缺表/结构/非法内容；非公历区域日期；坏 JSON 日备份阻止新快照；性能工具读取已删除 WAL 的竞态。

坏 JSON 回归 RED：1 FAIL / 43 PASS；修复后 GREEN 和最终回归均 44 PASS。早期失败日志不是当前未解决 FAIL。最终性能第一次尝试在读 WAL 时失败，只得到空闲片段，不能算全采样通过；修复后重新完整采样，采用本报告数据。

通信/权限切换中断后依据 Git、文件及测试证据恢复；早期取消的测试没有完成证据，记执行中断，不能推断通过。未重建原材料，未终止用户或无关进程。本轮所有自有验证进程有完成/退出证据；受工具可见性限制，不声称盘点了全系统所有进程。

## 未解决限制与正式发布条件

实际锁屏、S0、休眠、改时钟、物理断网、干净机、真实 IME、手工完整键盘/托盘退出流程、多显示器 DPI 尚未覆盖；S3 本机不支持。未签名、无安装器/自动更新/备份自动清理，无云同步/AI/复杂错题/模考。跨用户会话、目录别名、多用户管理不在已验收范围。超 512 MiB 备份校验被拒绝，数据未加密。

核心计时、修改后统计、事务、完整备份恢复、持久化及本机启动满足现有自动验收；可进入本机日常使用及进一步设备验收。需完成上述重要真实环境验证后，才能宣布完整正式发布验收通过。没有为了未授权电源实验中断其他开发。

使用及备份恢复步骤见 USER-GUIDE.md。V0.2 可优先补齐真实设备验收、键盘流程、暗色主题、统计图和备份保留策略。
