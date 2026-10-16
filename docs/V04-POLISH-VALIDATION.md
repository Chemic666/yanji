# RC-Polish 定向验证报告

状态：READY FOR FINAL APPROVAL（隔离人工试用候选；非公开Stable）。程序提交 `e62aa0ff29f4f5cfca6037d00882489417e8108c`。

## 环境与范围

Windows 11 10.0.26200、x64、.NET SDK 10.0.401 / Runtime 10.0.12，原生192DPI（200%）。只使用artifacts/v04-polish下的合成数据库；无真实数据读取、复制、升级，无系统时钟/DPI/电源设置修改。

最终ZIP已校验CRC/逐文件SHA并实际解压到唯一publish目录；随后启动该目录程序进行perf-accepted与native-accepted。性能为默认WPF渲染；截图验收为测试入口SoftwareOnly + 实际HWND PrintWindow。不是网页截图，也不是物理键鼠/输入法通过证据。

## 周期卡顿证据

相同场景：300条历史、395知识节点、1200×800，依次今日/展开目录/学习数据，约30秒暂停300ms并继续，约15秒检查点保留。计时来自既有可信时间源，不按Tick累加秒数。

| 指标 | 修复前（perf-before） | 最终候选（perf-accepted） |
|---|---:|---:|
| 实际观察 | 125.501秒 | 125.313秒 |
| 周期内整页Refresh | 7 | 0 |
| UI心跳最大间隔 | 177.12ms | 140.61ms |
| UI心跳P99 | 42.49ms | 47.41ms |
| 心跳间隔>50ms | 9 | 8 |
| Tick异步墙钟最大值 | 113.26ms | 170.14ms |

修复前7次周期刷新中位79.18ms、最大98.18ms，均UI线程；后台SaveTimer中位9.89ms，说明原问题主要在保存后的整页刷新链。本轮消除了该周期性整页重建，未通过增大间隔隐藏问题。

最终Tick异步最大170.14ms位于约62秒，包含后台保存51.76ms及UI调度等待；相邻心跳最大56.67ms，不能把异步墙钟等同UI持续阻塞。最大140.61ms心跳在主动暂停时。主动暂停131.14ms、继续17.08ms、结束134.50ms；暂停的显式Refresh81.86ms仍存在。**不宣称所有UI操作<50ms**，也不宣称长期运行/任意硬件绝无卡顿。

最终124次采样主窗/迷你一致，原始保存124796ms，ProjectedFinalMs相同；暂停不增长。完整原始结果、spans（开始时间/子步骤/线程/分配）、heartbeat、samples、actions均保留。前后采样不是统计显著性实验或帧率测试。perf-after/perf-final属于中间程序，不能替代最终perf-accepted。

## 定向WPF与数据回归

- native-accepted/result.json：62项PASS（含快捷检查的1项聚合门）；10张主窗口PNG、9次三模式秒级采样。
- shortcut-result.json：20项PASS、4张组件PNG。这20项包含在上述聚合门内，不当作82项独立测试宣传。
- 自由/倒计时/番茄三模式各3次真实不同时间点读取主/迷你文本；暂停2100ms冻结、继续、保存精确毫秒；托盘/最小化/恢复/切页；不触发系统睡眠。
- 独立进度、任务菜单、记录编辑/软删除/恢复、明细及更多入口、隔离备份校验与统计逐毫秒一致。
- 单行Enter、多行Ctrl+Enter、组合事件屏障、重复提交保护；故意制造真实SQLite版本冲突后输入保留，重试成功；补录30,000ms为DurationOnly且无伪造时间戳；节点名称快捷保存稳定ID不变，搜索Enter不提交。
- 真实文件只读复核：accepted-data Schema4、integrity=ok、FK错误0、3条有效记录合计11,426ms；perf-accepted-data 301条合计699,124,796ms（包含300条合成历史），本次124,796ms准确。见final-data-and-retention.json。
- Core/Data/tests/resources/依赖版本对基线无差异，复用既有216 PASS TRX及SHA（core/reuse.json），**本轮未重跑216项**。新增工作集中在受影响WPF路径；这不是新数据库迁移或全知识包生命周期复验。
- Release构建/自包含发布成功；最终ZIP许可检查PASS。原Tag、RC-Simple.2三个ZIP重新散列一致，没有删除历史文件。

## 失败与修复

failure-index.json保留所有重要失败与中间结果：按钮本地Content覆盖状态文字；保存中旧帧截图；导航无目标Reload取消指定节点加载。native-navigation-trace调用栈、native-delivery失败、未奏效的下拉源保护实验均保留，没有修改历史JSON为PASS。最终导航修正只跳过KnowledgeBusy期间的重复刷新；定向20项与最终集成均通过。

## 视觉验收与限制

已人工查看最终真实PNG：三主页面、900×600深色、浅深按钮状态、保存中/成功帧，无可见重叠或遮挡；高度不足使用滚动，未重新设计页面。完整14张及逐图SHA/来源在review与native-accepted；comparison包含RC-Simple.2真实旧图3张及最终图。颜色不是保存成功的唯一反馈。

NOT TESTED：物理微软拼音候选窗/实际Enter键事件；全键盘/鼠标走查、悬停按住全过程、系统减少动画设置切换；其他真实DPI、睡眠/休眠/重启、干净机离线、屏幕阅读器、长期稳定性、真实用户升级。模拟组合事件不能证明物理IME。约5分钟人工步骤见V04-POLISH-START.md。

已知P2：主动暂停/结束汇总仍约130～135ms；单次异步Tick仍可受后台IO/调度影响。无已知数据损坏或计时事实错误；最终是否舒适仍需用户隔离日常试用。
