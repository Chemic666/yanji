# M3 Schema4 与兼容说明

本阶段仅隔离实现，正式用户数据库升级属于 M4，未开放自动升级入口。

## 增量结构

沿用 Schema2 `chapters` 稳定身份及 Schema3 知识包结构，不重建章节体系。Schema4 仅新增：

- `session_context`：session_id 主键及外键、profile_id、可空 task_id、task_title、node_path、captured_at、capture_kind。一条记录一份学习上下文，主要知识节点仍由 `sessions.chapter` 唯一关联。
- `session_weakpoints`：session_id / weakpoint_id 复合主键与外键、title_snapshot。多薄弱点关联不参与时长 JOIN。
- 档案/任务及薄弱点查询索引；`schema_migrations` 登记版本4与规范化SQL摘要，`user_version=4`。

`LearningTask` JSON 添加可选 ChapterId、ProfileId；WeakPoint 添加 ProfileId。旧 payload 缺字段按默认档案读取，不批量重写原文。`StudyRecord.Context` 与计时检查点是关系表的投影，不建立第二套可变学习事实。

## 保存与历史

首个保存事务根据稳定ID捕获任务名称、知识路径和时间。后续检查点与实际片段、修订共同提交；TimerMachine 接收数据库确认后的上下文。仅改备注或片段保留旧快照；明确改任务/节点则重新捕获并记录修订。旧 FocusContext 的任务字段保留原始意图，M3 UI优先使用新 Context，包括显式解绑后的空值。

新关联验证档案、科目及节点；新开始拒绝归档节点或归档祖先。历史关联不依赖任务今天绑定的科目，改名、移动、归档和任务重绑均不重写旧学习事实。新增薄弱点及关联在同一事务，失败无部分写入。

## 隔离迁移与备份

`UpgradeSchema3Isolated` 仅接受 `m3-isolated-fixture.txt` 内容严格为 `YanJi M3 synthetic` 的目录。实例互斥与文件锁 → 验证Format3完整备份 → 解包到候选库 → 事务执行Schema4 → 完整性/外键/语义检查 → 原有事实、章节和包表指纹对照 → WAL检查点 → 原子替换，保留旧库回退副本。after-ddl、before-replace、after-replace 故障均有隔离测试。

旧记录迁移为 Legacy，上下文保留档案和旧任务身份，不用今天的目录冒充历史路径。旧版活动备份仅在恢复候选中补齐对应上下文，原有效片段保持不变。直接目录迁移拒绝尚有活动计时的源。

备份支持格式1～4；恢复仅在候选库增量迁移，继续保留完整性、语义检查、完整回退和活动计时保护。普通启动严格检查Schema，拒绝旧库，不尝试升级。M2程序的Schema3身份检查拒绝Schema4，旧程序不可继续写新版数据库；回退需使用保留的旧完整备份。

## 聚合口径

时长事实仅来自有效 `parts`。知识子树先形成稳定ID集合，筛选非软删记录及日期内片段，再按Part.Id去重聚合原始毫秒，最终格式化到整数秒。次数按有贡献的Session.Id去重。自身和含后代不能相加作总计；未指定节点时长单列。任务完成、已学习、自评掌握、验收通过保持独立。

真实用户迁移、断电与休眠恢复不在本阶段实际验证范围内。
