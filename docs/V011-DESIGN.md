# V0.1.1 体验与组件规范

需求事实：六页面、现代 Fluent 仪表板、浅深主题、900×600 至大窗口、记录明细及安全片段操作是本轮授权。复用计时状态机、SQLite 模型、事务、修订和完整备份恢复；不访问正式用户数据。V0.1 标签和发布目录保留。

## 决策与组件评估（2026-10-13）

| 方案 | 当前兼容/维护证据 | 许可证 | 本次选择 |
|---|---|---|---|
| WPF UI | 官方 4.3.0 发布，4.1 起声明 .NET 10 支持；持续控件修复 | MIT | 可用；无需引入整个导航/窗口体系 |
| LiveCharts2 | 官方 WPF 列表 2.0.4；安装文档提示 SkiaSharp 3 的 Windows TFM 注意事项 | 核心 MIT，商业扩展另行授权 | 本次不引入原生绘图依赖 |
| ScottPlot 5 | 官方 WPF 支持与持续版本记录；擅长大数据科学绘图 | MIT | 此业务每日汇总图无需其科学绘图能力 |
| .NET WPF Fluent | 随运行时，官方支持浅深主题与标准控件 | 随 .NET 许可证 | 采用；局部 ThemeMode 实验 API 警告作用域限定在 ThemeService，实测双主题 |

图表采用 WPF 矢量控件，读同一有效片段投影，提供工具提示、明确数值表、键盘可选数据点及点击钻取。没有第二份统计事实源或额外组件包。取舍：需要维护几类简单业务图，但没有 Skia/native 包成本；未来高密度绘图可替换。

依据：[WPF UI](https://github.com/lepoco/wpfui/releases)、[LiveCharts](https://livecharts.dev/docs/wpf/list)、[LiveCharts license](https://github.com/Live-Charts/LiveCharts2/blob/master/LICENSE)、[ScottPlot](https://github.com/ScottPlot/ScottPlot)、[Fluent](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/whats-new/net90)。以上兼容声明不替代本项目运行验证。

## 视觉体系

8px 间距，卡片 16px 圆角/20px 内边距，输入与按钮 8px 圆角/36px 最小高；Segoe UI + Microsoft YaHei UI 中文后备。标题 28，区域标题 17，正文 13，辅助 11。轻边框，有限阴影；数据优先于装饰。

浅色冷灰背景/白面板，深色蓝灰背景/稍亮面板，青绿色主操作，红色危险操作，琥珀待核对；科目固定色并同时使用文字/图例，不能依颜色推断掌握。主题颜色通过 DynamicResource，原生输入控件采用 Fluent。焦点、悬停、禁用状态一致；图表不做延迟动画。

复用：MetricCard、StudyTimerCard、StudySessionRow、StudyTimeline、StudyHeatmap、SubjectDonutChart、StudyTrendChart、ChapterProgressTree、WeakPointCard、EmptyState、DetailDrawer、ConfirmDialog、StatusBadge、FilterBar。页面通过共享模板与小型矢量控件组合，不逐页复制样式。DetailDrawer 是同窗口覆盖详情层，提供片段/修订和操作，Esc 关闭；窄窗口占主要内容区。

## 布局和交互

今日：三 KPI + 紧凑计时卡，补录为明确按钮/弹窗，今日记录和感想并列；窄于 1100 逻辑像素转单列。历程：紧凑月历+每日详情，热力图可横向滚动；时间轴只放真实区间，纯时长独立列表。休息日由用户明确标记，零记录不自动称休息；待核对独立文字标签。

独立记录页作为第七个辅助导航视图，不改六大业务模块；日期/科目/来源/删除状态筛选，所有页面和图表钻取同一明细。记录行显式详情/编辑/删除/恢复，确认显示整次时长与全部受影响日期。

计划：科目为分组树的根、章节为叶（现有无章节父节点，不能虚构章节层级）；进度和掌握独立；紧凑待办。薄弱点：搜索/科目/现有三状态筛选，原因/复习日期/历史可见；不新增未经批准的业务状态。设置：档案、主题、计时、数据四区域。

统计：日/周/月/自定义日期和科目联动，环形占比、按日科目堆叠、周/月趋势、热力和章节投入/任务/弱点。聚合保持 long 毫秒；显示秒/分钟/小时，精确工具提示保留毫秒。日均分母为范围内已过自然日（含今天），学习日均另列，不混用；未来日期不增加分母。学习数据是已确认保存前缀，未知间隙不加入。

## 片段安全设计

不迁移数据库。排除片段是真正从当前 Parts 移除并保存修订中的原事实，不通过总时长补偿。必须保留至少一个有效片段；最后一个走整次软删除。恢复仅来自明确片段排除修订，沿用版本、活动计时限制、区间重叠和引用验证，失败完整回滚。先恢复整次记录再恢复其片段。所有日期投影和图表在同一次 UI Refresh 后更新。

假设：用户数据在本地磁盘，既有 V0.1 库未被外部修改；尚未实测的真实 IME、物理 DPI 切换、锁屏/电源、干净机、断网均继续 NOT TESTED。验证包含隔离 V0.1 数据打开/备份/恢复、片段操作负例、实际双主题/空数据/长文本/尺寸/DPI 位图截图；截图后实际修正问题。
