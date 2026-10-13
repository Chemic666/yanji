# 研迹

<!-- impeccable:product-schema 1 -->

## Platform

Windows desktop / WPF. Unsupported by the engine's web/ios/android/adaptive enum; do not substitute another platform.

## Users

备考者在 Windows 桌面记录学习、安排章节与任务、回顾每日投入和薄弱点。

## Product Purpose

本地离线保存真实学习证据，帮助用户继续下一段学习。没有账号、排名或虚构效率评分。

## Stack

.NET 10 WPF、SQLite；现有 XAML ResourceDictionary 与命名 Grid。

## Capabilities and Constraints

计时、纯时长与区间补录、跨日贡献、整次修订与软删除、片段排除恢复、备份恢复。毫秒是权威数据；未来范围的自然日均只使用截至今日的贡献。任务完成不生成学习记录。详情保护焦点，危险操作默认取消。

## Brand Commitments

用户固定 A「静桌」暖纸柔绿 Quiet Focus；遵循 docs/V021-PLAN.md，不重新选择风格。

## Evidence on Hand

src/YanJi.App 是正式应用；artifacts 中只使用隔离合成 fixture。V0.2.0 恢复标签与发布包保留。测试与原生窗口截图分别验收。

## Product Principles

保留真实精度与身份；动作可发现且可恢复；范围口径可见；原生操作优先于装饰。
