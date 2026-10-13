# 静桌 / Quiet Focus

固定暖纸画布、纸白内容、柔绿操作与墨色文字，深色成对设计。权威语义颜色位于 src/YanJi.App/Styles.xaml 与 ThemeService.cs。

## Typography

已安装 Segoe UI Variable / Segoe UI / Microsoft YaHei UI。页标题24/32、区域16/24、正文14/22、元数据12/18、主数字28、次数字20、计时44 DIP。自然时长数字与单位分层，原毫秒不回算；动态数字 Tabular。

## Spacing and surfaces

4/8/12/16/24/32 DIP；内容 gutter20–24；表面16、密集12；表面圆角12、输入按钮8、状态4。结构线与焦点环保留，不加双层壳、玻璃、纸纹或通用阴影。

## Components and behavior

SectionSurface / DenseSurface / SectionTitle / MetaText / Toolbar / InlineStatus 共用既有 XAML 资源。40 DIP 桌面目标。显式查看、编辑、删除；历史在详情渐进展示。今日摘要有界，七日节奏独立。统计每日实际时长为主、科目横条为辅，范围改变即时重算。详情禁用背景、焦点循环、关闭返回操作位置。

## Motion

高频交互使用静态状态反馈；无页面入场或数字 tween，不新增库。没有动画时不虚构10%回放验收。

## Adaptation

WPF Layout_Changed 修改命名 Grid；侧栏与内容独立断点。今日内容宽不足700才堆叠；统计小窗主次图堆叠。原生900×600与1200×800、浅深、空态与长文本验收。
