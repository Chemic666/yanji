# V0.4 RC-Final 验证报告

状态：**READY FOR FINAL APPROVAL**。本报告以2026-10-16本地收尾时点的文件证据为依据；实机采样原始时间保留UTC，不改写历史。最终封包结果以发行目录的 `release-manifest.json`、`SHA256SUMS.txt` 和各ZIP内部清单为准。

## 版本与边界

- 分支 `codex/v04-rc-final`；程序源码提交 `31a5cedca63fe0f4b18983b07cad85f1bd15bb3c`。
- 程序集 `0.4.0-rc.final.1+31a5cedca63fe0f4b18983b07cad85f1bd15bb3c`，新本地标签 `v0.4.0-rc.final.1` 指向该程序提交；源码包使用随后文档提交，准确完整HEAD写入外部清单。
- `YanJi.dll` SHA-256：`84308ACBB48DBD220EC65793D8190445F497217DE2919233CE643578B8207C78`。
- 后续源码差异只有发行脚本、报告与5份上游许可原始换行修复；打包门禁证明编译源码、版本配置、知识包、许可清单与程序提交一致。5份许可新字节必须等于已封闭Windows ZIP对应原文，CRLF归一为LF后必须等于程序提交旧Git blob。
- `src/YanJi.Core`、`src/YanJi.Data`、测试业务代码、依赖版本与Schema未变。不访问真实学习数据库/备份，不执行真实数据升级、公开发布、电源或系统设置操作。

## 本轮实际执行

证据根目录：`artifacts/v04-rc-final/`；证据ZIP内加 `evidence/` 前缀。

| 项目 | 结果与证据 | 范围/限制 |
|---|---|---|
| 自包含构建 | `publish.log` 退出0；Windows x64，一次自包含输出 | SDK10.0.401，运行时10.0.12；不重复构建 |
| 许可门禁负例/正例 | `license-gate-green.log`，8项通过 | 缺LICENSE/NOTICE、嵌套MIT、原文篡改、清单遗漏、错误统一许可、字段类型等；标准库工具 |
| 发行许可闭包 | `license-green-binary.json`、`license-archive-final-green.json` | 39组件；21 NuGet（含运行包）、9外部Skill、2系统资源引用、7知识包；23份固定原文/NOTICE、8份运行分发许可文件 |
| 实际ZIP解压启动 | `zip-extraction.json`、`windows-candidate.json` | 已封闭Windows ZIP实际解压至本轮自有、逐字节预验证的空数据staging；只保留一个自包含副本；511条目复核 |
| 新候选原生WPF | `window-accepted/result.json` / `checks.json` / `provenance.json`，37项通过，7截图，9实时采样；`native-exit.json`退出0 | 原生HWND及真实WPF控件/命令；不是网页，亦不是物理键盘IME验收 |
| 学习闭环 | 同上37项内：新档案、科目、408安装145节点、节点搜索编辑、任务及三计时模式、最近继续、补录、修订软删恢复、薄弱点、统计下钻、包更新冲突、备份恢复 | 使用新建合成SQLite；不宣称全部用户操作已手工通过 |
| 主/迷你真实刷新 | `window-accepted/timer-samples.json`，每模式3个不同真实时间点，共9条 | 自由计时00:00:01→02→03；倒计时/番茄剩余逐秒变化；暂停排除、主迷你同步；不改系统时间 |
| 知识树性能 | `window-accepted/ui-performance.json` | 本机10,000节点加载及搜索483.7029ms，搜索4.2298ms；单机样本，不是跨硬件承诺 |
| 实际数据隔离 | `isolation-and-restore.json`与各startup输出 | 探针输出实际Database.DirectoryPath，并查对应SQLite；独立空库0记录；默认路径实际为本轮程序旁`data-rc-v04`；没有仅依参数名推断隔离 |
| 关闭重开/另目录恢复 | 同上；`restore-other-directory.log` | 16张事实表逐行规范指纹一致；5学习记录、8片段、原始156053ms，Schema4，10,145节点，2知识包版本；完整性与外键检查通过 |
| 覆盖失败保护 | `restore-existing-target-rejected.log` | 再次恢复至已存在目标被拒绝，退出3762504530；源/目标已确认事实均未改变。工具异常退出是本负例预期结果 |
| 未签名确认 | `signature.json` | Authenticode为NotSigned；不等于SmartScreen实际行为已验收 |
| ZIP完整性 | 最终发行外部清单及各ZIP内部`manifest-sha256.json` | 封闭后重读全部条目CRC、字节数、SHA-256；源包/程序包实际许可门禁；截图旧清单单独逐项比对 |

恢复前后事实指纹：`48F75B628F1672A0C460B86BB131B57FBD8DE5645CB6108824DF9EE107DEAD98`。这是合成数据，不是用户学习内容。

## 复用有效证据，未重复执行

证据ZIP `reused-rc1-evidence/` 保留原始文件，不把这些结果计为本轮新运行。

- RC1 `core-final.trx`：197项核心测试通过；本轮Core/Data/业务测试代码未改，故不重跑全量。
- RC1 `migration-reconciliation.json`：Schema1/2/3→4与4幂等真实文件对账；V0.3.1旧格式升级/升级前备份回退的原始证据仍保留于RC1正式证据包。本轮不新增支持未知旧格式，也不执行真实用户升级。
- 前轮 `runtime-final.json`、`soak/result.json`、`samples.json`：900.29秒后台已发布ViewModel/WPF Dispatcher观察，891次读取；暂停3023.194ms、保存897234ms、对Stopwatch差6.445ms；私有内存159.45→163.75MiB、工作集采样峰值246.70MiB。它使用RC1 DLL，属于未改计时逻辑的复用证据，**不是本轮可见完整程序15分钟测试，也不是多日泄漏证明**。

## 失败与纠正记录（不覆盖）

1. `license-red-source.json` / `license-red-rc1.json`：原始缺发行声明证据。
2. `license-gate-tests.log`：初次默认临时目录沙箱权限错误；改用本轮自有隔离目录后通过。`license-shape-red.log`保留许可字段类型门禁负例失败，修复后8项通过。
3. 独立审查发现5份旧许可Git blob为LF、原文及运行ZIP为CRLF；`source-before-license-normalization.zip`与`license-archive-red.json`真实复现5项不匹配。普通add仍使用旧索引，首次`license-archive-green.json`**名称含green但内容仍为BLOCKED，不能作为通过证据**。
4. `git add --renormalize`仅对5份已核实原文操作；`index-license-proof.json`验证索引原文字节。提交`e195c3f`恢复原始CRLF，不改变许可条款；`source-license-verified-final.zip`及`license-archive-final-green.json`实际源归档门禁通过。没有重打Windows包、改写旧失败日志或伪称第一次修复即成功。
5. 首次联合下载是后续CC0请求403，Apache官方完整11358字节已成功。中间一份误归因日志保留，`fetch-log-correction.json`明确纠正；CC0最终从SPDX维护文本取得，来源独立记录。不执行远程脚本。

## 实际视觉检查与未覆盖项

`window-accepted/index.html`连同7张PNG一并入证据包。实际192DPI（200%），1200×800浅/深及900×600深色均有原生截图；代表性知识目录浅色与统计小窗深色已检查渲染，无本轮阻断布局问题。`package-conflict-dark-900.png`沿用文件名，**真实元数据为1200×800**；保留原图、原清单，并在索引明示，不能作为900小窗证据。

| 尚无实际证据 | 状态 | 发行影响 |
|---|---|---|
| 物理微软拼音候选窗、组合输入、焦点与五处中文字段 | NOT TESTED | 最小人工表约18分钟补齐；程序化中文设值不替代物理输入 |
| 其他物理DPI、多显示器切换 | NOT TESTED | 现有200%不推导其他缩放；有现成显示器才补测，不改系统设置 |
| 干净Win11完整离线启动、SmartScreen实际表现 | NOT TESTED | 无对应干净机/物理断网证据；已自包含、无云不等于实测离线通过 |
| 真实睡眠/休眠/重启/断电、完整应用多日运行 | NOT TESTED | 没有相关高风险授权；只提供后续步骤与恢复方案 |
| 真实用户数据库升级 | NOT TESTED | 本轮明确禁止；未来必须备份校验、单独授权，在新目录执行，不覆盖正式安装 |

当前没有已确认未修复的P0/P1；许可原文归档阻断已关闭。未测场景不是已确认故障，但也不能报告为通过。候选具备最终人工审批条件；对外Beta须披露上述限制并由用户接受；**Stable完整兼容验收尚未完成**，物理输入/干净机等仍需检查。技术准备不构成发布授权。

Skill：使用现有`yanji-domain-rules`保持计时/持久化不变量，使用现有执行计划、针对性调试/TDD与完成前验证指导；一次独立分支审查找出了源包许可换行问题。未安装新Skill或宣称本聊天自动发现了D盘项目Skill；未运行设计引擎或扩大UI改造。
