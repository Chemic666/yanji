# YanJi V0.4.0 RC-Polish.2 · 交付

状态：READY FOR FINAL APPROVAL（仅隔离人工试用；非Stable）。最终程序提交 `a8e26fc765aad0d7e7f4b085983880180b73dbc3`，恢复标签 `v0.4.0-rc.polish.2` 指向该提交。交付文档/封包检查HEAD可以在其后，src/resources对程序提交必须无差异。准确HEAD、全部ZIP哈希与最终CRC/逐文件SHA见release-manifest.json、SHA256SUMS.txt、final-closure.json。

## 本轮结果

1. 空闲×与Alt+F4走统一退出；活动计时提供保存退出、后台继续、取消。退出重入、写入失败、未保存目录/下一段备注均有保护；置顶迷你不遮挡确认。
2. 默认知识包展示所有科目；数学一/二、408、英语一/二均可找到；已安装408常驻并可查看目录。177节点、原145外部ID及用户修改/学习事实保持，未更改包内容。
3. 迷你420×260DIP，无标题栏、约90%半透明面板、不透明数字、细边/静态层次；拖动/位置记忆/置顶、悬停或键盘操作、×只隐藏。
4. 共享实际ControlTemplate提供柔和明暗、高光与绘制阴影，Pressed降低1.5DIP，焦点2DIP轮廓，主/次/Ghost/Danger/Icon等级；保存中/成功有文字。140ms轻反馈，拒绝动态模糊、Apple素材与新框架。
5. 保留此前15秒后台防丢保存，暂停或结束刷新汇总。两次125秒最终程序观察无周期整页重建，时长/主迷你同步通过；仍存在可量化调度与一次性汇总开销，详见验证报告，不把数据PASS当作零卡顿承诺。

## 交付位置

- Windows：`releases/YanJi-v0.4.0-rc.polish.2-a8e26fc7/YanJi-v0.4.0-rc.polish.2-windows-x64.zip`。
- 源码：同目录`YanJi-v0.4.0-rc.polish.2-source.zip`，包含正式设计/使用/验证文档与本地Git对应源码。
- 审核：同目录`YanJi-V04-RC-Polish2-Review.zip`，打开`evidence/review-final/comparison.html`（HTML与全部图片一起解压）；原始定向结果在native-approval、两轮最终性能在perf-approval/perf-foreground，216原始TRX在core。
- 便于本机查看：`artifacts/v04-polish2/review-final/comparison.html`。
- 启动：Windows ZIP完整解压新目录后双击YanJi.exe，默认独立`data-rc-polish2`。勿覆盖旧程序或选择真实库。

## 主要文件与边界

MainWindow.xaml.cs统一Closing与退出；KnowledgePackageViewModel/Window展示与已装入口；MiniTimerWindow与ThemeService材质/位置偏好；Styles/ButtonFeedback共享按钮模板；KnowledgeWorkspaceViewModel未保存检测；MainViewModel仅修正旧关闭提示文案；App/csproj为隔离入口及版本；Polish2Validation为定向验收，SimpleInteractionValidation适配关闭选项；tools/Package-Polish2.py强制源码/二进制/证据/许可证/哈希闭合。

Schema4、Core、Data、知识包JSON、原始毫秒、修订和备份格式无变化。Apache-2.0与原第三方声明完整，无新增依赖、无Apple字体/图标、无真实库操作、无历史Tag/ZIP删除、无远程推送。复用一个自包含解压运行目录，必要修复产生的中间ZIP保留为明确的非交付候选；最终选择见final-check-selection。

## 验证与Skill

最终37定向PASS（含20项快捷检查的聚合门）；216核心TRX有条件复用、没有重跑。最终二进制两轮125秒时长/同步PASS；原生OS关闭、透明叠加与按钮状态来源、复用条件在VALIDATION及review-final/provenance中明确。只进行受影响路径验证，没有追加无关全量测试。

实际读取项目ui-ux-pro-max、make-interfaces-feel-better并执行前者本地WPF检索，落实显式Style继承/层次/按压/光学对齐；yanji-domain-rules保护事实/ID/事务；systematic-debugging与verification-before-completion用于复现和封包校验；computer-use用于真实Windows输入。属于文件级指导与查询，不宣称自动加载所有Skill或运行Web设计引擎。详细采纳/拒绝见DESIGN；参考HIG与Fluent原则，不移植macOS窗口或专有素材。

## 人工试用与限制

物理微软拼音候选窗与真实组合输入；任务栏关闭点击、物理托盘菜单；其他真实DPI/混合多屏；系统减少动画设置切换、高对比度实机；睡眠/休眠/重启；干净Windows机及长时间稳定性。

已知P2：暂停/结束汇总仍有一次性开销，性能记录存在调度抖动；混合DPI多屏位置、物理IME仍待复核。两轮原始数据完整保留，不宣称所有操作<50ms。用户只需按START在隔离目录做约5分钟关闭/中文输入/迷你拖动体感检查。

本轮到候选包为止，不公开发布，不升级真实数据，不进入V0.5。
