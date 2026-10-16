# RC-Polish.2 · Quiet Focus + Soft Depth

仅完善高频控件与浮窗，不重排三主页面。暖米白/鼠尾草绿/深灰绿与系统Segoe UI、微软雅黑回退延续原版；卡片仍以背景/间距区分，不堆叠边框。

按钮复用FocusButton共享模板和Primary/Secondary/Ghost/Danger/Icon派生样式：8DIP圆角，40DIP常规、44DIP主操作、32DIP紧凑图标；原内边距/间距保留。主/次按钮使用垂直半透明明暗层、1DIP上沿高光与两像素绘制阴影；无需DropShadowEffect或BlurEffect。Ghost/Icon去掉深度层，危险按钮保留独立红色语义。Hover强化覆盖/轮廓，Pressed压低1.5DIP并收起阴影，2DIP键盘焦点，禁用去深度；Saving/Success用实际文字，不只靠颜色。140ms透明度反馈尊重ClientAreaAnimation，高对比度去装饰层。

迷你窗420×260DIP、20DIP圆角、230/255的面板alpha、细边与两层静态浅影；整个Window.Opacity=1，仅背景半透明，文字不降透明度。AllowsTransparency + WindowStyle=None，不依赖Acrylic/Mica，不做昂贵背景采样。工具条悬停或键盘焦点显示；正常输入命中，不强制穿透。位置按DIP保存并在主工作区夹取，避免离屏；混合DPI多屏需人工复核。位置/置顶为原settings表中的偏好，独立排队写入，退出等待完成。

针对性Skill审核先于实现：ui-ux-pro-max查询“button states animation”无匹配，按指南重试“state setters”获得WPF显式Key/BasedOn建议，落实共享模板及派生等级；原CanExecute/IsEnabled语义保留，不为套用建议重写计时命令。make-interfaces-feel-better采用光学层次、可中断轻反馈、数字等宽和明确状态，拒绝CSS/GSAP/macOS字体平滑等Web专属实现。yanji-domain-rules保护事实/ID/事务，systematic-debugging以基线Alt+F4实机失败和实际Catalog列表定位。以上为实际读取和本地查询，不宣称运行Web设计引擎。

设计参考：Apple HIG [Materials](https://developer.apple.com/design/human-interface-guidelines/materials)与[Buttons](https://developer.apple.com/design/human-interface-guidelines/buttons)提炼少量材质、前景可读和清晰按压状态；不复刻Liquid Glass，不使用Apple资源。[Windows layering/elevation](https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/layering)用于主次层级及按下时降低深度，非直接调用WinUI ThemeShadow。WPF约束依据[Closing](https://learn.microsoft.com/en-us/dotnet/api/system.windows.window.closing)及[AllowsTransparency](https://learn.microsoft.com/en-us/dotnet/api/system.windows.window.allowstransparency)。
