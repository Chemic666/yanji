# M2 Schema3、目录更新与安全回滚

原 chapters、sessions、parts、tasks、weakpoints 和所有旧ID不重建。Schema3仅添加4表及操作查找索引，DDL见 `src/YanJi.Data/YanJiDatabase.PackageSchema.cs`：

| 表 | 作用 |
|---|---|
| knowledge_package_versions | (package_id,version) PK，内容哈希、来源身份、验证过的完整JSON基线 |
| knowledge_package_installs | 安装ID、档案FK、包/当前版本复合FK、显式科目绑定JSON、行版本；档案/包唯一 |
| knowledge_package_nodes | 安装/外部节点复合PK、本地chapters FK且唯一、active；inactive为解绑/移除墓碑 |
| knowledge_package_operations | 安装FK、时间、回滚标记、前后模板/映射/节点差异JSON；不储存第二份学习总时长 |

新库重放Schema1→2→3。旧程序严格拒绝Schema3；新版不偷偷升级Schema1/2。`UpgradeSchema2Isolated`只接受明确合成标记、数据目录单实例与独占迁移锁；先验证Format2完整备份，再在候选快照中事务迁移，比较全部旧学习事实、完整chapters/Profile/科目组合，最后WAL检查点与原子替换；失败保留旧文件和证据。Schema1实验接口同样提升至最新Schema3。

M1迁移摘要原先取源码换行，导致LF重新编译版本与CRLF旧登记不同。本阶段只兼容受控Migration2/3脚本的精确LF/CRLF摘要；新Schema3写LF摘要，表/索引/约束仍严格重放比较。没有正式用户升级入口。

Format1/2/3备份均验证身份、SHA256、SQLite integrity/FK、实际Schema、学习片段、目录、包基线、映射、版本、操作历史。恢复旧备份在隔离候选中提升至3，然后保留当前完整备份与回退副本再替换；恢复成功使旧编辑/预览epoch失效。恢复是整库操作，知识包回滚从不调用恢复。

## 三方更新

B=当前安装模板，L=本地KnowledgeNode，U=新包。ParentId先映射本地稳定ID。Name/Description/Parent/Kind/Order分别比较：L=B采用U；U=B保留L；L=U无需改；其余冲突默认保留L，用户可明确选择模板。用户归档或解绑另有显式选择，不自动复活。进度、状态、掌握、学习事实不进入模板合并。

上游移除且有历史记录（含软删）、薄弱点、用户修改/状态或本地子节点的目录保留解绑，其必要祖先也保留。其他仅归档，永不物理删除。预览列出目录差异、直接历史关联数量、冲突及目标。最终全图必须有效，不是只验证输入的U。

预览令牌覆盖完整目录、科目/档案、安装/映射/历史、sessions/parts/tasks/weakpoints及恢复epoch。提交重新生成计划；不信任客户端传入Changes。数据变动要求重新预览；有效应用前验证完整备份，活动计时禁止应用。节点、修订、版本、映射、操作在同一SQLite事务写入。相同安装内容零写入、零新增修订。

## 安全回滚

逆向比较操作After / 当前L / 操作Before。后续进度、掌握、状态、学习记录全部保持；后续备注永远不由目录回滚覆盖。后来改名/移动的冲突默认保留；有后来学习/修改/孩子的新增节点保留解绑，其祖先保留。未用且未改的新增节点归档，Id及映射永久保留。

仅撤销最近未撤销的目录操作，可依次回到更早版本。首次安装可撤销目录新增，但版本、来源和安装审计留存；重新导入相同包仍是幂等操作，不自动启用墓碑，可在知识目录明确重新启用或在后续版本预览中明确重新绑定。此功能不是物理卸载知识包。

本阶段没有session_context、任务节点ID或知识包在线更新，M3必须另行授权。
