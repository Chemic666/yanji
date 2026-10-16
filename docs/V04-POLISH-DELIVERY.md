# YanJi V0.4.0 RC-Polish 交付

状态：READY FOR FINAL APPROVAL。已完成本轮周期卡顿修复、快捷保存与按钮精修，停止本轮改造；未公开发布、未推送远程、未接触真实学习数据。

## 使用与结果

按用户决策，实时秒数继续每秒更新；页内汇总/记录列表/图表在暂停、结束和明确数据操作后刷新。后台防丢检查点仍约15秒一次，不改变SQLite事务、原始毫秒、暂停排除、跨日、未知间隙、知识包、Schema4、备份及Apache-2.0声明。

根因是TickAsync在后台保存后无条件触发UI全页Refresh。相同125秒场景周期重建7次降到0；实际数字和限制见V04-POLISH-VALIDATION.md，不将一次性保存/汇总约130ms描述为完全无停顿。

快捷键：单行Enter，多行Enter换行/Ctrl+Enter保存；组合输入期间禁止提交、搜索与下拉排除。高频编辑等待真实持久化成功才关闭；失败保留输入、重复提交阻止。物理微软拼音仍NOT TESTED。

按钮采用统一Primary/Secondary/Ghost/Danger/Icon，焦点轮廓、140ms轻量反馈、保存中/已保存文字；浅深主题复用原设计。另修正导航异步重复加载覆盖指定节点选择，保持稳定ID。

## 交付位置

- Windows x64：`releases/YanJi-v0.4.0-rc.polish.1-e62aa0ff/YanJi-v0.4.0-rc.polish.1-windows-x64.zip`（完整解压新可写目录，双击YanJi.exe；不覆盖旧版本）。
- 源码：同目录`YanJi-v0.4.0-rc.polish.1-source.zip`。
- 审核：同目录`YanJi-V04-RC-Polish-Review.zip`，含原始失败/通过证据、性能记录、截图、设计与验证说明；排除运行目录与原始DB。review/comparison.html及其图片一起入包。
- 本地真实WPF前后对比：`artifacts/v04-polish/review/comparison.html`。
- 启动与5分钟人工清单：V04-POLISH-START.md；组件规范：V04-POLISH-DESIGN.md。

Windows ZIP 75,714,543字节，SHA-256 `775D22167BCA263BF07510554C061F2D632788CF0D26D3214AFBA7B2A2344569`。各包封存后重新从ZIP读取所有文件，CRC与逐文件SHA一致；最终结果见release-manifest.json和独立final-closure.json。

## 源码追溯

分支`codex/v04-rc-polish`；程序提交`e62aa0ff29f4f5cfca6037d00882489417e8108c`，本地恢复标签`v0.4.0-rc.polish.1`指向该提交；程序集`0.4.0-rc.polish.1+e62aa0ff29f4f5cfca6037d00882489417e8108c`。后续HEAD仅为验证文档提交，二者不同是有意区分，release-manifest记录DocumentationHead。最终代码到文档HEAD的src/resources/版本配置无差异。

实际修改文件：

- `Directory.Build.props`
- `src/YanJi.App/App.xaml.cs`
- `src/YanJi.App/ButtonFeedback.cs`
- `src/YanJi.App/EditorDialog.cs`
- `src/YanJi.App/KnowledgeWorkspace.xaml`
- `src/YanJi.App/KnowledgeWorkspace.xaml.cs`
- `src/YanJi.App/MainViewModel.Presentation.cs`
- `src/YanJi.App/MainViewModel.cs`
- `src/YanJi.App/MainWindow.Learning.cs`
- `src/YanJi.App/MainWindow.Presentation.cs`
- `src/YanJi.App/MainWindow.Release.cs`
- `src/YanJi.App/MainWindow.Simple.cs`
- `src/YanJi.App/MainWindow.xaml`
- `src/YanJi.App/MainWindow.xaml.cs`
- `src/YanJi.App/PerfTrace.cs`
- `src/YanJi.App/PolishPerformance.cs`
- `src/YanJi.App/PolishUiChecks.cs`
- `src/YanJi.App/SaveShortcuts.cs`
- `src/YanJi.App/SimpleInteractionValidation.cs`
- `src/YanJi.App/Styles.xaml`
- `src/YanJi.App/YanJi.App.csproj`

## 验证与Skill

最终62项WPF集成门和其内20项快捷检查PASS；14张真实PNG、本机200%DPI、三模式主/迷你秒数、暂停/继续/结束、统计毫秒、失败保留输入、隔离备份均有证据。216项核心测试在源码未改条件下复用旧TRX，不重复计为本轮执行。

实际使用：systematic-debugging进行先复现和线程/调用栈定位；yanji-domain-rules约束计时与数据不变量；dotnet-wpf参考和ui-ux-pro-max本地WPF查询用于绑定、MVVM和反馈；verification-before-completion用于程序/截图/包/源码核对。未安装新Skill，未使用Web动画引擎，未宣称新会话自动加载成功。

尚未实测：物理IME、其他DPI/减少动画切换、完整鼠标键盘流程、真实电源恢复、干净机和长期运行。适合继续隔离人工日常试用；不是正式用户升级或公开发行授权。既有恢复Tag/ZIP/失败记录保持完整。
