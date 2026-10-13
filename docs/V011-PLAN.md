# V0.1.1 实施计划

执行方式：本会话自主实现，使用 executing-plans；最后独立代码复核。用户明确取消中间审批。

Goal：交付可靠、双主题、可视化及记录操作完整的 Windows V0.1.1。
Architecture：保留 Core/Timer/Data；新增纯统计投影与片段操作；App 分出展示模型、主题、组件与页面布局。
Tech Stack：现有 .NET10/WPF/SQLite/MVVM；新增绘图库依赖为零。
Spec：V011-DESIGN.md。

约束：无正式用户库访问；V0.1 可恢复；动态主题；900×600；统计仅有效片段；未知和纯时长不伪造区间；片段修订事务与版本一致。

复核重点：跨午夜局部贡献/整次删除；过期或重叠片段恢复；未来范围日均分母与空数据；活跃计时刷新输入；深色日期弹窗与长中文/多科目布局。

1. [x] 基线与 P0 数据：保留 v0.1.0；新增 PartOperationTests 先 RED，新增 ExcludePart/RestorePart/ExcludedParts，再 GREEN；验证跨日统计、历史、恢复冲突/过期/活动计时/最后片段及备份兼容。
2. [x] 统计事实投影：Core StudyAnalytics.Build/DurationText，AnalyticsTests 先 RED，再精确聚合、日均、分组、筛选、秒数/极端值；所有图表消费 AnalyticsSnapshot。
3. [x] 设计系统与组件：ThemeService、Styles.xaml、MetricCard、交互矢量图表/热力/时间轴、详情层与确认；原生 Fluent；实际渲染双主题和空状态。
4. [x] P0/P1 页面：MainViewModel.Presentation.cs、MainWindow 布局与交互；记录筛选/钻取/编辑/软删除/恢复/片段/修订，紧凑今日、月历/热力/时间轴、分析工作台。刷新与输入保留回归。
5. [x] P2 页面：科目/章节树、独立进度掌握、待办、薄弱点筛选/分布/历史、设置分区、主题持久化；键盘焦点和危险按钮。
6. [x] 全部验收：旧 44 回归 + 新一致性；隔离旧版库兼容；实际 WPF 双主题、空/长文本/大量记录、900×600/1200×800/大窗口、125/150/200% 位图、截图目视检查；真实未覆盖明确 NOT TESTED。
7. [ ] 提交与交付：0.1.1 独立自包含目录/ZIP、源码/Git、设计规范、截图与报告/证据、解压程序实际运行/哈希校验；保留旧发布目录，不更改用户数据。

构建测试入口 tools/Invoke-Build.ps1；证据 artifacts/v011-*。发布 releases/YanJi-v0.1.1-win-x64。测试入口 SmokeRunner 扩展新场景；使用新的 data-dir。
