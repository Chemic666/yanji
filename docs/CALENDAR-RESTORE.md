# YanJi RC-Calendar.1 · 学习数据与图标收尾

这是本地隔离候选，不是公开发行或真实数据升级。解压 Windows ZIP 后运行 YanJi.exe；默认数据位于程序旁 data-rc-calendar，不使用真实 LocalAppData 学习库。请在可写目录解压，不覆盖旧程序目录。

## 使用方式与界面

- 今日保持计时与补录，学习进度保持章节进度；三个主要导航不变。
- 学习数据首先展示今日、本周、本月，再展示当前月份日历。上月、下月、今天可直接操作；“今”与边框表示今天，填充表示选择，时长表示已确认学习，待核对与休息有文字提示。
- 点击日期直接查看当天科目、章节和有效贡献。跨日保留整次记录身份；“查看”进入原有详情、修订和片段操作。纯时长补录没有假定的时间位置。当天时间轴、已删除记录和每日感想保留在就近选项。
- 趋势、科目分布、章节投入、范围及年度热力、阶段复盘和回顾材料直接按分区展示，顶部文字入口可快速定位。只有自定义筛选与统计口径仍折叠。历史学习记录在页首直接进入。
- 日历月份与分析日期范围独立；两处均标注当前范围。图表仍支持悬停与原始记录下钻。

## 实现边界与刷新

复用原 CalendarDay、StudySessionRow 的记录语义、StudyChart、OpenDetail/OpenRecords 与现有统计引擎。没有修改 Core/Data、Schema4、SQL 迁移、稳定节点 ID、备份、知识包或计时事实。

日期/月切换使用最近一次成功数据刷新所得的有效记录与感想快照，不重复读取完整 SQLite 记录。成功补录、修正、删除、恢复和片段操作仍经 RunAsync → Refresh 更新事实快照及图表。保存感想后同步更新缓存；切换日期前先保存，失败保留输入。

15 秒保存继续在后台执行，汇总在暂停、结束或数据变化时更新。没有引入新的定时刷新。月历只创建 42 个日期项。900×600 为上下布局；宽窗口为日历与当天记录双栏。

## 图标与版本

图标独立提交 31fe8eef6d8581fc31637f36ad34c2456ee55ad4。源图 design/branding/yanji-icon-v1.png 原样保留；src/YanJi/Assets/Icons/YanJi.ico 含 16/24/32/48/64/128/256 七帧，PNG 为256方形。

EXE 使用 ApplicationIcon，主窗口使用内嵌 pack URI，NotifyIcon 从同一 ICO 取小图标。发布校验逐帧比较 PE 资源；源码 ZIP 明确校验原设计 PNG、ICO、标准 PNG 及生成脚本均与 Git 一致。不新增安装器或清理图标缓存。

本说明之前 APP-ICON-INTEGRATION.md 记录的是第一次未提交候选，保留为历史证据；当前提交及包哈希以本轮 release-manifest.json/BUILD-INFO 为准。旧 RC-Polish.2 恢复点及所有历史 ZIP 不变。

## 验证和限制

使用 --calendar-preview / --calendar-check 与新的项目 artifacts 隔离库。前者执行受影响的数据与窗口检查；后者增加至少125秒原生WPF实时采样、主迷你同步、暂停排除及多轮检查点观察。原始 checks、performance、timer-samples、screenshots、provenance 保留，最终结果另见 CALENDAR-VALIDATION.md 和候选审核包。

截图为真实 WPF HWND，实际200%DPI，不是网页模拟。窗口尺寸为DIP；小屏幕上的1200×800窗口可能部分在屏幕外，原生窗口截图仍保留完整控件布局。物理输入法、其他系统DPI、电源实验没有本轮实测。系统任务栏/托盘栏位截图若工具未提供访问能力，明确保留 NOT TESTED，原生图标资源验证不能代替栏位目视验收。

使用 computer-use 完成真实输入及窗口检查；ui-ux-pro-max 的本地 WPF 查询用于自适应布局、避免阻塞UI及布局属性；yanji-domain-rules 保护原始毫秒与修订不变量；verification-before-completion 核对实际结果。没有重新安装Skill或声称运行完整设计引擎。
