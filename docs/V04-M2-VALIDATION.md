# M2 验证报告 · 2026-10-15

最终产品代码 **430a67bb0fe186572a230d3dcef2461d8ae272cd**，版本0.4.0-m2.0。以下来自本机实际执行证据，未访问真实学习数据。报告/归档后续提交可以不同，发布清单分别记录代码和文档HEAD。

| 验证 | 结果 | 原始证据 |
|---|---|---|
| .NET xUnit/VSTest全量 | **168 PASS / 0 FAIL**；M1基线134（当前Schema断言更新至3）+ M2新增34 | artifacts/v04-m2/final-core/final-core.trx |
| 独立审查修复复现 | 6项，修前5 FAIL/1已有保护，修后6 PASS | review-red / review-green TRX与对应SQLite文件 |
| Schema2→3 | 文件SQLite，旧全部学习事实/完整知识节点/档案/科目组合一致；升级前Format2完整备份验证通过 | final-core-data/**/schema2-upgrade.json、migration-evidence/operation.json |
| 迁移失败 | after-ddl/before-replace/after-replace，源Schema2事实指纹与integrity不变 | final-core-data/**/schema2-fault.json |
| 知识包事务故障 | after-nodes/before-commit全部回滚，原事实/目录/安装不变，重试一次成功 | final-core-data/**/atomic-retry.json |
| 生命周期 | V1→本地改名/备注/子节点→V2→冲突→后来学习→目录回滚；稳定Id、后来记录/进度保留 | final-core-data/**/lifecycle.json |
| 格式/安全 | 重复键/节点、缺父、环、深度、字段类型、版本、源身份、哈希、控制字符、大小；零部分写入 | KnowledgePackageFormat/Safety/ReviewTests 与TRX |
| 幂等/陈旧计划 | 同版本同摘要零写；同版本异内容拒绝；目录编辑/恢复使计划失效；伪造Changes/Payload不作为提交事实 | 同上 |
| 完整备份恢复 | Format1/2/3、目录/包历史/绑定语义检查；归档科目可备份；篡改历史集合/映射拒绝恢复 | 现有备份测试 + M2 Migration/ReviewTests |
| 旧程序版本拒绝 | 实际M1自包含程序拒绝Schema3（exit1）；源文件SHA256不变；当前程序startup exit0 | old-version-guard.json、m1-rejects-schema3/ |
| 原生WPF最终发布程序 | **27 PASS / 0 FAIL**，10张HWND截图、9个真实计时采样 | window-accepted/checks.json、result.json、samples/provenance/screenshots |
| WPF功能 | 模板筛选/树、确认安装、同包提示、文件更新、实际ComboBox冲突选择、目录回滚、错误提示、脚本焦点/Tab | window-accepted/ |
| 三种计时模式 | 原生主/迷你每种采样1/2/3秒、暂停实际等待、继续/结束原始毫秒一致；每次并行验证10k知识包预览 | window-accepted/timer-samples.json |

## 实际性能与环境

Windows build **10.0.26200**（Windows11），x64，自包含.NET **10.0.12**，SDK **10.0.401**；既有依赖保持锁定。真实WPF DPI192，软件渲染用于确定性HWND捕获；不是全设备GPU性能验收。

最终全量套件中的一万节点文件SQLite：预览 **1827ms**，应用 **7043ms**，备份 **2306ms**。同轮有其他测试/磁盘负载，不能用更早464/1609/760ms的较好结果替代。验证阈值分别5/15/10秒。最终原生WPF懒树：100根、展开99子，生成/布局 **49.14ms**；不是整个程序冷启动耗时。写应用期间禁止活动计时，读取预览在后台执行。

## 保留的失败证据

- format-red/lifecycle-red：新增接口尚未实现时的TDD失败，不是既有产品故障。
- first-regression：旧测试期待Schema2，按正式Schema3需求更新断言。
- migration-safety：冻结旧DDL的LF摘要被M1原先CRLF敏感校验拒绝；migration-green确认修复。
- window-run1：无效包异常未被捕获；修正后window-run2通过26项。
- window-final：固定等待脚本在可信713ms时错误要求显示非零秒，主/迷你/VM一致；改为达到可信采样时长再读取。原FAIL保留，window-accepted最终27项通过；没有修改计时事实计算。
- review-red：独立审查的4类缺口；已真实复现并完成一次修复，未以第二轮审查替代回归。

## NOT TESTED / 限制

真实用户Schema升级与数据迁移、物理中文IME/真实键盘、其他实际DPI、真实睡眠/休眠/断电/重启、跨进程恶意写入及大规模长期版本历史性能未测试。没有系统时钟/电源/权限配置修改。65秒以上原生计时的M1证据仍属其原代码；本轮不重复该完整矩阵，也不把它改记为M2新实测。

程序适合**独立数据目录的M2功能验收**；不开放真实用户升级，不是V0.4正式生产迁移发布。具备进入M3隔离开发的基础，但须用户另行授权。
