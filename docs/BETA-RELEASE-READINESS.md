# V0.4.0 Beta 发行准备与首次推送方案

日期：2026-10-18。**最终状态：BLOCKED。** 本文件不能作为公开、push、tag或Release授权。用户已确定Beta和目标仓库，但历史隐私未批准；图标/原A图的单独素材许可范围也仍需明确。

## Release Gate Matrix

| 项目 | 状态 | 证据与边界 |
|---|---|---|
| GitHub目标 | PASS | 用户创建`Chemic666/yanji`；API核对Public、main，ls-remote未见heads/tags；本地origin指向确认地址。配置remote不是push授权。 |
| 作者/邮箱及本机路径 | **BLOCKED** | `RELEASE-PRIVACY-REVIEW.md`和`legal/privacy-review-redacted.json`列出两组身份、文件、commit、数量及风险。尚未批准公开，不能推送。 |
| AI素材来源声明 | PASS WITH LIMITATION | 用户确认图标、A图、背景均AI生成且无第三方网站图片；精确文件哈希及仓库证据已核对，不伪造生成模型/账号/回执。 |
| 图标与A概念图公开许可 | **BLOCKED** | 来源确认不等于指定许可；需明确允许随仓库/程序分发及适用素材许可。背景沿用已记录Apache声明，截图继承画面内素材边界。 |
| 图像元数据与README | PASS WITH LIMITATION | 9个资源及7帧ICO未见PNG文本/EXIF身份信息；4张主页图取自最新UI-Copy真实WPF隔离截图，原字节未编辑。不是正式环境或新Beta构建截图，也不是隐写保证。 |
| Beta版本与构建 | PASS | `Directory.Build.props`为0.4.0-beta.1，实际DLL ProductVersion一致；File/AssemblyVersion为0.4.0.0；仅版本元数据和文档变更，C#/XAML/Schema/目录包不变。 |
| 相关定向验证 | PASS | 6项ProductCopyTests，0失败；新Beta原生WPF隔离启动2项检查通过，约1844ms，运行时10.0.13。未重跑全套业务或长时间测试。 |
| 既有计时/数据证据复用 | PASS WITH LIMITATION | 业务源码、数据路径隔离及冻结视觉未变；复用UI-Copy的29项相关测试、34项WPF检查、4598ms保存重开对账，只承认原有覆盖范围，不称本轮全部重测。 |
| 许可文件/候选来源 | PASS WITH LIMITATION | 新publish的许可文件门禁、实际EXE七帧图标检查通过；构建输入151项已冻结。封包后完整CRC、逐文件SHA与最终Git对应关系另有本地最终交付索引，不用文件存在冒充已验收。 |
| 物理IME/电源/其他DPI/干净用户/SmartScreen | NOT TESTED | 用户已接受作为Beta限制披露；README和Release Notes保留。不是Stable，不豁免P0/P1安全问题。 |
| 新的P0/P1数据安全问题 | PASS WITH LIMITATION | 本轮没有发现新的已证实故障；仅版本/文档改动和短启动检查，不能由此担保全部系统无缺陷。 |
| 公开推送、标签和Release授权 | **BLOCKED** | 本轮明确禁止，未上传源码、历史、附件或创建远程资源。 |

封包结果以本轮本地`releases/YanJi-v0.4.0-beta.1-<交付提交前缀>/PACKAGE-INFO.json`及`artifacts/beta-release-prep/FINAL.json`为准；不把旧RC包重命名冒充Beta。本轮最终提交及新增身份统计由FINAL记录，避免让文档自引用commit哈希形成无意义提交循环。

## Git历史与分支方案（仅准备，不执行）

- 保持本地`codex/v04-rc-polish2`，不为本次准备强行切main或改写历史。
- 基线90个祖先commit加本轮普通本地材料提交，作为拟公开主线；历史含I1/I2署名和本机路径，须先获批准。
- 目标：`origin/main`。首次推送计划只发送明确分支ref，不使用mirror/all/follow-tags；既有恢复分支和9个附注Tag不在首推范围。
- 若后续批准，可审查执行`git push --no-follow-tags origin codex/v04-rc-polish2:refs/heads/main`。**这只是方案文本，本轮未执行**。若目标分支届时已有新提交，先重新核对，不强推。
- 建议`v0.4.0-beta.1`目标为最终交付提交；未创建本地或远程标签。必须另外批准标签创建/推送，不把普通分支授权扩大为Release授权。
- GitHub自动生成的source archive与手工source.zip不包含相同元数据；源码包不含Git历史，但已有敏感路径仍在源码文件中。上传ZIP也不能绕过隐私许可。

## 计划附件

1. `YanJi-v0.4.0-beta.1-windows-x64.zip`。
2. `YanJi-v0.4.0-beta.1-source.zip`。
3. `SHA256SUMS.txt`。

本地审核证据另行保留，**不拟上传**原始TRX、构建诊断日志、数据库、备份或完整临时证据。这些原始文件可能包含计算机名、工具路径或环境元数据，即使是合成数据库，也不应批量提交或上传。正文、原始文件及哈希各自区分，不修改历史证据。

## 最少剩余决定

1. 批准或拒绝隐私清单中的I1姓名/QQ邮箱、I2工具身份及本机路径公开；明确接受完整主线历史还是另行授权处理方案。
2. 明确图标源/派生文件、原始A概念图允许的公开再分发范围和素材许可。来源已经确认AI生成，不再要求把“没有生成回执”误当成侵权事实。
3. 审核最终提交、包哈希与Beta限制后，单独授权要推送的分支、是否创建建议标签、是否创建/发布Release和上传哪些附件。当前仅建议标签，不擅自创建。

完成后停止；不访问真实学习库，不公开，不进入V0.5。旧`V040-RELEASE-GATES.md`是本轮前的历史审查，不代表上述用户新决策未生效；此文件和本次FINAL优先描述当前准备状态。
