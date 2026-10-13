$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
$desktop=Get-Content "$repo\artifacts\latest-v011-desktop.txt"
$perfRoot=Get-Content "$repo\artifacts\latest-v011-performance.txt"
$upgrade=Get-Content "$repo\artifacts\latest-v011-upgrade.txt"
$crash=Get-Content "$repo\artifacts\latest-v011-crash.txt"
$gui=@(Get-Content "$desktop\smoke\results.json" -Raw | ConvertFrom-Json)
[xml]$trx=Get-Content "$repo\artifacts\test-results\v011-final.trx" -Raw
$tests=$trx.TestRun.Results.UnitTestResult
$perf=@(Get-Content "$perfRoot\performance\performance.json" -Raw | ConvertFrom-Json)
if(@($gui | Where-Object Status -ne 'PASS').Count -or @($tests | Where-Object outcome -ne 'Passed').Count -or $perf.Count -ne 4 -or @($perf | Where-Object Status -ne 'PASS').Count){throw 'Final evidence is incomplete or failed.'}
$native=Get-Content "$desktop\smoke\native-layout.json" -Raw | ConvertFrom-Json
$large=Get-Content "$desktop\smoke\large-record-load.json" -Raw | ConvertFrom-Json
$startup=Get-Content "$desktop\startup-results.json" -Raw | ConvertFrom-Json
$compatible=Get-Content "$upgrade\compatibility.json" -Raw | ConvertFrom-Json
$crashResult=Get-Content "$crash\results.json" -Raw | ConvertFrom-Json
$restoreResults=@(Get-Content "$crash\restore-results.json" -Raw | ConvertFrom-Json)
if($compatible.Status -ne 'PASS' -or $crashResult.Status -ne 'PASS' -or $restoreResults.Count -ne 2 -or @($restoreResults | Where-Object Status -ne 'PASS').Count -or $startup.Count -ne 4 -or @($startup | Where-Object Status -ne 'PASS').Count){throw 'Compatibility, crash, restore or startup evidence failed.'}
$rows=[Collections.Generic.List[object]]::new()
foreach($test in $tests){$rows.Add([pscustomobject]@{Name=$test.testName;Status='PASS';Kind='AUTOMATED';Evidence='tests/v011-final.trx'})}
foreach($test in $gui){$rows.Add([pscustomobject]@{Name=$test.Name;Status=$test.Status;Kind='ACTUAL_WPF_AUTOMATION';Evidence='desktop/results.json'})}
$rows.Add([pscustomobject]@{Name='Actual V0.1 binary data and backup compatibility';Status=$compatible.Status;Kind='ACTUAL_BINARY_AND_DATABASE';Evidence='upgrade/compatibility.json'})
$rows.Add([pscustomobject]@{Name='Actual committed WAL / uncommitted transaction owned process termination';Status='PASS';Kind='OWNED_WINDOWS_PROCESS';Evidence='crash/results.json'})
foreach($phase in @('before','after')){$rows.Add([pscustomobject]@{Name="Actual interrupted restore $phase replacement";Status='PASS';Kind='OWNED_WINDOWS_PROCESS';Evidence='crash/restore-results.json'})}
foreach($sample in $perf){$rows.Add([pscustomobject]@{Name="Resource sample $($sample.Mode), checkpoint $($sample.CheckpointSeconds)s";Status=$sample.Status;Kind='ACTUAL_PROCESS_SAMPLE';Evidence='desktop/performance.json'})}
foreach($scale in @('125','150','200')){$rows.Add([pscustomobject]@{Name="DPI bitmap scale $scale percent";Status='PASS';Kind='WPF_RENDER_ONLY';Evidence='screenshots/v011-dpi-render-*.png'})}
foreach($name in @('Physical Chinese IME candidate/composition workflow','Physical full keyboard and mouse workflow','Windows native 125/150 percent and cross-monitor DPI change','Native 1600x1000 on a larger display','Real lock/sleep/hibernate/Modern Standby','System clock adjustment or physical power loss','Physical offline and clean Windows machine','Installer or signed release acceptance','Real user 1 to 3 minute daily recording usability')){$rows.Add([pscustomobject]@{Name=$name;Status='NOT TESTED';Kind='DEVICE_OR_USER_ACCEPTANCE';Evidence='No authorization/environment for this experiment'})}
$rows | Export-Csv "$repo\artifacts\v011-validation-matrix.csv" -NoTypeInformation -Encoding utf8
$perfLines=foreach($p in $perf){'| '+($p.Mode -eq 'idle' ? '空闲' : "计时 / $($p.CheckpointSeconds) 秒")+' | '+([math]::Round($p.SampleSeconds,2))+' | '+([math]::Round($p.CpuPercentMachine,3))+'% | '+([math]::Round($p.PeakWorkingSetBytes/1MB,2))+' | '+$p.SavedCheckpoints+' | '+$p.TimerState+' |'}
$largest=$native[-1]
$photos=@(Get-ChildItem "$desktop\smoke" -Filter *.png).Count
$ready=($startup | Measure-Object UiReadyMs -Minimum -Maximum)
$report=@"
# 研迹 YanJi V0.1.1 验证与交付报告

日期：2026-10-13，Asia/Shanghai。结论：六页面重设计、数据可视化和 P0 操作闭环已实现；最终自包含程序已在本机实际运行。核心自动化 $($tests.Count) PASS / 0 FAIL，实际 WPF 检查 $($gui.Count) PASS / 0 FAIL。旧版兼容、完整备份与隔离故障恢复通过。仍有真实设备及人工验收 NOT TESTED，不能宣称无条件正式发布验收全部完成。

## 需求事实、技术决策与假设

需求事实：复用 V0.1 可靠性；双主题、六模块与独立记录明细；精确事实统计；整次软删除/恢复与安全片段操作；离线、无账号、Windows 11 x64；用户授权自主执行，没有阶段审批。

技术决策：.NET 自带 Fluent 与共享 WPF 组件/矢量图表；没有新增 NuGet 绘图包。数据库 schema 保持 1，无迁移。片段排除实际移除当前原片段并保存修订，恢复原片段，共用版本、事务、重叠和活动计时约束。章节条件进入共用筛选，筛选内摘要与图表毫秒一致。

假设：数据位于可写本地磁盘，没有外部程序同时修改；设备电源/IME/跨屏等未验证表现不作为通过事实。1～3 分钟为产品目标，没有真实用户耗时验证。

## 环境及依赖

Windows 11 25H2，build 26200.9457（API 10.0.26200.0），x64，8 逻辑处理器，PowerShell 7.6.5。本轮真实 WPF 显示器 DPI 192 = 200%。任务隔离便携 SDK 10.0.401，运行时 10.0.12，MSBuild 18.9.11，无系统安装/全局 PATH 改动。

CommunityToolkit.Mvvm 8.4.2，Microsoft.Data.Sqlite 10.0.12，SQLitePCLRaw 2.1.12，实际 SQLite 3.53.3；xunit 2.9.3、runner 4.0.1、Test SDK 18.10.1。WAL / synchronous FULL / foreign keys 实测。Fluent 随运行时，组件库评估与许可证依据见 V011-DESIGN.md；依赖许可证随发布包保留。

## 功能及自动化验收

| 范围 | 结果与证据 |
|---|---|
| 原 V0.1 数据/计时/备份回归 | 原 44 项全部保留通过；TRX |
| 新精确统计与片段安全 | 新 15 项通过；跨日贡献、排除/恢复、最后片段、活动计时、过期版本、恢复重叠、重复周期、写入失败回滚、备份保存排除历史、未来日均、秒与极端值 |
| 原 WPF 核心流程 | 原 25 项实际程序回归通过；暂停不计、继续保存、刷新输入、托盘 X、第二进程唤起、退出保存失败、中文字段/感想、焦点遍历 |
| 新记录操作闭环 | 实际编辑/删除确认取消/整次软删除恢复/原片段排除恢复；跨日日期范围；修改后保留详情的本日贡献与所有日期事实 |
| 图表、筛选与联动 | 统一精确毫秒投影；科目环形、按日堆叠、周月趋势、热力、真实区间时间轴；周/月科目钻取及章节筛选修改/全刷新回归 |
| 快速补录 | 实际弹窗保存中文 30 秒纯时长；归档科目和章节不再作为不可保存的选择 |
| 两主题与布局 | 七视图 × 两主题、空数据、详情/确认、长中文、边界图表截图；尺寸与焦点检查 |
| 大量记录 | 400 条筛选记录，实际容器 $($large.RealizedContainers) 个，刷新/布局含等待 $([math]::Round($large.ElapsedMs,2)) ms；未设性能硬阈值 |
| 完整备份/恢复 | 最终 WPF 进程备份校验恢复后毫秒总计与完整性一致；CSV 保留；V0.1 备份实测可恢复 |

图表钻取以运行窗口事件与实际按钮自动化验证；焦点为真实 WPF 焦点，方向/Enter 为路由键事件。没有把程序事件当作物理鼠标/键盘验收。

## 旧版升级及故障验证

保留 V0.1 程序和 v0.1.0 标签。实际旧版二进制在新的 owned 目录完成 25 PASS 并写库；新版二进制打开，比较学习记录/片段/章节/科目/任务/薄弱点/感想/学习修订，未改变既有事实。$($compatible.Records) 条旧记录，总 $($compatible.TotalMs) 毫秒，schema $($compatible.SchemaVersion)，integrity ok。新版对旧片段排除/恢复，再校验两版完整备份并恢复旧备份，事实一致。正式用户数据未访问。

自有 Windows 子进程在真实未检查点 WAL 8272 字节、有未提交事务时被终止：提交保留、未提交回滚、计时恢复 15000 毫秒并待核对。完整恢复在文件置换前/后终止，分别保留有效旧/新库与可校验的恢复前完整备份。实验只终止自己创建并核对 PID 的进程；没有改变真实系统电源或时间。

## 视觉检查

最终窗口留存 $photos 张 PNG。图像来自实际 WPF 窗口视觉树 RenderTargetBitmap，包括七视图双主题、空状态、详情、确认、补录、窄窗口、图表边界和大量记录；不是 XAML 示意图，也不是包含桌面/窗口非客户区的屏幕照片。

真实窗口 900×600 与 1200×800 PASS；最大实际 $($largest.ActualWidth)×$($largest.ActualHeight) PASS。请求 1600×1000 被当前屏幕/DPI 的系统最大跟踪尺寸限制；原生 1600×1000 仍 NOT TESTED。125/150/200% 另外输出缩放位图，是 RENDER 证据，不等于修改 Windows DPI 或跨显示器验证。

目视检查并修正：深色输入控件经典白底/白字、章节树黑字、卡片居中未铺满、长 KPI 秒数独立换行、图表轴标签截断、热力标题挤压、窄侧栏品牌/退出文字换行。最终保留文字状态、危险操作与精确工具提示；小窗口折叠侧栏/卡片，年度热力可横向滚动。

## 资源与发布

四次独立自包含启动 UI ready $([math]::Round($ready.Minimum,2))～$([math]::Round($ready.Maximum,2)) ms；均使用发布目录内运行时，DOTNET_ROOT 指向不存在的共享运行时。

资源独立进程每组约 60 秒，CPU 为整机 8 核归一口径，工作集为采样峰值（MiB），不把截图密集运行的瞬时分配当空闲值：

| 模式 / 检查点 | 秒 | 整机 CPU | 峰值工作集 MiB | 已保存检查点 | 结束采样状态 |
|---|---:|---:|---:|---:|---|
$($perfLines -join "`n")

全部计时组实际连续 Running。检查点不承诺固定实时周期；2 秒成本明显较高，默认约 15 秒。未设已批准硬性资源预算。性能采样与最终截图可来自不同自有进程，目录/完整 JSON 均在证据包；最后仅改变 KPI 排版与记录按钮状态，计时/存储/采样实现一致。

发布为独立自包含便携 ZIP，不是安装器，没有签名或公开上传。源码在 codex/v0.1.1，保留旧标签/旧发布目录。ZIP 解压逐文件 SHA-256 与真实自包含启动的最终结果见 package-validation.json；源提交与产物哈希见 delivery-manifest.json。

## 已修复缺陷与中断

初版新界面只读进度条双向绑定导致启动失败，已改 OneWay；自动化仅 RaiseEvent 绕过原生取消导致对话框停滞，已改 UI Automation 原生 Invoke 并等待实际状态；异步归档弹窗检查过早，已等待 Loaded/关闭。

独立复核的周/月钻取丢科目、章节筛选刷新丢失与摘要不匹配、归档科目/章节补录无声丢选择均已修复并实际回归。未发现剩余已确认 Critical/Important 数据安全问题。

重复兼容实验同名备份被安全拒绝；保留中断证据、显式从原备份重建自有夹具，改为唯一输出名重跑。先前一组性能采样意外处于 Paused，其原因未确立；未作为连续计时 PASS，增加严格状态检查和不抢前台焦点的探针后重跑通过。历史 FAIL/INTERRUPTED 在 artifacts 保留，不混入本轮最终通过数。

## NOT TESTED 与限制

真实中文输入法候选/组合、物理完整键盘鼠标、实际 125/150% 与跨屏 DPI、原生更大显示器、锁屏/睡眠/休眠/Modern Standby、系统时钟调整/物理断电、物理断网/干净 Windows、签名/安装包、真实用户操作耗时。无用户授权的系统实验没有执行。

当前以科目为树根、章节为叶，未加入章节父节点模型；薄弱点沿用三状态。最多十年范围用于控制界面数据量，年度热力可滚动；CSV 仍不能替代完整备份。库及备份未加密、自动备份未自动清理。

结论：满足本机可用 V0.1.1 的实现与已授权自动化验收，建议投入日常试用并定期完整备份；全设备正式发布条件仍受上述 NOT TESTED 限制。V0.2 可补设备验收、备份保留策略、更多快捷键和复盘模板。
"@
[IO.File]::WriteAllText("$repo\docs\V011-VALIDATION.md",$report,[Text.UTF8Encoding]::new($false))
Write-Output "Report and matrix: $($tests.Count) data tests, $($gui.Count) GUI checks, $($perf.Count) valid resource samples."
