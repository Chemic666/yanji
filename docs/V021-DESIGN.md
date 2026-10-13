# 研迹 V0.2.1 · 静桌

本版深化现有暖纸、柔绿 Quiet Focus。正式实现是 .NET 10 WPF，使用原有 XAML ResourceDictionary、命名 Grid、原生图表绘制和语义画刷；没有新增 UI 库、字体或 Web 样式层。

## 共享规范

| 角色 | 规范 |
|---|---|
| 画布/内容 | 浅色暖纸 `Canvas` / 微暖 `CardBackground`；深色独立成对 token，保留系统高对比映射 |
| 字体 | 已安装 Segoe UI Variable Text / Segoe UI / Microsoft YaHei UI；不下载商业字体 |
| 标题/正文/元信息 | 页标题 24，区域标题 16，正文 14，元信息 12 DIP；明确字重、行高与组间距 |
| 数字/单位 | NaturalDuration 从原始 long 毫秒生成 Run：数字 SemiBold，单位 55% 字号；Tabular numerals；原毫秒不回写、不从字符串反解析 |
| 时长精度 | 小于一分钟显示秒和最多三位小数；Compact 省略低位用 ≈，完整值和毫秒仍在 Tooltip/可访问名/详情 |
| 间距/容器 | 4/8/12/16/24/32；宽页 gutter24、紧凑20；SectionSurface16 padding / DenseSurface12，圆角12，输入/按钮8 |
| 交互 | 桌面按钮至少40 DIP；开始44 DIP；查看、编辑、删除显式可见；静态 hover/focus/pressed/disabled 反馈 |
| 动效 | 不新增页面入场、数字 tween、按钮 scale、尺寸动画；高频操作保持静态反馈 |

共享资源包含 SectionSurface、DenseSurface、SectionTitle、MetaText、InlineStatus、Toolbar、RecordItem 与 EmptyState。既有 Focus* 输入、弹层和危险确认样式继续使用。

## 页面构图

- **今日学习**：快速补录降为次级操作；今日/周/月是紧凑指标带。侧栏折叠与内容双栏独立判断；900 和1200保留计时与记录双栏。左侧计时后接回顾；右侧有界 Recycling 摘要列表后接固定七日节奏。Compact 行保留查看/编辑/删除，整次身份和跨日贡献说明不变；摘要仍最多8条。
- **统计复盘**：范围预设与原始记录入口同页头；日期/科目/刷新采用立即生效工具栏。三个指标保留各自分母，未来范围的自然日均仅用截至今日贡献。宽窗每日实际时长主图与科目排序横条按约1.7:1排列；900先完整主图，再科目构成。趋势是较低高度的第二层；热力和章节仍可滚到。
- **历程**：月历和日事实分栏，月历按自身内容高，不与长文强制等高。纯时长与区间时间轴保持区分；日历42格、周一开始、跨日和时区事实保留。
- **计划**：章节去掉不必要 MinWidth280，内容换行；树与待办顶部对齐，进度/掌握保持独立。
- **薄弱点**：搜索与选择器统一40高；长标题和困难正文可读，列表主宽、分布辅助，修订渐进展示。
- **设置**：档案、主题、计时偏好归为连续设置区，以分隔线组织；备份操作独立，恢复危险操作与导出/校验分组。路径换行，不填个人身份字段。
- **明细/详情**：full 行继续保留四动作及恢复入口，有界列表不变。drawer宽度受可用空间约束，关闭按钮稳定命名，历史折叠；背景禁用、焦点循环、Escape和失效原焦点 fallback 保留。

## 图表契约

Ranking 每科目一行44 DIP、一个非重叠 hit；值与占比文字直接标注。条长度按实际值/最大值，0不画假条；全零没有 hits。鼠标/Enter传递原 PlotPoint，范围/科目不从 label 解析。长名称由精确原生 Tooltip补全；低值通过整行可达，不放大数据条。MeasureOverride 遵守可用高度，避免 MaxHeight 下绘图区和命中区域被裁切。

## 拒绝的建议

不采用全卡双壳、胶囊、玻璃、grain、磁吸、首屏入场、商业字体与营销 Hero。不替换计时/SQLite/修订模型，不新建 ResponsivePanel。不把 native 称作 iOS/Android/adaptive，不用 DOM/CSS 扫描替代 WPF 验收。

真实覆盖与限制见 V021-VALIDATION.md；恢复与交付见 V021-DELIVERY.md。
