# RC-Simple 独立进度兼容性

## 存储与接口

- 维持 Schema 4，不新增表、列或迁移。`NodeProgressNote(NodeId, Content, Next, Version, UpdatedAt)` 以 JSON 字符串保存在既有 `settings` 的 `study-progress:v1:<nodeId>` 中，编码方式与阶段复盘一致。
- `ProgressNotes()` 返回所有已保存文字，包含归档节点的历史。未保存过文字的旧目录返回空列表，不从模板说明或学习记录备注推导内容。
- `SaveProgressNote(nodeId, nodeVersion, state, content, next)` 使用当前节点版本检查并递增节点版本；文字版本独立从 1 递增。节点状态、原有 Progress 映射、文字与两条审计在同一个 SQLite 事务提交。状态变为未开始/已学为 0/100，学习中最大 99，暂缓保留原进度；Mastery 不随状态变化。
- 可无计时保存；空文字允许用于仅保存状态或下一步。不创建学习记录或时长，不改模板 Description 或 `StudyRecord.Note`。Content 最多 20000 字符，Next 最多 2000 字符；允许 CR/LF/TAB，其余控制字符拒绝。
- 节点本身、任一祖先或科目已归档时拒绝写入；历史保持可读。节点改名、移动不会改变绑定所用 NodeId。过期节点版本拒绝覆盖。
- `PackageBindings(installId)` 只读返回原安装绑定，含 inactive 绑定；安装不存在抛出参数错误，不触发安装、升级或合并。

## 备份与升级边界

完整备份和复制升级沿用既有流程。新增语义验证检查 progress 前缀中的 JSON、键与 NodeId 一致性、节点存在、正版本且不超过节点版本、文本限制与保存时间；progress 审计同样检查节点和连续版本。旧格式没有此命名空间时仍按原规则验证、迁移和恢复。此兼容性不意味着旧程序理解进度文字；旧程序重新打包前应使用本候选程序执行完整语义校验。

实际统一验收使用 `YANJI_M1_TEST_ROOT=artifacts/v04-rc-simple/core-data` 下的真实文件SQLite，不读取真实用户数据库。13个新进度案例已纳入一次216项核心回归，原始结果为 `core/core-simple.trx`、`core-simple.log`。先前 `progress-red.log` 与编译受并行未完成文件阻塞的 `progress-green.log` 保留，后者不作为测试通过证据。原生WPF结果另见本轮验证报告；真实用户升级没有执行。
