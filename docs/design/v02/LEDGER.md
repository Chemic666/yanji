# 进度 — docs/design/v02/PLAN.md

- Task 1 complete：Taste 内容人工读取/来源固定；4张真实 HWND 基线图已目视检查；59项基线测试 PASS，artifacts/v02-tests-baseline/v02-baseline.trx。
- Task 2 complete：双方案规范与实施计划已制定；feature分支codex/v0.2-visual-exploration，v0.1.1保留。
- Task 3 complete：独立 `design/YanJi.DesignLab` 原生 WPF 样板，两种导航/布局/图表结构，各含今日与统计的深浅主题。真实计时、补录、感想、原始记录、备注修订、软删除/恢复复用已有服务。
- Task 4 complete：原业务最终回归 59 PASS；发布样板运行断言 28 PASS；21 个常规/边界窗口及 8 个空态窗口已实际截取，每个同时保存 Native PrintWindow 与 WPF visual-tree 证据。
- Task 5 complete：独立代码审查的 2 个 Important、3 个 Minor 全部修正并复查；自包含 win-x64 程序实际执行 exit 0。交付比较说明、规范、运行指南及报告。
- 已有正式 App/Core/Data 源码保持基线；没有数据库迁移。v0.1.1 原标签及发布保留；实验夹具与最终用户数据库隔离。所有演示操作仅写入标记为 VISUAL-LAB-ONLY 的独立目录。
- 本轮完成视觉探索；方向未获用户选择，不推广六页，不合并正式应用。

## 最近可靠证据

- 常规最终发布：`artifacts/v02-final-release-acb2cd496ceb4b88813017fa7d9e2010`，`execution.json` exit 0；`screenshots/interaction-results.json` 28 PASS。
- 空态最终发布：`artifacts/v02-final-empty-606e5f33ffd944bdb1be88aae8a2202e`，exit 0；8 个空态原生截图。
- 正式业务回归：`artifacts/v02-tests-final/v02-final.trx`，59 PASS、0 FAIL、0 SKIP。
- 原始基线：`artifacts/v02-baseline-5b58b7dbe96b4881a28d3caad345b014`，4 个 V0.1.1 原生窗口。
- 检查结果来自文件和进程退出码，不从计划推定 PASS。捕获 JSON 的 CAPTURED 只是文件生成；人工视觉检查结论单独写入 VALIDATION.md。

## 已发现和修正

1. 下拉框显示对象名、行内容居中、深色主按钮文字颜色错误：统一模板与显式文字资源后复拍。
2. Closing 重入异常：改为 Dispatcher 排队关闭；最终程序正常退出。
3. PowerShell 参数拼接引入路径内空格：修正括号；仅正常关闭确定属于样板的错误窗口，没有强制终止其他程序。
4. 后续 Native PrintWindow 在硬件渲染下出现白图：样板进程使用 SoftwareOnly；最终原生截图和实际 visual-tree 一致。正式应用没有切换渲染策略。
5. 小窗选择器箭头布局及开始按钮拥挤：模板 Stretch、间距和时钟字号按窗口调整。
6. 备注保存从后台读取 UI 属性：先在 UI 线程捕获字符串，再执行事务；先前 note-revision FAIL 不算通过，最终重跑 PASS。
7. 独立审查：异常核对入口未立即出现、浮层键盘穿透、删除过滤影响预览、深色日期输入、键盘精确提示。全部修正，新增回归断言。`artifacts/v02-review-red-1956f23e065f4f71a3afc9ad1a171a7b` 保留修正前 review-entry-live FAIL。

## 验证边界

实际本机 200% DPI；900×600、1200×800 和本机可用大窗口。100/125/150%、跨显示器、物理 IME、物理键鼠、屏幕阅读器、真实休眠/电源恢复及洁净离线设备 NOT TESTED。路由事件、模拟异常、SQLite 数据断言和截图不能替代这些实验。
