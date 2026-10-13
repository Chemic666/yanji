# 研迹 V0.2.1 Implementation Plan

> **For agentic workers:** 使用 `superpowers:executing-plans` 逐任务在当前分支直接实施。用户明确禁止本阶段代理、重复建分支和再次阶段确认；不要按通用模板启动 subagent 或新 worktree。本文件是已授权交接计划，下一会话直接开始。

**Goal:** 将正式 WPF 六模块与学习记录深化为 A「静桌」暖纸、柔绿、Quiet Focus，通过重新分配空间、信息层级和操作密度提升日常使用，而非仅换颜色、阴影、圆角。

**Architecture:** 保留 MainViewModel 的业务投影、Core/Data、计时与存储调用。以现有 XAML ResourceDictionary、DataTemplate、命名 Grid 和 view-specific code-behind 改造构图；StudyChart 仅改变展示几何，继续传递原始 PlotPoint。新增隔离截图工具以相同 fixture 对比修改前后，操作回归与视觉截图分开。

**Tech Stack:** .NET 10 WPF / Windows；CommunityToolkit.Mvvm、Microsoft.Data.Sqlite；无新增 UI 或图表库。

**Spec:** 本文件第 1–5 节为实施规格；Skill 实际调用、证据和支持边界见 [V021-SKILL-USAGE.md](V021-SKILL-USAGE.md)。状态：`ready-for-medium-implementation`。本 High 阶段没有实现产品改动，也没有运行新版回归。

## 1. 已核实的基线与边界

- cwd：`D:\Projects\yanji`；分支：`codex/v0.2.1-skill-workflow`；分析起点 HEAD：`1d6acc336c11402d076925e9447d34fbf8f6ef58`；开始时 Git 工作区干净。`v0.2.0` 恢复标签存在。
- `git diff v0.2.0 HEAD --` 对 MainWindow.xaml、Styles.xaml、StudyChart.cs、MainWindow.Presentation.cs、MainViewModel.Presentation.cs 无差异。不能把旧报告的 PASS 计入本会话测试。
- 本会话用 `view_image` 实际查看 12 张 V0.2 **native** 文件，包括今日/统计 1200×800 浅深、900×600 两个代表状态、空态、历程/计划/薄弱点/设置。来源：`artifacts/v02-desktop-bc7218132f9c452ca6f4d5a5ec47763a/visual/`。这是既有原生证据，**不是本 High 新抓图，也不是已配对的 V0.2.1 before**。
- 上述 environment.json 报告 Windows 11、DPI 192；PNG 像素尺寸和窗口 DIP 不等同，后续记录 ActualWidth/ActualHeight、DPI 和 HWND 矩形。不修改系统 DPI、时钟、电源、高对比或真实数据库。
- 旧设计实验 `design/YanJi.DesignLab` 不是正式应用。实施、验证与交付对象均为 `src/YanJi.App`。
- 五 Skill 本次从实际注入路径读取并应用；Impeccable context 真执行 exit 0，但 platform=null、hasVisualImplementation=false，不能作为没有 WPF 实现的证据。正式源码与原生截图优先于这项 web-oriented 识别结果。
- 当前没有根目录 PRODUCT.md/DESIGN.md；已有 `docs/V02-DESIGN.md` 等说明。保留用户已指定 A 与现有 Quiet Focus 世界；属于构图深化，不再办方向竞赛、重做访谈或随意替换身份。把本文件的产品事实与设计规格作为本轮明确上下文，不伪造引擎已加载它们。

## 2. 真实约束和可修改点

| 位置 | 当前行为与不变量 | 安全修改范围 |
|---|---|---|
| MainWindow.xaml:1、MainWindow.Presentation.cs:20 | 最小窗口 900×600；1200×800 默认；`Layout_Changed` 以 ActualWidth<1100 同时折栏并将 Today/Calendar/Stats/Aux 第二列移至第二行 | 分离侧栏与页面内容断点；按实际内容区宽度排版，保持命名元素或同步更新唯一视图引用 |
| 响应式结构 | `rg` 在 src/design 找不到 `ResponsivePanel`；当前实现是 Grid + Layout_Changed | 本版继续命名 Grid，不引入通用自定义 Panel。不要把 Skill 示例中的 ResponsivePanel 当成现有类。列、行、控件可见性由单一布局方法负责 |
| MainWindow.xaml:233–300 | TimerSubjects/TimerChapters、TimerActive 禁用输入、Start/PauseResume/Finish 命令；Reflection 多行编辑与保存；RecentTrend 绑定 MiniTrend | 移位、缩短空白、建立 FocusSurface；保留绑定和命令，输入不重新实例化；不创建第二个感想 TextBox |
| MainWindow.xaml.cs:88、MainViewModel.cs:246 | 导航回今日会 ChangeDayAsync；日期切换先 SaveReflectionAsync；感想 dirty 内容受 Refresh 保护；保存失败不应丢失输入 | 所有新导航/更多记录入口复用既有 handler，不绕过异步保存条件 |
| MainViewModel.cs:136–139 | 今日/周/月来自保存的有效 DailyTotals；周一开始；TodayRecords 只取最近 8 条并保留当天贡献；运行中计时另有 TimerText | 不把当前运行秒数直接加到今日已保存投入，不将 8 条摘要冒称全量记录 |
| MainViewModel.Presentation.cs:135 | StatsFrom/To/SubjectId 改变已自动 BuildAnalytics，RecalculateCommand 是重新计算；TrendPeriod 只切换周/月 buckets | 工具栏可改为「刷新统计」与「范围或科目更改后即时更新」，不得创造草稿提交状态或第二套筛选源 |
| StudyAnalytics.cs:8、20–34 | DurationMs 为选定范围全部有效贡献；CalendarDays 为 from 至 min(to,today) 的天数，最低 0；CalendarAverageMs 的分子是 ElapsedDurationMs；LearningAverageMs=整个范围 DurationMs/LearningDays，0 分母返回 0 | 所有指标绑定保持；自然日均不得用完整范围总量作分子；未来范围不擅自裁掉数据 |
| MainViewModel.Presentation.cs:144–173 | 每日叠加子点携 SubjectId+同日 From/To；趋势点含所选范围裁切的 bucket 起止；MiniTrend 为最近七日全部科目 | 图表不得自行按周/月聚合原始每日点，不继承不相关 StatsSubjectId，不从 Label 反解析日期 |
| MainWindow.Presentation.cs:49–59 | Chart_Drill 使用点 From/To，缺省落统计范围；StatsRecords 带当前科目；ChapterDrill 带章节 key；Heatmap_Drill 切换日历；Timeline_Drill 用整次 id 找记录 | 拆图或新 Ranking 仍交付原 PlotPoint；不复用不匹配的 drill handler，不把图的颜色/id 当过滤条件 |
| StudyChart.cs:13–135 | FrameworkElement 原生绘制；_hits 在 OnRender 重建；鼠标 FindLastIndex(FillContains)，箭头/Home/End 在 hit 序列选择，Enter 发 PointInvoked；Tooltip 用 Exact 原始毫秒 | 布局几何与命中区域一起修改，避免仅视觉改了、命中没动；新模式不修改 Data，不延迟 PointInvoked |
| StudyChart.cs:27–46、128–135 | Data 变化 ResetSelection 关闭 hover/keyboard popup；Tooltip style 为 FocusToolTip；Popup 是真实 native surface，RTB 不足以验收 | 保留并补充新模式精度/筛选清旧提示测试；查键盘离开和真正窗口 hover，不能仅调用 InvokePoint 当鼠标验收 |
| MainViewModel.cs:16–29 | RecordRow.Record.Id/Version 为整次身份；Contribution/ScopeFrom/ScopeTo/ScopeKind 是范围投影；DisplayDuration 可带≈；ScopeLabel 保留精确整次与贡献 | 可制作 compact/full 两模板；必须仍明确「本页贡献 / 整次」；不能把每个片段复制成可独立删除的整次行 |
| MainViewModel.Presentation.cs:174–207 | DurationOnly 独立列表，Timeline 仅 Interval；区间位置用每片段 ZoneId 的本地边界，含 DST 分歧处理；ExcludedParts 单独恢复 | 不为手动补录猜起止，不按 24h 固定分母替代现有时区边界，不将已排除片段重新纳入图表 |
| MainWindow.Presentation.cs:60–86、MainWindow.xaml:813 | ShowDetail 保存原焦点；drawer 禁用主页面与导航；Tab/ControlTab Cycle；Escape 关闭再恢复原焦点；整次软删与片段排除是不同动作 | 改宽度、分组、排版；保持禁用背景/焦点循环/取消优先。首焦点稳定指定关闭按钮，列表重建后原控件不可用时回 RecordList，不能陷入背景 |
| EditorDialog.cs:17–52、ConfirmDialog.cs:9–16 | Editor Owner/ShowDialog、IsDefault 保存、IsCancel 取消；首字段获焦；多行 AcceptsReturn；inline 验证保留输入；危险确认默认按钮是取消 | 统一字段/错误/页脚样式、宽度；不拦截 IME Enter，不把多行 Enter 改为提交，不更改保存验证函数；确认不换默认危险按钮 |
| ThemeService.cs:14–52、Styles.xaml | DynamicResource 语义画刷；Changed 刷图；深色持久化有加载 guard；high contrast 用 SystemColors 映射 | 保留语义 key；新表面优先复用 CardBackground/Soft/AccentSoft/Line；如必须新 key，浅深和模拟高对比三路同时定义，不修改系统设置 |
| MainWindow.xaml:791、V02VisualRunner.cs | RecordList 有限 MaxHeight+Recycling；MainScroll 外层滚动；验证依赖控件名、命令及部分可见按钮文字 | 不把记录改成外层无限测量 ItemsControl；保留 full 记录模板的四动作及测试标签，今日 compact 模板可只显示查看/编辑，历史/删除在 drawer 仍可达 |

### 数据精度和图表风险的决策

1. 原始时长始终 long 毫秒。短于一分钟保留秒/毫秒；Compact 的≈继续提示省略精度；完整值进入 Tooltip/详情。不能整数分钟除法、格式化后回算、用同高柱伪造比例。
2. 跨日同一记录，例如前日 10 分、今日 20 分、整次 30 分：今日行显示贡献 20 分并注明整次 30 分。删除影响全部日期；排除只影响对应片段；最后有效片段须整次软删。原 Version、审计历史和恢复路径全保留。
3. 主图仍是每日 **实际时长** 叠加图，轴零点不变；原有时/分/秒自动单位选择保留。本版不引入平滑插值、归一化100%、移动平均或非来源事实的「效率/掌握指数」。长范围继续全部数据+现有键盘路径，不为好看默默丢点。
4. 统计次图采用 **科目排序横条**（新 `Mode="Ranking"`），取现有按总量降序 SubjectPlot；一科目一条，标签+自然时长+占比同组，点击该条原样携科目点。百分比仅是展示 sum>0 时 Value/sum，不能取代实际长度/原始值。保留 Donut 模式供薄弱点「项」分布，不破坏 Unit="项"。
5. Ranking 整行一个非重叠 hit，视觉条只按真实比例；小值仍可点击整行，扩大目标不扩大数据条。0/all-zero 不画假条或假百分比；长标签省略但 Tooltip 与可访问描述包含完整 Label。首屏布局不得依赖增加同科目的多个重复 keyboard hit。
6. 图标与颜色是辅助线索。科目图有直接标签、原始记录替代入口；热力有日期/时长/状态，不因「单一品牌强调色」删除必要分类色。
7. 本版不解决尚未证明的性能问题。近十年逐日叠加很密的可读性是明确限制，保留按周/月节奏与原始记录路径；不得假称所有数据点都有40 DIP点击区或所有设备已验收。

## 3. 构图与共享系统的实施规格

### 3.1 静桌世界

用户主方向 A 优先于 Skill 的营销默认。保留暖纸 Canvas、微暖层级、柔绿动作、墨色正文和低彩度分类；深色为纸墨关系的成对设计。沿用已安装 Segoe UI Variable Text / Segoe UI / Microsoft YaHei UI，不假设商业字体可用，不下载字体。固定 DIP 角色：页标题24/32行高、区域标题16/24、正文14/22、元数据12/18、主指标28、次指标20、计时44；定宽数字，不给每个字做追踪/入场效果。

空间 scale：4/8/12/16/24/32。页 gutter：紧凑20、宽24；区域间16、主分组24；普通 surface padding16，dense12；Card radius12、输入/按钮8、badge4，少用嵌套壳。结构分隔线和焦点边框保留。不要普遍加双层卡壳、纸纹 bitmap、阴影、玻璃、倾斜叠卡或胶囊 CTA。

六模块共享 `SectionSurface`、`DenseSurface`、`SectionTitle`、`MetaText`、`InlineStatus`、`Toolbar`、`RecordItem`、`EmptyState`、字段/页脚/错误样式；它们在现有 Styles.xaml 与 MainWindow.Resources 中实现，避免第二套 styling system。MetricCard 当前没有构图用途，本版不用为了组件清单创建新依赖。

高频 hover/选择/刷新不演出；先保留静态反馈、焦点、40 DIP桌面动作/44 DIP主要动作。若确有按压过渡，仅 Feedback Opacity≤100ms，SystemParameters.ClientAreaAnimation=false 或高对比时即时切换。禁用尺寸动画、数字每秒 tween、所有按钮scale、页面进场动画。本版不需要 motion 新库；10%慢速运动检查仅在实际新增动画时执行并记录，不写「已检查」占位。

### 3.2 今日学习

阅读顺序：日期/标题 → 已保存今日投入 → 当前专注动作 → 今日记录/回顾 → 七日节奏。快速补录与计时不争同一主按钮权重。

- 页头一行，右侧「快速补录」为次动作，保留 Ctrl+N。标题辅助说明不超过一行常态；今日/周/月做紧凑信息带，高约72–88 DIP，今日较大，周/月支持现有点击进入各自范围与精确提示。
- **1200×800**：内容约936 DIP；左右约340 : 580、间距16，顶部对齐。左为「专注」紧凑主 surface（约260–290高，备注关闭时）；紧接同列「每日感想」（输入至少88高，整体约144–160），两者不是同等大卡。右上「今日记录」有界列表约280–320高，右下最近七日约100–120高，节奏始终不被8条记录推到极远。
- **900×600**：侧栏72；内容宽仍足够放左右约320 : 剩余（gap16），不沿用<1100一律纵向。当内容实宽不足700 DIP才整体堆叠（系统边界/未来布局防护）；计时内部可在自身宽<340时把科目/章节纵排。备注折叠时全部计时动作首屏可见；记录至少两条以及更多入口可见；回顾可自然垂直滚到，不通过缩字隐藏。
- 今日使用新的 `CompactStudySessionRow`：第一行科目/章节与贡献时长；第二行来源/状态及必要跨日范围；第三行最多两行备注；最后查看/编辑各40 DIP。跨日整次说明保留；普通同日日期不占独立重复行。既有 full 模板保留记录页四动作，避免本版无谓引入 ContextMenu。
- TodayRecords 本来只有8条摘要。可换有界 ListBox（ItemContainerStyle共用，保留 ItemsSource），不修改集合或排除记录；外层滚动与内层滚动各自明确，仅在记录框焦点/鼠标内滚动，更多记录进入完整页。使用12行以上摘要会违背事实，不做。
- 空态卡随内容自然收缩：「今天还没有已保存记录」＋开始/补录说明，共用现有可见操作，零值为零而非缺省图案；不设巨大空列表 MinHeight。NeedsReview surface在计时区上方/紧邻计时，明确未知时间未计入。

### 3.3 统计与复盘

阅读顺序：范围/科目 → 总投入与两种日均 → 每日实际时长 → 科目构成 → 周月节奏/热力/章节。各图共用同一个已生效范围。

- 页头+工具栏+指标带 **1200首屏累计≤230 DIP、900首屏累计≤240 DIP**（正常短标签 fixture）。保留可见开始/结束/科目标签，preset四按钮一组，输入/刷新另一组。900允许工具栏两行；不要单个「应用」按钮因为Stretch形成大方块。改文字「刷新统计」保持 RecalculateCommand，工具栏说明即时更新。
- 指标带保持三个不同分母：总投入28、已过自然日均20+CalendarDays、学习日均20+LearningDays；学习日与次数放紧凑辅助行。未来范围注释真实，不把分母隐藏进只有hover才能发现的角落。
- **1200**：主图 DailyStack 约60–64%宽、220–240 DIP绘图区；次图 Ranking剩余宽、每行44 DIP、4科目约176绘图区，标题和口径清楚。按内容高度，不强制把次图撑成与主图同高的巨空框。
- **900**：DailyStack 整行、绘图区约200–220 DIP，常态首屏可以看到完整纵轴、横轴、图及口径；Ranking随后自然流动（四科目可两列信息但每科目原始点不变；默认采用单列，避免新增分叉）。不把两个窄图硬挤到一行。
- 第二层「学习节奏」缩为约150–170绘图区，周/月 selector与标题同一工具栏；范围热力和章节清单是第三层支撑证据，可并排在宽窗、窄窗纵向。热力自己的横向滚动保留，禁止把主页面横向滚动打开来掩盖溢出。
- 空统计保留工具栏、0值和分母，一块紧凑说明/记录入口，图的全零高度约80–96，不渲染两张巨大空框。仅改变图空态的 measure，不造测试数据。

### 3.4 其余模块和叠层

| 模块 | 结构深化 | 业务保持 |
|---|---|---|
| 备考历程 | 月历与当天事实宽窗分栏；月历仅按内容自然高度，取消被右侧长文等高拉伸；每个格子的时长可Compact但精确描述可达；当天总量/状态同一行，区间和纯时长明显分组 | 42格、周一开始、SelectedDate、待核对/休息/无记录不同；跨日、本地时区时间轴、每日感想、热力下钻 |
| 计划与进度 | 章节 row约80–104高；科目标题/归档同行、掌握/进度同组；任务面板与树顶部对齐而非等高空大卡；长标题自动换行，取消不必要 MinWidth=280 | 进度和掌握独立、归档保留历史、任务完成不产生学习记录 |
| 薄弱知识点 | 搜索框与selector统一40高和基线；列表占主宽、分布仅辅助；标题/状态/复习日先扫读，困难正文紧接，历史渐进展开；空筛选提示具体 | 同一 WeakSearch/SubjectId/State；状态更新、ReviewDate和修订历史保持，不根据时长自动验收 |
| 设置与数据 | 档案/主题/计时偏好整合为有标题分隔的连续设置区；备份单独操作区，导出/校验一组，恢复危险动作另一组；路径换行，不写自定义真实个人信息 | 导出备份、校验、替换恢复、自动回退副本、CSV和完整备份区别；CheckpointLabel非实时保证 |
| 学习记录 | 共享full row更紧凑、日期/范围/来源组合，不降低40 DIP动作；筛选两行可换行；有界Recycling列表和记录Summary保留 | 每行整次身份、active/deleted/all、科目/章节/source/date筛选及贡献；无删除数据捷径 |
| DetailDrawer/Editor | drawer width=min(620,可用宽-32)，关闭控件稳定命名，内容滚动和底部动作固定；编辑字段标签/输入/错误/保存取消有共享间距；错误不把页脚推出屏幕 | Modal Owner、Tab/ShiftTab cycle、Escape回原焦点、IME/multiline Enter、默认危险取消、修订/排除恢复保留 |

## 4. 文件与实施顺序

### Task 1：建立可比基线（先于UI编辑）

**Files:** Create `src/YanJi.App/V021CaptureRunner.cs`、`tools/Invoke-V021Comparison.ps1`；Modify `src/YanJi.App/App.xaml.cs`、`MainWindow.xaml.cs` 仅显式验证参数路由；参考复用 `V02VisualRunner.cs`、`NativeCapture.cs`，不要更改旧runner事实。

**Interfaces:** 新参数 `--v021-capture <output> --v021-fixture <manifest> --data-dir <new-owned-directory>`，必须三者同时存在。`V021CaptureRunner.RunAsync(MainWindow window, string output, string fixtureManifest)` 只在显式验证路径调用；普通启动完全保持。引入单独 flag，保留原 --v02-visual 非空目录拒绝规则。

- [ ] 首次读取 `.agents/skills/impeccable/reference/craft-floor.md`；本 High 为planning-only没有加载该文件。后续任一UI编辑前必须重新读取，不把本计划当成已读证据。
- [ ] 用既有 build tool以 `--no-restore` 验证可用工具与本地包；不足不自动安装。任务1仅辅助runner编译，不进行UI改造。
- [ ] 写fixture manifest：anchor=当前项目时区 LocalToday、zone=Asia/Shanghai、固定id与CreatedAt、四科目、30天有效分布、纯时长1/1555/59000/60000/61001ms、跨日10+20分、已删除记录、已有排除片段与修订、长中文章节/备注/感想/薄弱点。标记 synthetic；预先计算完整原始记录/片段/范围期望。日期、范围、每张图使用的theme/page/scroll位置都记录。
- [ ] 两个状态集合：空库（默认科目保留，无学习/章节/任务/薄弱点）、已填充长文库。每个native capture用新的owned子目录，统一fixture JSON重放，不复用回归污染后的库。只用公开Database API；排除片段用既有ExcludePart产生真实修订。不手工删用户库，不复制运行中sqlite主文件。
- [ ] runner guard：目标仅项目artifacts子树，owned marker、非正式库、fresh目录、fixture schema合法；anchor不等于当前LocalToday就拒绝配对并保留失败证据，不能调整系统时钟或挪动普通业务LocalToday。保存canonical facts snapshot+expected analytics，与after逐字段比对；审计执行时间不同不伪称所有byte相同。
- [ ] helper只做播种/切页/ThemeService.Apply/设置窗口/idle等待/NativeCapture；不启动真实计时、不QuickAdd、不ExcludePart于抓图之间。主题只做display apply，不反复保存主题造成投影额外刷新。before辅助构建的UI源hash必须与分析HEAD相同。
- [ ] before产出 **7页×2主题×2窗口×2状态=56张native**，包括长文状态；至少主页面top，统计secondary再补捕获。保存窗口实际尺寸、DPI、HWND owner、源码/二进制hash、fixturehash、每图内容和SHA256。before是对照基线，不计after视觉第1轮。

### Task 2：共享资源、密度和响应式职责

**Files:** Modify `Styles.xaml`、`MainWindow.xaml` 的Resources/壳、`MainWindow.Presentation.cs` 的Layout_Changed；`ThemeService.cs` 只有必要新语义key时修改。不改 Core/Data/MainViewModel。

**Interfaces:** 保留所有业务binding/handler/命名chart引用。新Style key为 SectionSurface/DenseSurface/SectionTitle/MetaText/InlineStatus/Toolbar；新增CompactStudySessionRow，full StudySessionRow继续给记录、日历完整列表使用。

- [ ] 用scale替换散落间距并建立角色，避免Card global setter一次缩小所有控件而破坏popup。编辑器/确认的已有 Focus* style继续复用。
- [ ] 侧栏阈值1100可保持，内容布局单独计算 `MainScroll.ActualWidth` 扣Padding与滚动条后的实宽；今日700阈值，统计850阈值，Calendar/Aux按700–760实际最低可用宽单独设置。不在Layout_Changed调用Model.Refresh或DB。
- [ ] 所有结构改变保持视觉阅读与Tab顺序；900反复resize后不丢焦点/感想输入，背后drawer禁用规则不被资源层覆盖。
- [ ] 有界记录ListBox保留Recycling、横向禁用、默认行target≥40、keyboard focus visible；空态无巨大MinHeight。纯布局不写镜像style的unit test，使用后续native layout assertions。
- [ ] build `tools/Invoke-Build.ps1 build YanJi.slnx -c Release --no-restore`，只在新修改/失败时重跑相应检查。

### Task 3：今日构图完整实现

**Files:** Modify `MainWindow.xaml` page0、`MainWindow.Presentation.cs` 的今日列/局部适配；不改 TimerMachine、WindowsClock、VM计时/感想保存。

- [ ] 按3.2实现紧凑页头、指标带、FocusSurface、回顾、compact records、固定节奏区；今日快速补录降次级，开始仍是主要动作。
- [ ] 不销毁正在编辑TextBox，不新建重复Reflection binding，不把TodayRecords集合Take数量改成「今日总次数」。再利用已有 TodayRecords_Click/Metric_Click/Chart_Drill。
- [ ] 新native行为断言加在V021验证扩展：900×600默认备注关闭时开始/暂停/结束边界在可视内容内；1200感想输入和最近七日至少完整可达；compact cross-day row贡献与整次不同且原Id不变；Tab进入有界列表可滚至末条。

### Task 4：统计层级和科目横条

**Files:** Modify `MainWindow.xaml` page4、`StudyChart.cs`、`MainWindow.Presentation.cs` 布局；保留PlotPoint签名、SubjectPlot/DailyPlot/TrendPlot/StatsHeatmap来源。

**Interfaces:** `StudyChart.Mode="Ranking"` 新分支，输入 IReadOnlyList<PlotPoint>，PointInvoked继续 Action<PlotPoint>；其余Mode与Unit API不变。新展示测量高度按count×44，不添加存储属性。现有SubjectDonut字段可保留名称供旧编译引用，但Mode改Ranking；不要因名称不准确扩大业务重构。

- [ ] 排工具栏/指标，移除「待应用」误导，保持RecalculateCommand；按3.3主次图与支持证据排版。
- [ ] Ranking drawing/hits/Tooltip/keyboard同步实现；每行44 DIP、标签与数值空间先预留，条背景Soft，真实条color取PlotPoint；单点、全零、1ms、极大值防除零/NaN/负几何；数据与百分比不持久化。
- [ ] 图尺寸用容器与明确Height/MinHeight角色；仅为该mode与空态改MeasureOverride，不一并缩坏Timeline/Heatmap。PlotRect低于有效尺寸时不画负Rect。
- [ ] 新WPF断言：Ranking各hit携原SubjectId与缺省范围；鼠标与Enter进入同范围同科目，原始records贡献sum=point.Value；换筛选关闭旧原生tooltip；zero无hits/无虚构百分比；长label精确popup；两个viewport主图轴与标签全部在visible clip内。
- [ ] 旧趋势/叠加/热力/Timeline drill检验保留。无需新增复制既有StudyAnalytics算法的test；直接跑既有 AnalyticsTests/TimePartsTests/PartOperationTests/DisplayDurationTests。

### Task 5：全模块与编辑/详情一致性

**Files:** Modify `MainWindow.xaml` pages1/2/3/5/6 + DetailDrawer，`EditorDialog.cs`、`ConfirmDialog.cs` 仅排版/资源与焦点fallback，`MainWindow.Presentation.cs` 焦点处理；可扩展 `V02VisualRunner.cs`或新增专属 `V021VisualRunner.cs`（选择后不写两份同一套测试）。

- [ ] 按3.4完成所有页，内容驱动height而非固定大卡；设置restore动作独立危险组，原说明事实不可删除。
- [ ] drawer命名关闭按钮并保持Tab/ShiftTab/Escape；若原控件因refresh被回收，回到可用RecordList/list item，再fallback页面入口。默认Editor首字段和多行Enter保留。
- [ ] 新WPF防回归仅测新增风险：compact打开drawer后回原可用焦点；更新导致容器回收仍不陷背景；900 long-text底部保存/取消可见可用；多行Enter保留换行、Escape取消、invalid保存不关闭；Ctrl+N在drawer时不打开新补录；full row危险取消依旧不写库。
- [ ] 按修改范围运行现有native回归 `tools/Invoke-V02Validation.ps1 -Executable <本次构建的exe>`；新的断言与旧计数分开保存，失败原样保留，禁止修改测试降低语义要求。

### Task 6：两轮有界视觉检查与交付门禁

**Files:** docs本两文件阶段状态、后续验证说明、`artifacts/v021-completion.json`；后续打包仅在实现和验证结束之后。

- [ ] 核心/数据：`tools/Invoke-Build.ps1 test tests/YanJi.Tests/YanJi.Tests.csproj -c Release --no-restore --logger "trx;LogFileName=v021.trx" --results-directory artifacts/v021-tests`。记录实际测试数，不继承旧70/90 PASS。
- [ ] **after第1轮**：完整实现后一次批量same fixture 56native + selected popup/drawer/editor；用view_image打开每个必验图，分组对比before，不每改一个padding抓一次。报告读序/密度/contrast/空长文/轴/命中/焦点缺陷。
- [ ] **一次修复批次**：合并第1轮所有实质问题，按源码风险补适当断言，build一次、必要回归一次。不做独立无限「微调」脚本。
- [ ] **after第2轮**：相同fixturehash/state/page/theme/实际window矩阵确认；统计每项resolved/partial/unresolved。至多两轮视觉审查，之后停止外观打磨。阻塞项保留失败状态，不能因预算已用完把未过门禁写PASS或ready-for-release；技术修复的必要验证不伪装第三轮美化。
- [ ] 完成标准：900常态计时全部动作可见；900统计完整主图轴/plot/口径可见；1200今日回顾+节奏与至少3条compact记录可用；1200统计主次一眼区分，次图不以空高度争权重；所有页空态自然收缩，无主页面横向溢出；长中文完整可查看；浅深正文/元数据contrast达到4.5:1，焦点与状态可辨；不可用动作真实禁用；overlay和popup不被clip；所有旧业务及新增断言无FAIL。
- [ ] before/after canonical事实相同（学习、片段、排除、修订、任务等；审计时间与capture派生文件按清单排除），不更改真实资料，不以软件渲染验证宣称GPU/设备DPI/真实IME/睡眠/高对比全部通过。
- [ ] 后续交付如需V0.2.1 ZIP，沿用现有Package-V02能力更新明确版本身份，禁止覆盖v0.2.0恢复包；独立owned解压验证再写完成JSON。该打包/验收本 High 均not-run；本计划提交仅docs，不推送。

## 5. 接受与拒绝建议

| 候选 | 决策和理由 |
|---|---|
| 排序科目横条、紧凑真实时长、共享toolbar/行/状态、侧栏与内容断点分离 | 接受：直接改善比較、首屏操作与不同内容的空间分配，属于view projection |
| 调整字体角色、定宽数字、低彩度暖纸绿、内容高卡片 | 接受：现有WPF资源表达，不下载商用字体，不另建CSS/React |
| 自动化focus/tooltip/真实native配对，新增少量风险断言 | 接受：新几何与响应布局有实际可复现风险；既有数据库测试继续守事实 |
| 高端Skill的全卡双壳、全胶囊、py-24、首屏800ms入场、磁吸按钮、玻璃noise | 拒绝：用户明确密度/Quiet Focus与Operate优先，WPF读秒/列表高频操作不能做营销演出 |
| ui-ux搜索的Product Demo+Features、hero video、橙CTA、远程Plus Jakarta字体 | 拒绝：两次design-system检索仍误配营销模板；不把未验证平台匹配的推荐持久化为MASTER |
| 搜索结果的Radar或新Web图表库 | 拒绝：首次chart top result错类型，已窄化重试得到Trend Over Time；保留WPF真实日叠加与原生几何 |
| 重做MVVM/全部PreviewKeyDown改InputBindings、删除侧栏、新通用ResponsivePanel | 拒绝：当前已验证交互，无本轮需求，扩大状态/焦点风险 |
| 未测即写速度提高、效率/掌握评分、物理IME/多设备PASS | 拒绝：没有来源事实与真实验收，不能用视觉变化推出这些结论 |

**Review Focus:** 范围含未来日期的两种日均；跨午夜整次身份/单片段恢复；1ms/单科目/全零图；900长中文和native popup焦点；resize/刷新回收列表后的drawer焦点。这五项分别由Task3–5的已有或新增行为断言验证，完整场景仍以native矩阵检查。

本计划完成关键约束与方向决策，剩余是明确实现和有界验证。可以切回 Medium，监督会话立即实施，不等待阶段确认。

## 恢复实施 ledger

从 bfe616e 和未提交捕获入口恢复，保持同一分支；没有重做High分析、Skill安装或代理分工。当前五Skill实际匹配、context、搜索与采用矩阵见 V021-SKILL-USAGE.md 的恢复实施段。

| Task | 实际实施与门禁 |
|---|---|
| 1 | 修复捕获器显式using后成功抓取60张before；固定合成fixture，输出owned标记、raw记录、metadata；未重复入口 |
| 2 | 共享Section/Dense/Meta/Toolbar资源与独立内容断点，保留Grid/虚拟化/业务绑定 |
| 3 | 今日指标带、计时/回顾与有界compact三动作、固定七日绘图区；900三动作可见性与焦点回原处有新增断言 |
| 4 | 统计立即生效工具栏、分段自然时长、排序横条与主次图；原范围/科目/毫秒保持；鼠标/Enter/zero/extreme/native popup均执行 |
| 5 | 历程自然高、章节约束、薄弱点筛选、连续设置与恢复危险分组、详情命名关闭和fallback；旧编辑/危险取消/片段回归保留 |
| 6 | 70核心测试、原生扩展门禁、配对图/事实与升级兼容、4性能样本；最终发布计数/提交/包路径以completion.json为准 |

Ruling: 旧900强制今日纵排断言更新为批准的双栏契约，同时保留折叠导航、统计纵排与新增真实边界断言；业务断言不减弱。

Ruling: 两轮外观检查后只继续已观察失败的技术门禁修复（容器测量clip、口径首屏、搜索实际40DIP）；保留失败尝试，停止审美微调。MeasureOverride遵守available.Height，而不是仅继续缩小字号或改测试。

Ruling: 数据库自动生成CreatedAt/审计时间和修订ID，比较时明确排除这些生成字段，核对全部其余表事实；不伪称canonical byte相同。before未单独保存EXE哈希，列为证据限制，不补造。

范围边界：未进行物理IME/Narrator、真实系统电源/时间/DPI/高对比变更、干净机断网、全帧GPU或十年全部点目标大小验证。没有将这些checkbox标为PASS；详情刷新回收后fallback为源码实现，新增断言只覆盖普通compact开关焦点，复杂回收路径未另做物理输入验收。
