# 原生 WPF 组件落点

此项目用于验证设计原则，现阶段不向正式六页面拷贝布局。没有引入浏览器、CSS、GSAP 或新 UI/图表库。

|设计组件|实际实现|约束|
|---|---|---|
|语义颜色 / 浅深主题|`LabToolkit.cs` 的 LabTheme；`DesignResources.xaml` 动态资源|A/B 各自完整颜色族；危险动作独立语义|
|按钮 / 输入 / 选择器|`DesignResources.xaml` 的 LabButton、LabPrimary、LabPill、LabTextBox、LabCombo、LabComboItem|hover/pressed/focus/disabled；44 DIP 操作目标；主操作明确|
|导航 / 标题 / 指标|`LabWindow.cs` 的两套布局；Ui.Text / Ui.Number|数字等宽、≈ 简化值、辅助说明弱化；不堆三张等权 KPI 卡|
|StudyTimer / Reflection / SessionRow|`LabWindow.cs` 共享构造方法 + 既有 MainViewModel|计时状态决定可用操作，未知时间不确认；感想简短；记录行打开明细|
|趋势 / 堆叠 / 环形 / 热度|`LabChart.cs` DrawingContext|StudyAnalytics 毫秒事实；真实轴；命中区域映射日期/科目；鼠标与路由键盘精确提示|
|科目投入条|`LabWindow.cs` 科目列表|长度相对于本范围最大科目用于比较，旁边标实际时长；不宣称完成率|
|明细 / 补录 / 确认|`LabWindow.cs` 侧浮层|禁用底层、Tab 循环、Escape、还原焦点；删除显示整次范围，软删除可恢复|
|空态 / 核对状态|共享文本、操作入口与状态说明|无数据不虚构图形；核对入口实时刷新；浮层打开时延迟刷新防止丢失输入|
|图标 / 动画|原生 Path；120ms 轻微淡入|统一描边，无字体图标依赖；尊重系统动画开关，捕获时关闭动画|

布局使用原生 Grid、DockPanel、StackPanel 和 ScrollViewer；样式资源与模板位于 XAML，示例页面组合写在 C#。这让两套布局可以共用控件和业务；所选方向推广时再按正式 MVVM 页面边界拆分，不让样板的构造代码成为业务事实来源。

## 推广顺序（等待方向选择）

先把所选语义 token、输入/选择器、导航和明细浮层纳入正式共享资源，保持原有服务与数据库结构；然后今日、统计，再历程、计划、薄弱点、设置。每页分别完成深浅主题、900 DIP、实际窗口及数据操作回归。托盘和关闭规则继续使用正式 V0.1.1 实现。
