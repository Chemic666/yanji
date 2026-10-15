# M3 Implementation Plan

> Agentic workers: use superpowers:executing-plans inline. One independent final review; no per-task implementer agents.

Goal：把知识节点、任务、专注、学习记录与薄弱点组成少步骤工作流。
Architecture：复用Core/Data/App；Schema4添加上下文及弱点关联，沿用原记录事务、计时器、目录及知识包。
Tech Stack：原.NET10/WPF/MVVM/SQLite，不加依赖。
Spec：[V04-M3-DESIGN.md](V04-M3-DESIGN.md)。

## 任务

- [x] 1. Core上下文/任务字段、Schema4、文件迁移与备份：先失败测试，事务贯通记录/检查点/修订；旧事实、包历史、回滚与活动旧备份恢复。
- [x] 2. 关联编辑/弱点创建事务、知识洞察与去重：实际文件SQLite；任务改绑/目录改名/归档不改旧事实；跨日、补录、软删/恢复/片段排除；未关联对账。
- [x] 3. WPF一键开始、最近学习、可跳过的结束补充、补录任务、记录/目录双向下钻、相关弱点与任务；双主题小窗口。
- [x] 4. 可控时钟完整链路测试及真实HWND采样（自由/倒计时/番茄、暂停/继续、主/迷你、树与统计负载）；有意义回归，不机械重复旧完整视觉矩阵。
- [ ] 5. 一次独立审查，重要项RED→GREEN；单一活动自包含预览目录、精简Windows ZIP/源码/审核与原始证据；包内SHA/CRC复核；保留分支/标签，停止M3。

## Review Focus

Context在保存失败/异常恢复/重复完成中丢失；记录修正从当前目录伪造旧路径；Focus旧任务与新关联冲突；任务后来跨科目变化使历史备份失效；新关联跨档案/科目；旧活动备份恢复不一致；弱点创建部分写入；包回滚丢失M3引用；父子/弱点多关联重复时长；大树查询阻断秒级刷新；快速重复点击多保存。

## 执行账本

- 只读审查：458e5d6/codex-v04-m2；M0五份hash一致，M2四包外层hash一致。没有版本混杂。用户已切High进入关键实现。
- 工程规则：显式读取yanji-domain-rules、writing-plans、executing-plans、run-tests和WPF参考。按已授权自主工作，不再询问普通设计/阶段审批。产物仅artifacts/v04-m3，禁止真实数据库。
- 实施完成：185项核心回归PASS；独立审查4项修正复验PASS；最终自包含2ebe0b9原生WPF31项PASS、10图与9样本。
- 任务5的审查、自包含程序与原始证据已完成；ZIP闭包与哈希验收以最终releases目录release-manifest.json为准，未完成前不宣称归档通过。
- M3后停止；M4真实升级、物理IME和真实电源实验保持未执行。
