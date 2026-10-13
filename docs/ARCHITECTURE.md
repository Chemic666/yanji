# 架构、数据与决策

## 已确认的需求事实

Windows 11 x64，离线单用户，2028 年入学；计时与补录共享 StudyRecord/StudyPart（业务上的 StudySession）；一次一个科目、可选主章节；最多一个活动计时；暂停不计时；未知间隙需核对；已知区间禁止重叠；日期分配不打散一次学习身份；修改后统计一致；归档、软删除、历史及完整恢复。

## 实现边界

| 模块 | 责任 | 不承担 |
|---|---|---|
| YanJi.Core | 不依赖界面的模型、计时状态机、日界分配、半开区间重叠 | Windows API、数据库、界面 |
| YanJi.Data | SQLite 事务/约束、当前记录、修订、统计查询、备份恢复、CSV | 页面与实时刷新 |
| YanJi.App | WPF/MVVM、输入与命令编排、Windows 时钟/消息、托盘/单实例 | 另造统计事实 |
| YanJi.Tests | 可控时钟、真实隔离库与负例 | 真实电源验收 |
| YanJi.Probe | 自有 Windows 子进程崩溃与恢复中断证据 | 用户库、系统干预 |

六个产品模块共享这些层，使用模块化单体。普通数据写入由简单锁串行化；界面用 Busy 和后台任务避免同时发起写命令；没有泛化命令总线、服务器或分布式锁。

## 当前事实与一致性

SQLite 表：subjects、chapters、sessions、parts、revisions、active_timer、tasks、weakpoints、reflections、settings。sessions 只存元信息；有效时长和日期来源仅是 parts。日、周、月和科目统计查询未软删除记录的有效片段，不维护另一个可修改的总计表。

StudyPart 有 `DurationOnly` 和 `Interval` 两种；前者不含时间区间，后者开始/结束成对、正时长、半开区间，并按时区午夜分割。来源 Manual/Timer 与片段类型独立。存储日期固定使用公历 ISO 格式，避免 Windows 日历设置改变日期事实。

记录新增/修改/软删除/恢复与修订在同一事务中；版本检查阻止过期修改。已知区间冲突在事务中检查；软删除不计入冲突，恢复重新检查。必要新增提交和相同计时快照重试去重。活动计时检查点带版本，不能覆盖已结束/已修正记录。只有一个 active_timer 槽位，Windows 同数据目录单实例再提供界面层防护。

章节 progress 与 mastery 独立存储。任务计划日期和每日学习片段分别建模。引用实体归档，保留外键。感想、任务、薄弱点、设置也保留修订；V0.1 界面重点提供学习记录历史。

## 技术决策及理由

- .NET 10 / WPF：已批准的 Windows 原生方向。版本由实际工具链和锁文件记录。
- Microsoft.Data.Sqlite，WAL、foreign_keys=ON、synchronous=FULL，事务、非池化短连接：让故障回滚和恢复关闭文件边界清晰；备份使用 BackupDatabase，一致快照，不单拷主库。
- Windows 采样比较 awake、boot、UTC 与进程身份：学习增量按不包含休眠的连续 awake 时间计；明显的睡眠/跳时/身份变化冻结未知间隙。优先动态加载 precise 接口，兼容非 precise unbiased/GetTickCount64 后备；本机实际使用 precise。
- 2 秒 UTC 差和 1 秒 boot/awake 差为采样异常阈值，容纳正常采样抖动；不是物理电源行为已验证的证明。
- 暂选约 15 秒检查点：60 秒采样显示 2 秒模式明显更频繁写入，60 秒模式尾部风险更长；保持可配置，不宣称硬实时调度。
- 程序重启只恢复成功检查点；Running 转 NeedsReview，Paused 保持 Paused。显式核对只添加指定日期的纯时长，然后保持暂停。
- 备份先写自己创建的 partial 文件、刷新、验证，再发布新文件名；完整恢复先全量验证、备份当前库、关闭连接/清理自身检查点，再 File.Replace，保留回退副本。没有合并恢复。
- 自包含目录与 ZIP 交付：无需修改共享开发环境或管理员安装；不做裁剪/AOT，不提供安装器、签名或自动更新。

## 假设与仍需验证

SQLite 位于普通本机可写目录，用户不会同时用外部工具修改同一库；不支持网络共享文件夹并发写入。单实例作用于当前 Windows 会话与相同规范路径，跨用户/会话及路径别名未验收。

真实锁屏/Modern Standby/休眠/时钟调整、物理断网、其他 Windows 安装、原生中文输入法与跨显示器 DPI 不以模拟结果代替。用户日常记录 1～3 分钟是产品目标，尚未做用户操作时间测量。没有约定资源硬门槛，不捏造“性能预算达标”。

## 官方依据

- [Microsoft.Data.Sqlite 备份](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/backup)
- [SQLite WAL](https://www.sqlite.org/wal.html)、[PRAGMA](https://www.sqlite.org/pragma.html)
- [不包含休眠的连续时间接口](https://learn.microsoft.com/en-us/windows/win32/api/realtimeapiset/nf-realtimeapiset-queryunbiasedinterrupttimeprecise)
- [.NET 10 官方发布元数据](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json)
