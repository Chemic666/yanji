# 自然分层 · Quiet Focus + Soft Depth

本轮基线 v0.4.0-rc.calendar.1，Git HEAD fcfda5f。仅调整视觉资源、一个日历展示属性及隔离验收入口，不改变布局、学习事实、Schema4、迁移、知识包或计时机制。

## 统一边界

| 区域 | 采用方式 |
|---|---|
| 卡片与图表容器 | 卡片12DIP圆角，常态不描边；暖白主表面与灰绿辅助表面形成层级，保持原间距，不给每个区域添加阴影 |
| 输入框与选择器 | 浅色或深色内嵌底色、1DIP低对比底线、10DIP圆角；悬停改变底色。焦点使用半透明1.5DIP外光环及强调底线，保留明确键盘提示 |
| 按钮 | 取消常态外围描边，保留轻渐变、仅上沿高光和几何浅阴影；原140ms反馈与1.5DIP按压位移保留，系统减少动画/高对比路径不移除 |
| 导航与目录 | 选中用背景与文字强调，取消选中矩形描边。导航焦点用左侧标记，目录/记录键盘焦点保留细线 |
| 日历 | 有记录日期用浅背景，选择用更明显填充，今天保留“今”字样；待核对细底线及文字语义保留。无表格外框，不更改时长或日期口径 |
| 分隔与菜单 | 分隔线使用独立Separator色，图表轴线仍使用原Line色。菜单采用圆角填充和低对比0.5DIP边缘，移除默认灰色图标沟槽；保留Header、Command、Check、手势文字、原生子菜单部件 |

没有引入实时模糊或DropShadowEffect。阴影只使用轻量几何图层。静态卡片大多原本已没有边框，主要修正输入框、按钮与默认菜单模板等真实来源，避免盲目给卡片叠加效果。

## 设计资源与Skill

- ui-ux-pro-max：执行本地WPF与focus visible keyboard查询；采用原生XAML布局、可见焦点、异步长操作原则，未安装依赖。
- make-interfaces-feel-better：读取SKILL及surfaces指导，采用“阴影表达层次、边界表达结构与状态”、统一半径和克制反馈。CSS box-shadow、Web动画、浏览器字体平滑未直接引入WPF。
- computer-use：真实Windows鼠标/键盘与样板状态采集。实际状态JSON必须证明Normal无悬停、Hover无按下、Pressed=true，不能仅凭文件名判断。
- 验证遵循verification-before-completion；核心业务不变量沿用yanji-domain-rules，源代码及Schema语义未改。

## 运行与隔离

候选沿用程序集版本0.4.0-rc.calendar.1，包含未提交样式改动，不能只凭该版本号或HEAD代表本轮程序；BUILD-INFO和source-state记录编译输入指纹及DLL SHA256。本轮未得到新的Git提交授权，因此未提交、未建标签、未推送。

将Windows候选ZIP解压到新的可写目录运行YanJi.exe。默认仅使用程序旁data-rc-calendar。不要覆盖已使用的目录，不要指定真实学习库。视觉截图与检查全部使用新的artifacts/surface-refinement隔离数据库，未访问真实LocalAppData数据。

审核包提供comparison.html和全部相对图片依赖；页图来自真实WPF HWND，非网页模拟。窗口尺寸为DIP，本机192DPI，1200×800对应2400×1600像素，900×600对应1800×1200像素。日历近景为原始窗口图的像素裁切，保留源图与裁切坐标。

验收结果、来源指纹、未验证项和精确变更清单见SOFT-SURFACES-VALIDATION.md及原始JSON。完成后只等待人工验收，不进入下一轮重构。
