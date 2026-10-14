# V0.3 Skill 能力与真实使用

## 来源核查（2026-10-14）

| 官方仓库 | 固定提交 | 许可 / 维护快照 | 选择 |
|---|---|---|---|
| [dotnet/skills](https://github.com/dotnet/skills) | 9de888b8ce55da34e76a113bd7b2d463d6b3e1e6 | MIT，非归档，提交 2026-10-13 UTC | run-tests、coverage-analysis |
| [microsoft/winappCli](https://github.com/microsoft/winappCli) | d399e3151db3a6e549ca4ac9d9cbeefa21260ed8 | MIT，非归档，提交 2026-10-14 UTC | winui-ui-testing 指导；未安装 CLI |
| [github/awesome-copilot](https://github.com/github/awesome-copilot) | f25421d3f1b8b3bb217f78a7c0567cf7ec117cd8 | MIT，非归档，提交 2026-10-14 UTC | breakdown-feature-prd；WPF 指令仅参考 |
| [openai/skills](https://github.com/openai/skills) | 49f948faa9258a0c61caceaf225e179651397431 | 非归档；仓库 API 未给统一许可证。skill-creator 单项 Apache-2.0 | 已随 Codex 提供，不重复安装 |

固定提交代替不存在的统一 Skill 发布版本，不能把仓库推送日期作为每项 Skill 的更新日期。原始 metadata 和文件哈希见 docs/skills/v03-installed.json。dotnet 的 agents/*.agent.md 是 Copilot custom agents，并非 Codex 原生 agent；标准 SKILL.md 是可移植指导。csharp-expert 是路由入口，本次没有安装整个 .NET 插件。诊断/性能指南已评估；无本次性能故障证据，因此没有安装 profiler 或进行全面性能扫描。

## 安装与执行边界

安装在 `D:\Projects\yanji\.agents\skills\<name>`。先审查 SKILL、全部附带文件和许可证。官方安装器先遇到沙箱临时目录限制，调整项目内临时目录后仍遇到 Windows 长路径；没有修改系统长路径策略。改为将预先读取的精确文件复制到项目，逐文件按 Git blob SHA-1 核对固定提交，并记录 SHA-256。没有运行远程一键脚本或未知二进制。

coverage-analysis 的三个 PS1 是本地 XML 解析/计算工具，审查后只运行 Extract-MethodCoverage 对合成 Cobertura 样本的解析。它正确得到样本 1/2 行命中；这不是研迹代码覆盖率。未安装 collector / ReportGenerator，未声称应用覆盖率已测量。

winui-ui-testing 的 winapp ui 工具当前未发现；Windows Sandbox 未启用或启动验证。本轮采用原有 owned HWND 截图和 WPF UIA provider Invoke 方法，不能写成 winapp 或物理键鼠测试通过。无 WinUI 框架迁移、SDK 系统安装、MSIX 身份/证书安装。

## 五种状态与实际应用

本桌面协调会话的真实 cwd 仍是旧的 C 盘 Codex 材料目录。D 盘项目技能不在该桌面会话初始技能目录清单中，自动加载标为 **未证明**；明确路径读取不是自动发现。

为验证支持的 `$skill-name` 调用，在正确 D 盘工作区运行一次 Codex CLI 0.160.0 的 ephemeral read-only 项目会话。它在查找文件前明确报告运行时目录已包含 `$yanji-domain-rules` 和 `$run-tests`，随后读取两个完整指导并完成实际 FocusTimerTests / TimerMachine 审查及可运行的精准测试命令建议。它没有执行额外测试、修改文件或读用户库。完整 JSONL 和回答随证据包提供；这不是 app-server 的纯发现探针，也不冒充原桌面会话已重绑定。

| 能力 | 已安装 | 当前原桌面自动加载 | 已读取 | 实际工具/任务 | 落实 |
|---|---|---|---|---|---|
| run-tests | 是，固定提交 | 未证明；D 项目 CLI 原生发现和显式调用已验证 | 是 | 本轮 xUnit v2/VSTest 定向红绿测试与一次核心回归；CLI 只读命令审查 | 最小过滤、真实 TRX、没有测试平台迁移 |
| coverage-analysis | 是，固定提交 | 未证明 | 是 | 合成 XML 解析脚本实际成功；没有应用覆盖采集 | 缺失证据保持未知，不用百分比伪装测试充分性 |
| breakdown-feature-prd | 是，固定提交 | 未证明 | 是 | 指导型 Skill，没有独立引擎；产出 V03-PLAN.md | 问题/范围/默认/验收与技术方案；拒绝 SaaS 和绝对输出路径 |
| winui-ui-testing | 是，固定提交 | 未证明 | 是 | 完整 winapp 工作流未执行，工具缺失 | owned PID、真实窗口、批次证据和输入/截图区别 |
| dotnet-wpf | 没有当作 Skill 安装 | 不适用 | 是，参考指令 | Copilot applyTo 不在 Codex 自动触发 | MainViewModel.Focus、共享 MiniTimerWindow、异步保存及样式资源；不重建框架 |
| skill-creator | Codex 系统自带 | 可用技能目录中存在，本轮读取 | 是 | quick_validate.py 验证领域 Skill 成功 | 简短入口、真实代码锚点、无泛化框架 |
| yanji-domain-rules | 是，本地 1.0，版本随源码提交 | 未证明；D 项目 CLI 原生发现和显式调用已验证 | 是 | 领域不变量设计、隔离测试、独立实际审查 | 目标上限、任务关联事务、修订与备份一致性；不把计时结束当作完成任务 |

没有安装全部仓库。没有将 Copilot 专用指令、可移植 Skill 和额外执行工具混为一类。已有设计 Skill 与新能力职责分离；用户低打扰授权优先于要求逐阶段审批的通用流程指导，测试范围以真实风险为准。

## 后续建议（不是实施授权）

- V0.3：领域规则、WPF/MVVM 参考、run-tests；winapp 待有明确工具与桌面测试条件时再验证。
- V0.4：先更新领域规则和版本化模板/迁移验收，再使用 PRD 拆解及必要覆盖分析，不提前实现。
- V0.5：数据语义与可解释分析优先，覆盖率只能提供缺口线索，不能代替事实正确性。
- V1.0：发行、安全 UI 自动化、迁移矩阵、真实故障验证与许可证审查；诊断工具按实际故障选择，不全部常驻安装。
