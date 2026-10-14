# M1 Schema2 与隔离迁移

本阶段版本0.4.0-m1.1，原始application_id保持0x594A4931，user_version明确为2。普通客户端只创建/打开2；无参数启动拒绝进入数据目录，Launch-M1.cmd在程序旁建立sandbox-data。不会在启动时升级Schema1。

## 实际结构

- `chapters`保留id/subject/name/progress/mastery/archived六列；追加profile_id（FK，SQL可空但领域非空）、parent_id（自引用FK，可空根）、node_kind（0章节/1知识点）、description、sort_order（非负）、learning_state（0未开始/1学习中/2已学习/3暂缓）、row_version（正整数）。
- `exam_profiles`：id/name/exam_key/admission_year/exam_date；`exam_subjects`以(profile_id,subject_id)唯一约束并有FK。M1仅默认档案及组合，暂不提供完整档案编辑。
- `schema_migrations`记录版本2、DDL摘要与时点。增加父节点/范围排序、session章节点、revision种类实体索引。
- 新建库与升级库重放相同Schema1+Migration2Sql，继续严格比较sqlite_master。数据语义检查还校验树环、深度、科目/档案、进度、版本与迁移摘要。
- 不建立knowledge_package_*、session_context，也不修改StudyRecord/TimerSnapshot/Task/WeakPoint字段。它们属于M2/M3后续迁移。

定义：[YanJiDatabase.Knowledge.cs](../src/YanJi.Data/YanJiDatabase.Knowledge.cs)；迁移：[YanJiDatabase.Migrations.cs](../src/YanJi.Data/YanJiDatabase.Migrations.cs)。

## 旧映射与进度

旧Chapter.Id原地保留；parent=NULL、kind=Chapter、description空、profile=legacy-default、sort按原rowid、version=1。旧百分比、掌握、归档不改，百分比0/1..99/100分别映射未开始/学习中/已学习。未来状态变更是明确操作：未开始置0，已学习置100，学习中从100恢复为99，暂缓保留百分比；不由学习时长或Task.Done驱动。

原subjects/sessions/parts/revisions/tasks/weakpoints/reflections/settings/active_timer全部旧字段按ID有序指纹对照。包含软删计时、跨日片段、Focus/任务关联、原修订JSON，不通过重写总时长“修正”迁移。旧长名称保留，新增/重命名限200字符。

考试入学年2028；原合法日期复制到档案来源快照，原exam-date设置完整保留并仍供M1设置界面使用。多档案日期编辑与单一权威迁移尚未实施，不能据此宣称完整档案系统完成。

## 实际升级路径

`UpgradeLegacyIsolated`要求目标目录存在内容精确为`YanJi M1 synthetic`的`m1-isolated-fixture.txt`。该入口仅供本阶段隔离实验，普通用户升级尚未启用。探针还要求路径位于项目artifacts。

1. 占用同数据目录单实例Mutex及迁移锁；检查严格Schema1/完整性/语义，存在活动检查点拒绝。
2. 创建并验证Format1完整备份；从WAL安全备份提取候选，不复制打开的主文件。
3. 候选在单事务执行DDL、回填、登记、版本更新。提交后校验完整性、FK、图及旧事实指纹。
4. 源库WAL检查点必须无busy/未完成页，重核源指纹；提升候选，保留独立Schema1 rollback。
5. 所有提升后异常纳入回退；成功前重新校验。操作阶段、源/候选/备份/rollback路径与指纹保存在独立operation.json，失败证据保留。

异常注入：DDL/替换前/替换后抛异常，主库保持可用Schema1；原有SQL位置插入在回退副本继续成功。真正终止隔离探针：替换前主库保持1；替换后主库可能已是完整2，保留1备份/rollback。此时保持新库并审查阶段记录，**不自动覆盖为旧库**，不把进程测试当断电证明。生产级中断恢复界面尚未开放。

## 备份与恢复

Format2保持ZIP两条目database.sqlite/manifest.json、SHA-256与旧物理记录/时长库存，增加节点/档案数量校验，包含新字段/树约束。Format1独立验证，候选升级后完整替换当前2，不合并。恢复先备份当前库、检查点、原子提升并保留rollback，覆盖后异常回退。

恢复成功递增进程内知识编辑epoch，缓存清空再加载；旧编辑即使ID/row_version碰巧相同也拒绝写回。节点行版本用于一般陈旧修改，两者职责不同。当前Schema2整体退回V0.3.1需使用升级前Schema1完整快照，不自动合并升级后的学习。

## 验证边界

真实文件、目标Microsoft.Data.Sqlite10.0.12、旧程序拒开/回退、异常及自终止进程已有隔离证据；未访问真实用户库或备份，未执行真实用户升级、电源变化、重启或断电。
