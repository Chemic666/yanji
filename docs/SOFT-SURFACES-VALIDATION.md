# 自然分层 · 定向验收

## 结果与来源

已完成当前授权范围，等待人工视觉验收。基线HEAD fcfda5f7307b37adc3ac675cfa190aa384a78dba，分支codex/v04-rc-polish2；原v0.4.0-rc.calendar.1恢复标签未改变。没有新提交或推送。

程序沿用0.4.0-rc.calendar.1程序集版本。**本轮包含未提交工作区改动，HEAD或版本号不能单独代表候选程序。**

- 编译输入指纹：6144ECAE6C62003BBB6FB56AFD1ED220893CE4C5AE9584F7BD46F07724AF8E72。
- 最终YanJi.dll SHA256：FCE6FC7FECB0D3081A5ED89CE3FDBB62E4AC7EA3B88F420163CC91359A42074D。
- 原始最终窗口证据：artifacts/surface-refinement/sealed；before为基线界面；after为本轮定向计时检查。
- 比较页及审核ZIP、Windows候选、工作区源码快照由tools/Package-SurfaceReview.py生成；包内清单在ZIP关闭后逐文件重新校验。

## 已执行

| 检查 | 实际结果与证据 |
|---|---|
| 最终Windows x64自包含发布 | PASS，publish-menu-final.log；无编译错误 |
| 今日、进度、数据 | PASS，3页×浅深×1200/900，12张真实HWND截图；192DPI，即本机200% |
| 输入与选择器 | PASS，真实KeyboardFocusWithin；默认/焦点图，原生ComboBox弹出层，未替换文本输入部件 |
| 按钮状态 | PASS，浅深各Normal/Hover/Pressed共6张；actual-button-states.json记录真实IsMouseOver/IsPressed；按下位移1.5DIP |
| 菜单 | PASS，最终共享模板去掉默认灰色图标栏；外部Windows键盘Down+Enter打开已有编辑对话框，随后取消，没有保存修改；external/menu-keyboard.json/jpg |
| 日历与数据入口 | PASS，LearningCalendar、ChapterAnalysis、StudyReviewSection保持直接可见；月历及原始截图裁切近景 |
| 事实与SQLite | PASS，最终4项检查，合成原始学习时长41,761,201ms未变，integrity_check=ok |
| 定向计时回归 | PASS，after/checks.json的8项检查；33,479ms、32次连续真实采样，期间切换月份；两个周期保存没有Refresh.UI，暂停不增加时长，保存毫秒值与计时引擎一致 |
| 发布包与源码 | 包内BUILD-INFO和源码指纹区分HEAD与未提交输入，打包前再次逐文件验证编译输入；不把版本号相同误写为代码相同 |

计时采样来自最终菜单模板之前的同轮样式版本。此后仅细化焦点色、菜单模板、增加仅截图的隔离验收开关；计时引擎、持久化与计时显示路径未变，因此复用该受影响回归，不重复计时测试。Tick.Async最大跨度39.5378ms，其中包括异步等待，并非UI线程阻塞时长测量；LoadDay.UI最大7.0122ms。只能据此说明此场景未发生整页重建，不能保证所有设备均无帧延迟。

基线另有126秒、10次检查点、主/迷你同步的计时证据，来源artifacts/calendar-restore/final，原样保留。本轮复用条件是计时、数据及迷你窗实现未改；不声称在最终样式上重新完成了126秒。该历史运行记录MaxGap212.3229ms，P95 41.4349ms，不能宣称全程低于50ms。

按钮状态使用真实鼠标操作。为捕捉合成器已呈现的Pressed帧，仅惰性按钮验收样板有140ms松开延迟，生产按钮没有该延迟。自动12场景为原生窗口捕获，不是HTML渲染。日历近景仅像素裁切，crop.json保留坐标与源图。

## 改动边界

- Styles.xaml：统一表面、输入/选择器、按钮、导航、记录列表、菜单与提示模板。
- ThemeService.cs：浅深主题的内嵌表面、细分隔和焦点色；系统高对比颜色回退。
- KnowledgeTreeStyles.xaml：选择背景与键盘细线。
- MainWindow.xaml：轻分隔、日历背景区分；没有改变页面结构及显示区域。
- MainViewModel.cs：只增加CalendarDay.HasStudy只读展示属性，不改变时长与日期聚合。
- App.xaml.cs、MainWindow.xaml.cs、Polish2Validation.cs、新增SurfaceValidation.cs：显式隔离验证开关与复用样板入口；不改变正常计时行为。
- 新增设计说明、此报告及本轮打包脚本。没有修改Core、Data、Schema、迁移、知识包、图标或许可。

## 证据保留

before、after、final、approved、sealed均保留；中间截图不覆盖。approved/light-normal曾受鼠标悬停污染，未作为最终Normal证据；sealed六个状态全部重新观察并记录真实标志。审核包只选before、sealed与必要after计时原始JSON，不重复打包全部中间图或数据库。

打包过程中曾错误地仅在源码ZIP内将5个第三方许可文件转为LF，许可哈希检查正确阻止交付。已改为原字节保留并保留失败包和原因于artifacts/surface-refinement/packaging-failure；未改许可原文件或程序。修正后重新执行源码许可与封闭ZIP检查。

## 未验证与限制

- NOT TESTED：物理中文IME候选操作、其他系统DPI、真实睡眠/休眠、系统高对比开关。未干扰系统设置。
- 没有重跑216项核心测试或数据库迁移，因为相关实现未改；不把历史通过改写成本轮执行。
- 不是全面无障碍认证，没有宣称所有对比度/读屏组合都已实测。
- 最终候选不覆盖原RC版本。解压新目录运行YanJi.exe，默认使用旁边data-rc-calendar；不得放进已有学习数据目录或指定真实数据库。
- 全部验收只使用artifacts/surface-refinement中的合成数据，未访问用户真实学习库。最终源码快照含未提交改动，待后续明确提交授权。

本轮完成后停止，等待人工验收。
