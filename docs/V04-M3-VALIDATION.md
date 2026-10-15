# M3 验证与限制

2026-10-15；范围为合成数据隔离验收，不是正式用户升级或M4发行许可。

## 最终结果

| 检查 | 结果 | 原始证据（artifacts/v04-m3 下） |
|---|---|---|
| Core/Data全部回归 | PASS，185/185，0失败、0跳过 | regression-final/regression-final.trx |
| 独立审查问题复现 | 3 FAIL / 1 PASS，已保留 | review-red/checks.json |
| 相同审查流程修正复验 | PASS，4/4 | review-green/checks.json |
| 最终自包含真实WPF | PASS，31/31 | window-accepted/checks.json、result.json |
| 真实连续计时 | PASS，三模式各3次主/迷你文本采样 | window-accepted/timer-samples.json |
| 最终窗口 | 10张HWND PNG，双主题、两尺寸、200% DPI | window-accepted/screenshots.json |
| 保留M2程序拒绝Schema4 | PASS，退出1，数据库SHA256前后一致 | old-version-guard.json、old-version-probe/startup-failure.txt |
| 自包含程序实际关闭/重开 | PASS，5条记录/8片段/142351ms及上下文、任务、弱点、修订摘要均相同；使用随包运行时 | actual-reopen-before.json、actual-reopen-result.json、reopen-probe/startup.json |
| Schema3→4及失败回滚 | PASS；原事实/章节/知识包表指纹对照 | migration/migration.trx；regression-final-data内schema3-upgrade/schema3-fault.json |
| 构建/自包含发布 | PASS，0警告0错误；直接运行自包含EXE | 最终Assembly及SHA见window-accepted/provenance.json |

185项回归在源码8151da5的业务实现上执行；最终源码2ebe0b9相对仅增加截图尺寸/焦点验收和M3版本提示，Core/Data/业务ViewModel/测试未变，未为这些变更重复全套测试。最终自包含原生验收对应 **2ebe0b9308678820c168957446954b039ed848c6**，程序集SHA256 **FA23A052DE80BFD4424F5B49F0AFF733BA5A09CD181B403A0360EEB2419BB345**。文档提交可晚于代码提交，发布清单分别记录，不能混作同一提交。

## 覆盖与口径

| 重要场景 | 验证方式 |
|---|---|
| 从节点开始自由学习 | 真实WPF按钮触发，稳定节点与上下文检查 |
| 从任务开始自由/倒计时/番茄 | 原生WPF绑定命令；真实计时采样；目标自动完成及上下文落盘 |
| 休息与暂停不计入 | 三模式各真实暂停3.1秒；番茄完成后真实休息3.1秒，事实未增长；可控时钟覆盖长暂停 |
| 异常恢复 | 文件SQLite关闭/重开，运行快照恢复为待核对；未知间隙不计入。真实崩溃/断电未做 |
| 任务和节点共同关联 | LearningContextTests、LearningWorkflowTests，时长仅来自parts；同一次学习不因多关联重复累计 |
| 节点改名、移动、归档 | 文件SQLite和真实WPF；历史路径、任务标题、节点ID及时间保持，归档详情可打开 |
| 任务后来改绑 | 文件SQLite与WPF生命周期；历史记录不重新归类 |
| 编辑/软删/恢复/片段排除 | 原有回归与M3子树统计对照；恢复重新验证，返回知识页刷新 |
| 跨日和纯时长补录 | 可控时钟跨上海午夜；真实WPF纯时长无起止位置；文件SQLite保存 |
| 未关联节点 | 明确单列未关联时长，不从科目汇总中丢失 |
| 薄弱点关联、解除、历史 | 同事务新建与关联，两个故障点回滚，重试与旧版本拒绝；解绑保留弱点和原修订 |
| 主/迷你同步与页面/窗口切换 | 实际DispatcherTimer触发；运行中读绑定文本；最小化/恢复、知识页加载后继续刷新 |
| 10,000节点查询 | 文件SQLite耗时68.7549ms；最终WPF采样中查询30.0～73.3ms，不占主VM Busy |
| M1/M2回归 | 同一185项套件涵盖知识树、包格式/安装/幂等/冲突/保护/升级/回滚及失败路径 |
| 备份与迁移 | 真实SQLite文件、格式1～4校验/候选升级/完整恢复、DDL与替换故障、旧版本拒绝 |

统计从去重后的有效Part.Id聚合原始毫秒，最后格式化；次数为有日期内贡献的Session.Id。自身/子树不可相加。学习状态、任务完成、自评掌握、验收保持独立。

## 实际环境

Windows 11 x64，OS build 10.0.26200；.NET SDK 10.0.401，运行时10.0.12；WPF，CommunityToolkit.Mvvm8.4.2，Microsoft.Data.Sqlite10.0.12；xUnit2.9.3 / VSTest。最终原生窗口DPI192（200%），逻辑1200×800 / 900×600，物理PNG2400×1600 / 1800×1200。

连续样本：自由00:00:01→02→03；倒计时与番茄00:00:58→57→56。没有用Tick加1秒作为事实，采样同时记录原始毫秒、实际UTC、DispatcherTick次数、主/迷你文字及查询耗时。整数秒采用现有显示策略，底层不改精度。

## 原始失败证据

- context-red：4项失败，随后context-green4项通过。
- regression-initial：171通过/1失败；失败为旧fresh-schema断言仍期望3，随正式Schema4更新。
- workflow-red：6通过/1失败，缺少计时提交前故障注入；修正后workflow-green7通过。
- workflow-extra：12通过/1失败；错误断言以为受保护包节点必然出现在变更集合，实际为保留冲突且不归档；修正断言后最终回归通过，没有削弱保护行为。
- window-initial：主题模板重建时查询按钮失败；增加布局稳定等待。
- window-round2：验收脚本在完成持久化Busy结束前启动下一模式，取到了前一条记录；31项最终流程等待活动及Busy都结束。
- window-final：30项逻辑检查通过，但一张PNG实际为最小化标题栏，不作为最终视觉验收。原图/原清单保留；window-accepted用恢复窗口和像素尺寸门槛，全部10图通过。
- review-red：独立审查指出的普通导航、失效补充提示及最近学习缺失，修正后4项通过。

不得把早期FAIL清单改写成PASS，亦不得把本报告摘要代替原始TRX、JSON与截图。

## NOT TESTED / 已知限制

- 未操作真实用户数据库、备份或Schema升级；正式迁移、安装包、干净机和长期实际使用留待M4。
- 未主动触发真实锁屏、休眠、重启、系统时间变化、断电或磁盘满；相关时间断续和写失败为可控/故障注入测试，不等同真实电源验收。
- 中文渲染及长标题已截图检查，WPF焦点与Tab遍历已脚本验证；物理键盘和真实中文输入法组合输入未验收。
- 实际DPI只验证本机200%，其他真实缩放比例未测；没有改系统DPI。
- 没有为本阶段重复验证六页完整旧视觉矩阵或无关交互。新补充区在900×600可需滚动，长目录及关联列表使用现有滚动/折叠。
- 大树性能为本机合成目录与少量合成学习记录，不能推断海量历史学习数据、低配置机器同样耗时。
- 关联撤销通过再编辑/解绑并保留修订完成，没有新增通用任意历史版本一键回放。

结论：M3具备进入M4准备的条件；**未进入M4，不宣称满足正式用户升级和正式发行条件**。发布ZIP条目SHA256/CRC、源码与程序对应关系以独立release-manifest.json为最终证据。
