# V0.2 视觉探索验证报告

## 实际环境与范围

2026-10-13；Windows 11 本机 OS API `10.0.26200.0`、x64；便携 .NET SDK 10.0.401；发布程序运行时 10.0.12。Segoe UI Variable Text / Display、Microsoft YaHei UI 等字体来自本机，没有下载字体。原源码基线 f8e5cd8eb2fca333c2d1988f78cae8e3c771194b，正式业务源码没有变化。

真实显示缩放 192 DPI / 200%。默认1200×800 DIP，小窗900×600 DIP；大窗原请求1500×950，被本机可用工作区限制为1455×915 DIP。没有修改系统缩放、时间或电源设置。

## PASS / FAIL / NOT TESTED

|项目|结论|证据与边界|
|---|---|---|
|既有自动化基线|PASS|59/59，`artifacts/v02-tests-baseline/v02-baseline.trx`|
|既有自动化最终回归|PASS|59/59，0失败0跳过，`artifacts/v02-tests-final/v02-final.trx`|
|发布样板运行断言|PASS|28/28，最终发布 run 的 interaction-results.json；真实 WPF/SQLite 调用及部分程序化路由事件|
|自包含发布和本机启动|PASS|win-x64；直接启动 YanJi.DesignLab.exe，exit0，运行时10.0.12|
|原生实际窗口捕获|PASS|验证进程拥有 HWND 后 Native PrintWindow；21常规/边界 +8空态，共29张原生PNG；另29张实际visual-tree辅助证据|
|主要深浅主题目视检查|PASS（检查者判断）|A/B × 今日/统计8张主要原生图逐一检查；结构不同、文字和图表可读；不是用户最终偏好验收|
|小窗、空态与辅助交互目视|PASS（抽查）|900×600两方案主要页面、空态代表、补录深浅、删除确认；次要内容允许纵向滚动，不声称29张逐个穷尽人工检查|
|精度及原始记录联动|PASS|秒级补录无伪造区间、27.125秒单点、极值毫秒不改动；科目筛选及图表下钻；soft delete / restore 后聚合恢复|
|暂停/继续/保存|PASS|真实 Windows 连续时间来源，等待暂停不增长，继续身份一致；SQLite重新实例读取|
|异常核对入口|PASS（模拟）|调用异常标记后入口即时更新，未知时间不自动计入；不代表真实电源恢复|
|主文字对比度|PASS（有限范围）|四主题画布对主文字11.48–14.26:1；未宣称全控件均符合完整WCAG审计|
|独立源码审查|PASS|2重要+3次要问题已修复复查；未剩阻断；不替代实际设备输入和用户选择|
|100/125/150% DPI、多显示器切换|NOT TESTED|只在本机200%实际捕获；未改系统设置|
|物理键鼠、中文输入法、屏幕阅读器|NOT TESTED|程序化中文绑定/持久化、路由事件和焦点约束通过；不是物理输入验证|
|真实锁屏/睡眠/休眠/时钟变化|NOT TESTED|没有主动触发；复用原逻辑与模拟检查|
|洁净机器、断网运行、安装器|NOT TESTED|仅本机自包含 exe；没有新安装器或系统级安装|
|GPU渲染/性能长跑|NOT TESTED|样板进程 SoftwareOnly；原正式应用渲染未改；未采集CPU/内存性能基准|
|用户偏好 / 六页正式推广|等待选择 / NOT TESTED|本轮只做两个页面双方向，不宣称正式V0.2发布|

## 原始证据与复现

最终常规发布：`artifacts/v02-final-release-acb2cd496ceb4b88813017fa7d9e2010`；空态：`artifacts/v02-final-empty-606e5f33ffd944bdb1be88aae8a2202e`。各目录 execution.json 退出0；screenshots/environment.json 记录进程、字体、运行时与隔离路径；capture-results.json 只标记 CAPTURED，不把文件生成当视觉通过。人工结论见上表。

原V0.1.1四张实际窗口：`artifacts/v02-baseline-5b58b7dbe96b4881a28d3caad345b014/screenshots`（具体文件名见该目录）。最新指针只帮助定位，不代替原始文件。

构建：`tools/Invoke-Build.ps1 -Arguments @('publish','design/YanJi.DesignLab/YanJi.DesignLab.csproj','-c','Release','-r','win-x64','--self-contained','true','-o','releases/YanJi-v0.2-visual-preview-win-x64','-m:1','-nr:false')`。

发布运行：`tools/Invoke-VisualLab.ps1 -Executable <发布 exe> -EvidenceName final-release`；空态另加 `-Empty`。脚本使用隔离GUID数据目录；完成退出，未擅自终止程序。生成的新发布目录中移除了项目引用多带出的 YanJi.exe，保留依赖 YanJi.dll，避免把原版入口误当作样板入口；原V0.1.1发布不变。

## 失败及后续修正

编译类型/路径错误、样板关闭重入、启动参数路径错误、选择器和深色按钮视觉错误都先产生失败，再修复复跑。硬件渲染的原生白图没有当作通过：仅样板选择软件渲染，重新截取实际窗口。之前 note-revision、review-entry-live FAIL 有独立证据；最终28项通过才作为交付结果。详见 LEDGER.md。

独立审查修复：核对入口实时变化；浮层隔离键盘并保留草稿；删除过滤不污染普通预览；深色日期输入；键盘提示实际打开。全部在最终发布产物再验证。

## 结论

可交付两套真实 WPF 视觉样板并进入用户方向选择。不能据此宣布正式V0.2六页面升级、完整可访问性或全部Windows设备验收完成。原正式数据未访问，核心源码、数据库结构与可靠性机制未改。
