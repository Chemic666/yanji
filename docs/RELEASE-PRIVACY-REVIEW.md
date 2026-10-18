# Git历史隐私审核清单（脱敏，待维护者批准）

**状态：BLOCKED。未批准公开作者邮箱和本机路径，不得推送。**

目标仓库为`Chemic666/yanji`，本地分支`codex/v04-rc-polish2`拟在后续批准后推到远程`main`。基线`16db09c7f9afee8af4b3bfa045e3f14e49ffc968`有90个祖先提交，未压缩、未删除历史。本轮准备提交也将成为待审批历史的一部分，继续使用I1当前配置，不修改署名。最终新增提交及精确统计见本地交付索引，不能用本报告的基线数字冒充最终所有提交数。

## 两组身份

| 标识 | 脱敏名称/邮箱 | 基线出现数量 | 公开后的可见性、风险与建议 |
|---|---|---|---|
| I1 | `C***c` / `27***@qq.com`；名称指纹`d28b44013fb4a051`，邮箱指纹`ac376d31c3601494` | 80个Author + 80个Committer，实际80个提交，不能相加称160次贡献 | 若推送完整主线，GitHub/API/克隆中的原始commit可读取完整姓名和邮箱。可能关联个人联系方式、垃圾邮件及身份。**需明确接受公开，否则暂不推送**。 |
| I2 | `C***x` / `co***@localhost`；名称指纹`616efbe96852d8c9`，邮箱指纹`63dc9f4a8bd6ff91` | 10个Author + 10个Committer，实际10个提交 | localhost为开发工具本地身份，不是可投递的个人联系邮箱。真实隐私风险低，但会公开工具署名且可能不计入GitHub贡献；可保留真实追溯，不伪造归属。 |

每组具体commit ID在`legal/privacy-review-redacted.json`的AuthorCommits/CommitterCommits中，可区分两组，不含完整敏感邮箱。GitHub账户更名（Chemic666/zhan-cm同一账户）不自动修改历史作者邮箱，也不能证明某邮箱已经绑定GitHub。

另有9个既有附注标签的tagger使用I1。**首推方案排除全部历史tag refs**，不使用follow-tags；因此tagger对象不能与主线90个commit混作自动公开范围。若之后推旧标签，需要再次批准这些tagger身份。当前`v0.4.0-beta.1`仅是建议，未创建。

## 本机路径

以下为当前HEAD七个文件的七处用户目录字面量；十个唯一历史blob中各有一处。按所有90个祖先快照重新定位，逐blob可见快照数量和代表commit保存在脱敏JSON，不以同一blob出现在多个提交中伪造更多独立泄露。

| 文件/行 | 脱敏信息片段 | 数量、风险与建议 |
|---|---|---|
| `docs/V04-CURRENT-ARCHITECTURE.md:125` | `C:/Users/C***c/Documents/Codex/.../work/v04-m0/schema-probe.json` | 当前1处；对应blob可见于36个基线快照。P2本地配置名/工具链路径；无凭据。可经用户接受后保留，否则需要另行处理方案。 |
| `tools/Invoke-Build.ps1:3` | `C:\Users\C***c\Documents\Codex\2026-10-13\yanji-windows-windows-11-2028-1` | 当前1处；对应blob可见于89个基线快照。P2本地配置名/工具链路径；无凭据。可经用户接受后保留，否则需要另行处理方案。 |
| `tools/Invoke-V022Run.ps1:4` | `C:\Users\C***c\Documents\Codex\2026-10-13\yanji-windows-windows-11-2028-1\work\phase1\toolchain` | 当前1处；对应blob可见于66个基线快照。P2本地配置名/工具链路径；无凭据。可经用户接受后保留，否则需要另行处理方案。 |
| `tools/Invoke-VisualLab.ps1:8` | `C:/Users/C***c/Documents/Codex/2026-10-13/yanji-windows-windows-11-2028-1/work/phase1/toolchain/sdk-path.txt` | 当前1处；对应blob可见于80个基线快照。P2本地配置名/工具链路径；无凭据。可经用户接受后保留，否则需要另行处理方案。 |
| `tools/Package-Release.ps1:1` | `C:\Users\C***c\Documents\Codex\2026-10-13\yanji-windows-windows-11-2028-1\outputs` | 当前1处；对应blob可见于88个基线快照。P2本地配置名/工具链路径；无凭据。可经用户接受后保留，否则需要另行处理方案。 |
| `tools/Package-V011.ps1:1` | `C:\Users\C***c\Documents\Codex\2026-10-13\yanji-windows-windows-11-2028-1\outputs\v0.1.1` | 当前1处；对应blob可见于82个基线快照。P2本地配置名/工具链路径；无凭据。可经用户接受后保留，否则需要另行处理方案。 |
| `tools/Package-V02.ps1:1` | `C:\Users\C***c\Documents\Codex\2026-10-13\yanji-windows-windows-11-2028-1\outputs\v0.2-quiet-focus` | 当前1处；对应blob可见于74个基线快照。P2本地配置名/工具链路径；无凭据。可经用户接受后保留，否则需要另行处理方案。 |


公开后，当前源码和历史版本均可能被下载或搜索。上表私有用户目录在这里均被遮盖；原源码没有被本轮修改，所以**只有报告脱敏，不代表历史内容已脱敏**。

普通`D:\Projects\yanji`、`artifacts/...`及版本/运行编号通常只透露工程组织，不属于账号凭据，建议保留。绝对路径启发式发现63个唯一文本blob（含样例、正则和历史版本）；文件、对象ID、行号、命中数在JSON的AbsolutePathBlobs中，不能把全部63项称为真实个人路径。上游Avalonia/JavaFX示例和Package-V021脱敏正则已经排除出真实用户目录清单。

## 其他隐私检查

- 计算机名：此前39个模式命中是desktop-first、desktop-only、CLI词或desktop测试编号；没有确认真实主机名。当前计算机名精确匹配0。未据名称模式虚构设备泄露，也不保证涵盖所有可能设备标识格式。
- 凭据/数据：沿用并复核基线可达历史启发式扫描，90commit、998唯一blob、1580对象（含其他可达类型），凭据规则0命中；未发现被跟踪的学习SQLite、WAL/SHM、备份、日志、转储或运行包。不是绝对无秘密证明。
- 开发元数据：当前172项`.agents`是刻意保留的开发Skill/许可证/索引，不是用户会话、凭据或私有库；未发现被跟踪的`.vs/.idea/.vscode/.codex`用户配置、`.user/.suo`。旧工具脚本含本机路径已列出，不重复当作独立泄露。
- 图像：基线9个图像blob均为当前资源，没有额外旧照片blob。README已换成真实UI-Copy隔离截图；旧图仍存在Git历史。逐图目视无个人账号/路径/真实记录；PNG文本/EXIF检查未发现身份信息。未做OCR或隐写鉴证。
- 早期A/B原型：HTML、DESIGN/VALIDATION、浏览器证据等仍在本地未跟踪状态，本轮不暂存、不打入源码ZIP。**原始A-inspiration.png已经被跟踪且存在历史中**，不能与未跟踪B原型混淆。

## 是否需要改历史

| 选择 | 是否需要历史重写 | 限制 |
|---|---|---|
| 用户明确接受现有身份和路径公开，推完整主线 | 不需要 | 这属于隐私决策，不默认获得同意。 |
| 仅在新提交改邮箱、加.mailmap、清理当前文档 | 不能消除旧历史 | 只影响未来或显示；旧对象仍有原值。此轮未做。 |
| 要求完整主线内不再包含旧邮箱或路径 | 通常需要另行审查的历史重写或重建公开历史 | 此轮严格禁止，不能自行Squash/filter/rebase或强推。 |
| 用户以后选择从干净快照建立公开仓库 | 不重写当前本地历史，但会改变公开历史范围 | 需要单独授权；仍需审查当前树路径，不是直接推现有main。 |

本轮建议保持历史和真实署名，先由用户逐项决定。完整邮箱不写入公开候选文档；不要把本清单上传当作对原历史的批准。
