# RC-Polish.2 · 局部修复计划

基线：v0.4.0-rc.polish.1 / 程序e62aa0ff，文档HEAD bfb9d29。仅隔离数据，保留Schema4、知识包内容、原始毫秒、15秒防丢机制、版权材料。不增加新框架或业务模型。

- [x] 复现关闭重入与默认数学筛选；核对最终ZIP的408=177节点及145稳定外部ID。
- [x] 统一关闭路径：Closing回调返回后调度；活动计时结束保存/继续后台/取消；失败不退出、重复关闭串行保护；未保存知识编辑确认。
- [x] 知识包默认全部科目，保留明确筛选和目标校验；安装状态常驻、已装查看目录、原有预览/更新/冲突流程复用。
- [x] 迷你窗：WindowStyle=None、AllowsTransparency、独立不透明文字、约90%面板、圆角/细边；拖动、位置与置顶持久化，悬停/键盘焦点呈现操作。无动态背景模糊。
- [x] Soft Depth按钮：共享模板/派生等级，上沿高光、轻量绘制阴影、垂直明暗、明确按压和焦点；140ms轻动效，尊重系统动画/高对比度。真实控件属性与像素状态验收，不用静态设计稿代替。
- [x] 受影响WPF/SQLite检查、真实Windows关闭操作、同尺寸前后/五状态截图、1200×800与900×600浅深和本机200%DPI、125秒连续计时/主迷你同步；保留失败原件。
- [x] 最终封包检查已实现：ZIP关闭后CRC/逐文件SHA/图片清单核对，记录程序与文档提交；执行结论以release-manifest.json与final-closure.json为准，候选交付后停止。

设计依据：项目ui-ux-pro-max的WPF显式样式继承与CanExecute建议；make-interfaces-feel-better的层次/光学对齐/轻量反馈；Apple HIG Materials/Buttons只提炼可读材质和清晰状态；Windows Fluent layering/elevation采用按压降低层级。拒绝CSS/GSAP、Apple字体/SF Symbols、Liquid Glass仿真和大面积动态模糊。系统字体及现有资源不变。

官方参考：https://developer.apple.com/design/human-interface-guidelines/materials ，https://developer.apple.com/design/human-interface-guidelines/buttons ，https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/layering ，https://learn.microsoft.com/en-us/dotnet/api/system.windows.window.closing ，https://learn.microsoft.com/en-us/dotnet/api/system.windows.window.allowstransparency 。
