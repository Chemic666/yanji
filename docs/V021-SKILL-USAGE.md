# 研迹 V0.2.1 Skill 实际使用与关键分析

状态：**下方旧 High 记录保留为历史；当前 D 工作区恢复会话已实施正式 WPF 改版并执行新回归。** 当前证据以文末「恢复实施会话」和 artifacts/v021-implementation/current-skill-match.json 为准，打包实际结果见 artifacts/v021-completion.json；不借旧会话冒充当前运行时加载。

## 当前运行时匹配与原始证据

当前 available-skills 注入的 r12 根是 `D:/Projects/yanji/.agents/skills`；用户又提供了五个完整 Skill 及以下绝对路径。磁盘内容通过当前会话 `functions.exec → tools.exec_command → Get-Content` 读取，hash与正文副本保存在 `artifacts/v021-high-analysis/loaded-evidence.json`、`guidance/`。这是工具读取和本模型执行指导的证据；三个纯文本 Skill 本来没有独立 CLI，不虚构它们有引擎进程或另一种 Skill RPC。

| 真正注入 name | 实际磁盘 SKILL.md | 本会话实际使用 | 状态 |
|---|---|---|---|
| ui-ux-pro-max | `.agents/skills/ui-ux-pro-max/SKILL.md` | 6次真实Python搜索；design-system误配筛选；WPF keyboard规则、focus语义、chart重试；据此制定native方案 | guidance+search executed；推荐已判适用性；产品未实现 |
| redesign-existing-projects | `.agents/skills/redesign-skill/SKILL.md` | 扫正式WPF/XAML/ResourceDictionary；看native浅深，诊断信息密度、等权卡与弱层级；制定针对性升级 | Scan/Diagnose executed；Fix planned |
| high-end-visual-design | `.agents/skills/soft-skill/SKILL.md` | 选择Editorial暖纸/柔绿与不等权分栏作为材料和构图参考；逐项裁决营销指令与Quiet Focus/Operate冲突 | model-guided direction applied；无UI代码输出 |
| impeccable | `.agents/skills/impeccable/SKILL.md` | 真实launcher context一次；critique-storage slug一次；读取Operate/critique/layout/typeset/native audit及init/new-work必要指导；完成受限原生审查 | context+slug engine executed；设计/技术审查model-guided；无完整native引擎验收 |
| make-interfaces-feel-better | `.agents/skills/make-interfaces-feel-better/SKILL.md` | full模式、五类源码/状态覆盖；定宽数、层级、40DIP目标、focus/tooltip、motion restraint和性能边界的审查 | full review executed with boundaries；changes proposed |

未重装任何 Skill，未启动代理，未重复建分支，未注册hook，未安装工具或字体。使用了 `using-superpowers`、`brainstorming`、`writing-plans` 的流程指导；等待审批/执行方式选择被用户“只完成关键分析、直接交接、不等待确认、不启动代理”的明确指令覆盖。

## 真正的引擎与搜索执行

所有下列命令cwd为 `D:\Projects\yanji`；result JSON与stdout/stderr分别原样留存，不能只留一个成功摘要。

| 执行 | 数量 | exit | 输出/判断 |
|---|---:|---|---|
| launcher `context --target src/YanJi.App/MainWindow.xaml` | 1 | 0 | `context.*`；确实找到目标，但platform=null、hasVisualImplementation=false；NO_PRODUCT_MD/PRODUCT_INIT_REQUIRED、无hook、未发现image转换器 |
| launcher `critique-storage slug src/YanJi.App/MainWindow.xaml` | 1 | 0 | `critique-slug.*`；目标slug为 `src-yanji-app-mainwindow-xaml`，仅稳定目标解析，不是critique自动评分 |
| ui-ux `study productivity quiet dashboard --design-system --density 8 --variance 4 --json` | 1 | 0 | `design-system-initial.*`；product=Productivity Tool，但pattern=Product Demo+Features，是营销结构 |
| 更窄 `desktop productivity dashboard --design-system …` | 1 | 0 | `design-system-retry.*`；仍为同一营销pattern；方向级推荐未验证匹配，不能持久化成MASTER |
| `keyboard focus controls --stack wpf` | 1 | 0 | `wpf-controls.*`；top=Support keyboard navigation，Found=3；采用Tab/Cycle与view-specific焦点，不按一般建议重写现有PreviewKeyDown |
| `focus not obscured --domain ux` | 1 | 0 | `focus-outcome.*`；Found=3；web AA/AAA语义作为设计检查参考，非WPF规范认证 |
| `time series comparison --domain chart` | 1 | 0 | `chart-comparison.*`；top=Radar，任务不适合，不能直接采纳 |
| 窄化一次 `trend time series --domain chart` | 1 | 0 | `chart-retry.*`；top=Trend Over Time，采用时序/直接label/数据替代路径；Web库和通用“少于4点不用图”不覆盖既有单点真实WPF图 |
| 自有 `python …/measure-contrast.py` | 1 | 0 | `contrast.*`；读取当前ThemeService的20组浅深token配对，20检查、0低于4.5；不是native像素、透明叠层、禁用态或高对比的整体认证 |

**已跟踪Skill/分析子进程总数：9；非零退出：0。** 搜索语义不匹配有3次（两次design-system及首次chart），已保留原始输出；这不同于执行失败。操作检索另有不存在design-system目录/三个猜测provenance路径的rg错误；无业务影响，未假造缺失文件，改用实际existing environment.json、v0.2.0源diff。memory检索无研迹匹配，未使用历史memory事实。

二进制：`artifacts/v021-engine-supervisor/impeccable-windows-x64.exe`，0.1.14；本会话重新检查SHA256=`38E09451694973BA664A24E67E1502A6913E6D0E533A0A5AE1E04C5963A804B1`，与官方sidecar相同；Authenticode=Valid、Renaissance Geek, Inc.、有时间戳。原supervisor provenance保留不改（它描述“当时未执行”），新baseline/result描述本会话执行。

两次launcher均在各自实际PowerShell进程设置：

```text
IMPECCABLE_BIN=D:\Projects\yanji\artifacts\v021-engine-supervisor\impeccable-windows-x64.exe
IMPECCABLE_HOME=D:\Projects\yanji\artifacts\v021-high-analysis\engine-cache
IMPECCABLE_SKILL_DIR=D:\Projects\yanji\.agents\skills\impeccable
```

### 支持边界与未运行项

- Context只做查找/路由。它没有读取 `docs/V02-DESIGN.md`作为根DESIGN，亦没识别WPF平台；模型直接读正式代码、已有项目文档与截图，不把null当项目无UI。承认引擎上下文未含完整原生指南。
- Operate、layout、typeset是本会话读取playbook后 **模型执行** 的评估/计划，不是把这些词传二进制就声称自动设计成功。
- `audit.native.md`明确面向iOS/Android/adaptive，要求源码审查且无CSS detector。WPF另有UI Automation、DIP、资源字典、native popup/窗口语义。本次借五维审查框架并按WPF源码映射，不能宣称HIG/Material/WPF官方认证。
- **没有执行detect/layout scope/type scope CSS扫描**。对原生XAML运行web detector不能证明native质量；native flow的跳过优先于web通用提示。不得把“0 CSS findings”填为WPF PASS。
- **craft-floor：deferred-before-ui-edit**。其SKILL明确“Do not load it for planning-only work”；本阶段无UI编辑，已把Medium首次任一UI写入前读取实际文件列为Task1第一步。不是已执行craft-floor gate。
- init/new-work：必要指导已读；用户明确A/Operate方向和自主细节已取代未决访谈。已有世界被正式实现与native截图证明，不新建方向竞赛、serve-question、root PRODUCT/DESIGN或随机concept seed。本文件没有声称这些引擎流程已跑。
- 原生pointer移动、Narrator、真实IME/系统设置、after截图、build/tests、performance/package本阶段均 **not-run**。view_image是在当前会话查看旧native文件，不是当前重新操作应用。

## Impeccable critique（限定原生分析）

⚠️ DEGRADED: single-context (sub-agents explicitly declined by user; native WPF outside detector/platform presets).

评估A先根据正式源码和native图判断特定性/认知负荷；评估B继而核对真实投影、源码hash、既有capture环境与20组contrast。二者共享本上下文，**不具独立性**。本次是manual adapted critique，不是满足全部web critique detector/overlay条件的成功运行；目标slug由引擎生成。

特定性判断：Quiet Focus暖纸/绿与中文离线学习语境成立，计时、回顾、跨日和恢复不是通用展示页；但上半屏大指标+同重卡片令日常频繁动作变慢，统计图分析密度不够。改进的是阅读顺序与空间职责，无品牌推倒重来。

| Nielsen项（模型/source＋图判断） | 分数/4 | 依据/限制 |
|---|---:|---|
| 系统状态 | 3 | TimerStateText/Busy/NeedsReview现有，页底Status需避免截断遗漏 |
| 现实语义 | 4 | 自然时长、纯时长无位置、整次/贡献明确，保留 |
| 用户控制 | 3 | 取消/软删/恢复/片段排除源码完整，本会话未点击验证 |
| 一致性 | 3 | Focus*资源与定宽数字已有，页面密度/搜索高度不一致 |
| 防错 | 3 | 默认危险取消、Version/备份规则、inline校验存在 |
| 识别优于记忆 | 3 | 可见label良好，窄栏缺显式Automation name需要补审 |
| 效率 | 2 | 900今日计时下缘、统计图大部在首屏外；重复记录元信息 |
| 简洁与审美 | 2 | 结构层级与卡片等权、空统计两个大框；改善重点 |
| 错误恢复 | 3 | 存储失败/输入保留/历史/恢复源码存在；动态长文待验证 |
| 帮助说明 | 2 | 数据口径清楚，但散落说明与重复长文占空间 |
| 合计 | **28/40** | 规划诊断评分，不是引擎分数、交付gate或用户研究 |

认知负荷：分组、可见口径、取消可返回是优势；首屏主次层级、重复行动作与高卡密度是三个待改善因素（中度问题）。7目的地不为了通用“≤5”规则强行删模块，六模块+记录真实信息架构已授权。新手需看见「无记录≠休息」，键盘用户需完整焦点路径，长文用户需drawer全文和可靠保存；不虚构目标年龄或真实耗时提升。

优先问题：P1窄窗计时动作可见性；P1图表范围/整次语义在重新排版时防回归；P2统计toolbar/指标过高；P2今日记录重复密度；P2统计空框与同等卡权重。方案和精确文件在PLAN第2–4节。

Run notes：ignore.md未发现既有输入；CLI detector/native browser overlay/live-server均未启动，因为不是HTML/browser surface；无server需清理；临时证据保留artifacts；无截图误称实时交互。Questions skipped: 用户明确关键分析结束直接交接，不等待阶段确认。

## Layout / typeset 流程实际应用

| 问题 | 已检查证据 | 本次形成的决定 |
|---|---|---|
| 读序/分组/节奏/密度 | 今日及统计浅深native、900图、page0/4 Grid布局 | 缩短工具栏与指标；今日专注/回顾左列、摘要/节奏右列；主图实际时长、次图横条 |
| 内容极端 | 空今日/空统计、日历长备注native、计划/薄弱点/设置native | 空态自然收缩；长文不逼同列面板等高；记录摘要与完整事实分工 |
| 适配 | Layout_Changed阈值1100，同时更改侧栏和页面拓扑；无ResponsivePanel类 | 侧栏/内容断点分开；依据容器最低可用宽布局；900今日仍可分栏，900统计主图整行 |
| 类型权威/层级 | Styles.xaml Heading26、Subheading17、Muted12、Number28，Timer已有Tabular；chart另用FormattedText | 保留native中文字体，统一24/16/14/12角色；数值适当缩而非Viewbox无底线缩小；chart文字与页面角色协调 |
| 阅读/压力/投递 | 长中文/路径、Editor多行、部分表格MinWidth、现有软件native证据 | 正文换行、摘要最多两行+全文drawer；保留字体fallback、避免新asset依赖；DPI不模拟冒称实设备 |
| 机械扫描 | WPF/XAML不在web detector覆盖 | model源码/native检查；scan not-run，完成计划不等于UI gate通过 |

## Native audit（WPF适配、源码范围）

| 五维 | 诊断分/4 | 已检查与仍未验证 |
|---|---:|---|
| 可访问性 | 2 | AutomationProperties、图表help、drawer Cycle与editor focus；窄栏label/name及真实Narrator顺序需实施检查；没有screen-reader实测 |
| 性能 | 3 | full RecordList有界Recycling、chart无外库；未测新版性能，Today可有界摘要；不从旧性能数据推出新性能 |
| 主题 | 3 | 20 opaque token对contrast通过；DynamicResource、Changed/HighContrast映射存在；popup/disabled/透明scrim未pixel检查 |
| 原生习惯 | 3 | Window/Owner/ShowDialog/native selector和Popup；没有浏览器套壳；不能把mobile safe-area规范强套WPF |
| 窗口适配 | 2 | 900–1200结构和旧图已查，当前<1100一律纵向损失空间；其他DPI/屏幕未验 |
| 合计 | **13/20** | 模型受限诊断；不是完整native审计PASS |

## make-interfaces-feel-better：full review

Scope：正式WPF MainWindow七页、Styles、StudyChart、EditorDialog/ConfirmDialog/DetailDrawer、ThemeService；现有XAML ResourceDictionary/DP/绘制几何。今日/统计有浅深截图，其余页只有表中列出的代表主题；没有实际motion replay、GUI事件或assistive technology操作。不能声称所有状态都走过。

| Category | 实际证据 | Result |
|---|---|---|
| Typography | Styles角色/Tabular、StudyChart.FormattedText、native今日/统计/长中文 | 2类建议；定宽数字已具备，不重复发明 |
| Surfaces | Card资源、page grids、StudySessionRow、空态、900native | 3类建议；结构先于颜色/阴影 |
| Animations | FocusButton Trigger即时Feedback，源码无定制page tween | restraint决定1项；10%motion replay **Not reviewed**，本版无已实现新motion |
| Icons | FocusIcon自绘、绑定Stroke、导航文字/ToolTip | 保留同族；窄栏可访问名待增强，未逐icon实测所有DPI |
| Performance | bounded RecordList、Today ItemsControl至多8条、StudyChart measure/render | 有界摘要/避免新effect；无本次CPU/FPS验证 |

以下为提出的全部系统性变更组（对应PLAN，不是已写UI）：

| Severity | Location | Before | After（计划） | Why |
|---|---|---|---|---|
| HIGH | MainWindow.Presentation.cs:20；MainWindow.xaml:233 | 900统一纵排，计时action在可视下缘 | 今日保持可用分栏、紧凑计时与安全容器断点 | 可视动作/焦点不被布局遮挡；需new native bounds断言确认 |
| MEDIUM | MainWindow.xaml:589；MainWindow.Presentation.cs:20 | toolbar/大指标占半屏，stats两图同重 | toolbar分组、指标密度与分母常驻；每日主图/Ranking次图 | 分组与视觉主次，真实数据比较 |
| MEDIUM | MainWindow.xaml:67、279、791 | 每记录多行日期/来源/4action；Today8条无限拉长 | compact今日摘要2action/full记录保留；有界列表/独立节奏 | 高密度与40DIPhit不冲突，保持业务可达 |
| MEDIUM | MainWindow.xaml:31、628；StudyChart.cs:39、49 | 全零图大固定高度/卡框 | 空说明自然高/空图80–96；零不虚构比例 | 内容驱动surface，减少无谓空白 |
| MEDIUM | MainWindow.xaml:437、545、708 | 章节较高、薄弱筛选不等高、设置分成多大卡 | 共享行/toolbar/连续设置分组；备份危险动作分离 | 统一组件词汇、optical基线与认知分组 |
| MEDIUM | StudyChart.cs:32、49、120 | 固定donut图例布局；几何与hit相关 | Ranking真实比例/44DIP行hit/完整label描述，保留其他mode与Tooltip reset | 小值目标可用，不伪造数据；精度和键盘路径 |
| MEDIUM | EditorDialog.cs:20；MainWindow.xaml:813；MainWindow.Presentation.cs:60 | drawer620固定、恢复原焦点可能来自回收行 | 适配可用宽、关闭命名/可用焦点fallback；错误页脚可达 | focus稳定、native overlay与表单键盘 |
| LOW | Styles.xaml:32–64；StudyChart.cs:46 | 文本角色局部散落；已有Tabular | 统一24/16/14/12、chart角色，保留系统fallback与Tabular | 稳定字阶和长文阅读，不乱加商用字体 |
| LOW | Styles.xaml:87；FocusIcon.cs；MainWindow.xaml:164 | hover/pressed静态，窄栏依赖ToolTip/隐藏label | 保留静态cue与同族图标，补显式Automation Name；最多100ms opacity（确有需要才加） | motion restraint/状态反馈与可访问目标；无scale演出 |

Considered but Rejected：全卡增阴影/双层壳；全按钮0.96缩放；商业字体替换/首屏入场；重写PreviewKeyDown或新增通用Panel。分别因Operate密度、高频注意成本、native/依赖边界、无业务需求增加风险被拒绝；详见PLAN第5节。

Verification：实际commands为上表9项；source读取、v0.2.0 UI diff、12次view_image、token pair计算已执行。原生hover/click/Tab循环/resize/编辑与保存/after matrix、10%动画回放、渲染pixel contrast均 **Not verified this session**。Review verdict：**Block for UI delivery**（HIGH布局问题及native新行为未验）；**Ready for Medium implementation**（关键分析/可执行计划已完成）。二者不混用。

## 阶段结果和下一会话入口

1. 当前只产生计划、审查说明及独立raw evidence；`src/`、计时、SQLite产品代码未改。
2. 第一动作读craft-floor；然后PLAN Task1建立same-fixture fresh before，完整实现Task2–5后一次after第1轮、集中修复、最多after第2轮。before和本次旧图阅读不冒充after迭代。
3. completion JSON更新为`ready-for-medium-implementation`，保留before/after fresh数组空值、tests0、packages空、delivery not-run；本次已看旧图独立放inspectedV02，不写为fresh before。
4. 本阶段可提交两份docs（本地，不推送）；raw artifacts按既有.gitignore保留于磁盘，不强制加入二进制/真实数据库。

可以切回 Medium；无需再次阶段确认。

## 恢复实施会话：当前真实匹配

本会话 cwd=D:\Projects\yanji；available-skills 的 r12 根与用户五次完整 Skill 注入一致。当前 functions.exec/tools.exec_command 对五个 SKILL.md 实际 Get-Content，并匹配 name/path 与 SHA256，记录 artifacts/v021-implementation/current-skill-match.json。没有 skills/list 探针、安装、字体下载、子代理或其他聊天通信。使用 executing-plans 与 verification-before-completion 流程指导；用户已完成分析和明确执行授权覆盖再次访谈/阶段审批。

| Skill | 运行时匹配 | 指导读取 | 引擎/工具执行 | 代码落实 | 原生验证 |
|---|---|---|---|---|---|
| ui-ux-pro-max | 当前 catalog+完整用户注入+磁盘 hash | 当前正文；quick-reference/pro-rules，区分 mobile 范围 | 当前 Python `keyboard focus controls --stack wpf`，exit0，3条，top Support keyboard navigation | XAML Tab/focus、目标尺寸、原始图点下钻与WPF资源 | 原生操作检查；不等于完整Narrator认证 |
| redesign-existing-projects | 当前同路径匹配 | 当前正文；沿用已批准 Diagnose | 文本 Skill，无独立引擎；源码+实际 before/after | 六页面构图、列表密度、统一表面、设置连续区 | 原生浅深、900/1200、空与长文 |
| high-end-visual-design | 当前同路径匹配 | 当前正文 | 文本 Skill，无独立引擎 | 暖纸/柔绿材料与不等权分栏；固定 brief 优先 | 直接观察原生图，不声称营销模板合规 |
| impeccable | 当前同路径匹配 | 当前正文；init/operate/critique/audit.native/layout/typeset/polish/craft-floor | 本会话 launcher `context --target src/YanJi.App/MainWindow.xaml` 一次，exit0，context-current.txt/stderr；实际加载 PRODUCT/DESIGN | Operate 层级、native适配、测量与口径修复、有限审查 | 模型 guided critique/audit/polish + WPF验收；不是引擎 WPF PASS |
| make-interfaces-feel-better | 当前同路径匹配 | 当前正文；typography/surfaces | 文本 Skill，无独立引擎 | 定宽数字/单位 Run、40DIP动作、焦点、静态反馈、虚拟化 | full范围的五类证据，见下表 |

### 安全实际执行摘录

- `tools/Invoke-Build.ps1 -Arguments @('build','YanJi.slnx','-c','Release','--no-restore','-m:1','-nr:false','-p:UseSharedCompilation=false')`：构建 exit0、0警告0错误；失败尝试另存日志，不把失败当PASS。
- `.agents/skills/impeccable/scripts/impeccable.cmd context --target src/YanJi.App/MainWindow.xaml`：进程 IMPECCABLE_BIN 指向已核验官方签名/hash二进制，IMPECCABLE_HOME=项目 artifacts；exit0，PRODUCT/DESIGN 与 surface 已加载。没有全局安装或未知二进制执行。
- `python .agents/skills/ui-ux-pro-max/scripts/search.py 'keyboard focus controls' --stack wpf`：Found3，Source stacks/wpf.csv；采纳键盘导航和仅视图逻辑 code-behind，未改业务模型。
- 新执行核心测试70项通过；最终原生计数、发布程序回归和包装验证以 V021-VALIDATION.md、completion.json 的实际文件为准。

引擎仍报告 Platform 值不在 web/ios/android/adaptive enum 中，并回退 web；hasVisualImplementation 对 XAML 不可靠。明确拒绝改填另一个原生平台，不运行 web detect/live/DOM/CSS 或 iOS/Android 真机流程冒充WPF。context 的 drift 提示不作为旁路修复或第二轮设计访谈；Operate 继续 code-first。当前设置不是完整 WPF engine context 支持。

### Critique / audit / polish 来源与限制

⚠️ DEGRADED: single-context（本轮执行未采用独立评审代理，并非用户明确禁止子代理）。设计审查与技术核查在同一工作会话内依次完成，不独立；Questions skipped: 用户已批准完整计划并明确无需阶段确认。

首轮批量56页面+4详情；第二轮确认后，额外技术复验只处理已观察到的 MeasureOverride 裁切、口径文字首屏失败和工具栏输入高度，保留所有失败。没有第三个视觉方向或无限微调；最终停止外观打磨。before与final配对图及 comparison.html 依赖全部本地。

| full 类别 | 实際检查证据 | 结果/边界 |
|---|---|---|
| Typography | Styles.xaml、NaturalDuration.cs、今日/统计自然时长、长中文，原始精度断言 | 数字/单位层级与毫秒保留；物理IME未测 |
| Surfaces | 六页面+明细、drawer浅深/两窗口/空长文；接触目标/无主横溢出断言 | 原生窗口观察与视口门禁；其他DPI未测 |
| Animations | 未新增自定义动画；现有静态hover/focus/pressed/disabled | 10%慢速运动检查不适用，没有虚构浏览器动画面板验收 |
| Icons | 既有WPF vector导航与状态、颜色辅助、可见按钮与AutomationName | 无新图标库；Narrator实际读序未测 |
| Performance | Recycling记录列表、图表几何、默认渲染四样本 | 真实采样文件；不声称所有帧率/设备覆盖 |

| Severity | Location | Before | After | Why |
|---|---|---|---|---|
| HIGH | MainWindow.Presentation.cs Layout_Changed / MainWindow.xaml 今日 | 900一律堆叠、动作在折下首屏不可见 | 导航与内容阈值分离；双栏计时、输入按自身宽度排版 | 操作可达性，实际视口边界验证 |
| MEDIUM | NaturalDuration.cs / MainWindow.xaml 指标 | 全串数字/单位同权重、粗大时长 | 原毫秒生成分段Run、单位55%、精确可访问名 | 层级和定宽数字稳定性 |
| HIGH | StudyChart.cs MeasureOverride | MaxHeight限制下仍返回220，轴被clip | 测量服从available.Height，命中与渲染使用同一实际尺寸 | 保留完整轴与真实几何 |
| MEDIUM | MainWindow.xaml 统计 / StudyChart Ranking | 等权大饼、工具栏“应用范围”暗示提交 | 主次图、44DIP横条、即时更新/刷新说明 | 比较扫描与准确反馈 |
| MEDIUM | MainWindow.xaml 今日模板/设置/计划/详情 | 高密度 full行挤掉节奏、等高或多张设置卡、未知关闭焦点 | 有界compact三动作、连续设置、自然高度、命名关闭和fallback | 密度、空间分配、焦点恢复 |

采用：共享原生资源、直接标签与精确Tooltip、保留结构/焦点线、语义浅深 token。拒绝：全卡双壳/噪点/玻璃；全按钮scale与首屏入场；字体下载和第二样式系统；用Web或移动平台流程代替WPF；为扩目标放大真实小值柱。

本范围Verdict：Approve（本轮已执行范围，122项原生检查与70核心测试通过）；未验证边界始终包含物理IME、Narrator、真实系统DPI/高对比/电源切换、干净机断网与全部GPU帧时间。没有宣称完整平台认证。
