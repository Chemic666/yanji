# GitHub 驱动的协作与交付约定

更新：2026-10-19。对应 [Issue #2](https://github.com/Chemic666/yanji/issues/2) 与 [PR #3](https://github.com/Chemic666/yanji/pull/3)。本地工具链、隔离测试和看板访问已补验；尚未配置周期唤醒、GitHub 事件触发或 CI。

## 事实来源与核查顺序

1. 每次开始先读当前 GitHub Issues（含讨论与依赖）、开放 PR、最新 main/相关分支与版本说明；能访问 Projects #2 时，再读取其中的草稿任务和字段。项目看板不能读取时，明确标记 **BLOCKED / NOT VERIFIED**，不能把 Issues 当作看板全部内容。
2. 本地工作树可用时，先运行只读 `git status --short --branch`、`git remote -v`、`git log -1 --format=%H`，比较远程 main；检查未提交改动并保护用户文件。不能访问本机时，不得声称已验证本地环境、编译或测试。
3. 以最新源码、可重现证据与当前产品需求为事实，版本阶段历史文档只是历史记录。已完成的功能不重复实现；需求不明确不擅自编造。
4. **单一任务入口**：一项明确可执行的改动至少关联一个 Issue；先去重，再写清验收标准、依赖、影响范围、风险和回退。Projects 状态为看板视图，不替代 Issue 的验收条件。

## 任务排序与执行

- 首先处理已确认的 P0 数据完整性/安全回归；其次处理阻塞现有任务的 P1，再处理可独立验证的 P2/维护事项。未验证的兼容性缺口单列“待验收”，不能谎称已出现故障。
- 对每个候选任务标记 `blocked by #N`、文件/模块依赖、测试前提；按拓扑顺序和最小影响范围执行。若无开放 Issue，先复核 Projects 和实际用户反馈；禁止凭旧版路线图自行扩展功能。
- 在授权范围内可自主分析、实现、修复常规问题；遇到不可逆的数据迁移、真实个人数据访问、许可证或安全设计冲突、破坏性 Git 操作、付费/按量计费或真正阻塞时暂停相关操作并请求决定。
- 禁止重新打包、覆盖或修改既有 `v0.4.0-beta.1` 标签/发行附件；新发行必须独立确定版本、完整门禁和授权。冻结的 Quiet Focus 视觉和数据语义保持不变，除非有明确改动需求。

## 分支、验证与进度回写

1. 优先接续同一任务已有分支和 PR；新分支默认使用 `codex/` 前缀。从核实后的远程基线开始，禁止在未检查本地差异时远程强推、覆盖用户工作区或自动合并复杂变更。若本地与远程历史无共同祖先，先核查历史清理记录，保留旧分支，禁止直接合并旧历史。
2. 先读受影响模块的代码和隔离测试规范，再实现最小范围更改。使用测试专用目录，不访问默认 `%LOCALAPPDATA%\YanJi` 的真实数据库和备份；不可用的真实环境记 `NOT TESTED`。
3. 阶段结束提交有意义的 commit；PR 正文链接 Issue，附测试命令、输出摘要、构建环境、回归风险、未测边界和必要回退方式。只对实际执行的测试写 PASS/FAIL，不把旧证据当新验证。
4. 同步 Issue 进展与验收项；若有 Projects 写入途径，则同步看板字段，否则留待后续手动验证。仅在验收条件实际满足时关闭 Issue；合并前核对 CI、PR diff、最新 main 与当前发行状态。
5. 阶段收尾重新读取 Issues、PR、需求更新及 Projects（若可访问），对新依赖重新排序。当前对话运行期间，这些步骤及必要的开发、测试、修复、阶段提交/推送和 Issue 回写可自主执行；PR 合并、Release、标签和高风险操作仍按单独授权办理。**后续自动执行需要真实触发器、在线执行器及适当权限**，本次聊天的权限不能构成持久在线保障。

## 当前连接和测试矩阵（2026-10-19）

| 能力 | 本次状态 | 说明 |
|---|---|---|
| GitHub 仓库、Issues、PR、分支 | 已验证读取与远程 fetch | 连接账户具备仓库写权限；阶段提交仅推送对应任务分支 |
| GitHub Projects #2 条目与状态字段 | 已通过登录浏览器读取、写回并刷新验证 | 无筛选的当前看板：Todo 0、In Progress 为 #2、Done 为 #1；无其他当前任务。已有测试 Issue #1 的 Status 写回 Done，刷新后保留；没有更改 Issue 开关状态 |
| Projects 自动加入/状态联动 | 配置与已有运行事件已核对 | 自动加入启用，目标 yanji，过滤 `is:issue is:open`；#1 时间线实际记录自动加入、设为 Todo、关闭后转 Done。本轮未新建重复测试 Issue，未新测所有七项规则 |
| 远程 main/Release | 已读取 | 当前 main `9f1089f4eecbface7084a7387b992bdf029cd621`；已有 Beta 预发行 |
| Windows 本地工作树 | 已核对并接续 PR #3 分支 | 原本地 `codex/v04-rc-polish2` 保留；未跟踪文件逐项哈希保存，本轮不提交这些资料；产品源码与远程 main 一致 |
| .NET 10/WPF 构建及本地隔离测试 | PASS，限下述范围 | 已安装 SDK 10.0.401 与缓存可用；默认系统 dotnet 仅有 6.0 运行时，不能直接使用 |
| GitHub Actions CI | 未配置 | 当前仓库没有 workflow 文件，本机通过不等于 CI 通过 |
| 自动周期唤醒/GitHub 事件触发开发 | 未配置，后续需触发 | 当前没有为此项目设置计划或事件调度；新的对话指令可启动下一轮，用户无需转发任务报告 |

## 本次环境基线与任务顺序

1. 已读取当前 Issues、PR #3、远程 main 和看板，确认现有 Beta/安装器已交付；不按过期本地发布报告重做功能。当前唯一开放任务为 #2，本次继续其剩余验收项。
2. 已拉取远程引用。旧本地历史与远程无共同祖先；结合 `docs/HISTORY-SANITIZATION.md` 核对后，直接接续已有 `chore/github-collaboration-baseline-20261019`，不合并或推送旧历史。
3. 已复用安装的 .NET 10 SDK 和包缓存，测试/构建使用 `--no-restore`。测试写入隔离目录，不启动正式模式，不读取真实学习库。
4. 已完成看板访问、自动规则和既有自动加入事件核查、测试事项状态写回验证，再补齐 Issue/PR 验收记录。原始本机日志和截图留在忽略的 `artifacts/github-collaboration-20261019/`，不上传原始证据。
5. 阶段交付后重新抓取待办。若没有其他开放任务或看板草稿，则停止当前轮次，不从历史计划编造新功能。PR 的审查/合并与新的运行触发分别报告。

## 本轮真实验证

验证源码基线为 PR #3 的 `888ac06`，其产品源码与 main `9f1089f` 一致。后续仅修改本协作文档，不重跑不受影响的业务检查。

- SDK：10.0.401；测试宿主运行时为现有 10.0.12，WPF 应用的自包含运行时配置仍为 10.0.13。本次未发布或重新制作安装器。
- xUnit/VSTest 定向测试：`dotnet test tests/YanJi.Tests/YanJi.Tests.csproj -c Release --no-restore --filter "FullyQualifiedName~RuntimeLocationTests|FullyQualifiedName~TimerMachineTests|FullyQualifiedName~TimePartsTests|FullyQualifiedName~DatabaseTests" --logger "trx;LogFileName=baseline.trx" --results-directory artifacts/github-collaboration-20261019/test-results --nologo`。**Passed: 44, Failed: 0, Skipped: 0**。
- WPF 构建：`dotnet build src/YanJi.App/YanJi.App.csproj -c Release --no-restore --nologo`。退出码 0，**0 warnings / 0 errors**。
- 上述 `dotnet` 指本轮确认的现有 SDK 可执行文件；包缓存沿用既有位置，CLI 状态和测试数据重定向到本轮 artifacts。没有新增安装或下载。其他环境先核对 SDK/缓存，不照搬本机路径。
- NOT TESTED：本轮没有新做 WPF 交互验收、真实电源/IME/DPI/干净机测试、全量回归或 CI。构建成功不能代替这些验收。

## 免费与安全约束

使用 GitHub 现有账户功能、本地已许可的工具与允许的免费资源；不主动启用有额外费用的功能，不依赖按量计费 API。GitHub Actions 如未来采用，必须先核实当时账户免费额度与执行环境，不假设无限免费。任何本地安装、网络下载、发布、权限变更都要受当前授权与项目风险门槛约束。
