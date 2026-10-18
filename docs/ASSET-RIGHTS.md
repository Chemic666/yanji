# 图像、字体与参考目录的来源和许可边界

更新：2026-10-18。维护者已进一步批准图标、派生资源与A概念图在依法可许可范围按Apache-2.0提供；完整范围见[AI素材授权记录](AI-ASSET-AUTHORIZATION.md)。本次只更新授权记录，不改变素材。

此前来源审查：2026-10-18，依据维护者本轮明确确认：应用图标、A「澄境」概念图及窗景均由AI工具生成，没有使用第三方网站图片。此确认改善了来源证据，但**不把所有文件自动改为Apache-2.0**。下表区分维护者声明、仓库记录和本轮独立检查。

| 精确资源 | 类型、实际使用与来源证据 | 再分发结论 |
|---|---|---|
| `design/branding/yanji-icon-v1.png`，SHA256 `07D610BAD2E7AA46E8DD6BA98B4CCB901E16A010F72D18886AEC5DEAB26A36DA` | AI源图；维护者确认。既有对话要求GPT图像生成并提供此精确文件接入；没有可独立绑定的完整原始生成回执或确定的模型/账号记录。 | **来源确认；维护者已授予依法可许可范围的Apache-2.0授权**。不编造具体模型、账户条款或原创版权担保。 |
| `src/YanJi/Assets/Icons/YanJi.png`，SHA256 `71F93F33D4EA1A8D4DEF335D9C67B2D0C8B7FEE0C27BD1DBE40C6FE816EEB2FF` | 上项缩放导出256px PNG，不是另一张AI作品。 | 继承源图边界。 |
| `src/YanJi/Assets/Icons/YanJi.ico`，SHA256 `5390EB4629A383397820457937CE483ACF8C6BD32D2A785DDADE99037CAF2010` | 上项派生16/24/32/48/64/128/256px资源，EXE、主窗及托盘使用。 | 继承源图边界；工程资源验证不等于权利认证。 |
| `design/quiet-focus-2/concepts/A-inspiration.png`，SHA256 `E9C309231C85596CE76F2611BF912DADC7416F6AD5B5A945D2DCCAB4AC231087` | AI设计概念参考，不是真实WPF截图，也不是运行背景。维护者确认来源；旧设计scope记录imagegen使用，但精确调用回执未独立找到。 | **来源确认；维护者已授予依法可许可范围的Apache-2.0公开授权**。旧内部冻结说明是当时记录，本次授权适用于依法可许可的范围；图不是WPF截图。 |
| `src/YanJi.App/Assets/Atmosphere/window-light-v1.png`，SHA256 `524FCF84FCEF1114AA1DB50ECE0A0150B6180C708EAA47C9A580BE821A54D6CD` | 项目OpenAI图像生成工具记录：2026-10-17，输出`exec-c23ad7b7-75cc-463d-98ae-30c35ef70834.png`；同目录README保留提示词，A概念作氛围输入，实际为静态WPF背景。 | 原有说明按项目Apache-2.0提供可保护的项目权利，受法律/条款限制；维护者确认输入也是AI图，无第三方网站素材。但不保证版权适格性、独占或无第三方相似作品。 |
| `docs/screenshots/{today,progress,data,mini}.png` | 最新UI-Copy基线真实原生WPF截图，显式隔离合成数据，未改图；不是AI概念或正式用户环境。精确程序集、源名和哈希见screenshots/provenance.json。 | 截图说明和项目软件不改变画面内素材的权利；使用和再分发继承图标/背景边界。 |
| 字体与代码图形 | SimSun、Microsoft YaHei UI、Georgia、Cambria、Segoe UI/Variable仅以系统字体族名称引用；图形来自既有WPF代码及应用图标。 | 不分发Windows/Apple字体文件，不因此授权系统字体再分发。截图输出权限与复制字体二进制不同。 |
| 参考知识目录 | 自编数学/英语/408等目录元数据，包内维护者、来源和版本声明，CC0-1.0。 | CC0不覆盖外部网页、商业教材、题库、讲义或大纲正文，不宣称2028官方完整考纲。 |

## 元数据与隐私

本轮按PNG块结构和CRC核对源图、概念图、背景、派生PNG及四张README截图；未发现`tEXt/zTXt/iTXt/eXIf`块。ICO帧也单独检查。没有发现嵌入的文本/EXIF身份或GPS信息；不等于完成隐写检测。图片字节没有为清除标识而被重写，源图、旧图及历史保留。目视检查未见真实人物、用户名、账户、路径或真实学习记录。

## AI条款与证据限制

2026-10-18查阅[OpenAI非欧洲地区个人条款](https://openai.com/policies/row-terms-of-use/)、[欧洲地区个人条款](https://openai.com/policies/terms-of-use/)及[服务协议](https://openai.com/policies/services-agreement/)。适用规则取决于实际服务、账号、地区和生成时有效条款，未凭当前条款倒推出未知历史生成账号。一般Output归属只约束条款当事人且受法律限制，不保证输出独占、版权适格性或第三方权利。不能冒充人工原创。

维护者确认可作为来源证据，不虚构不存在的生成回执；缺少回执本身不是已证实侵权。图标源图、派生资源与原A概念图的有限权利Apache-2.0授权现已明确，见AI-ASSET-AUTHORIZATION.md。不保证版权适格性、独占性或第三方权利。素材授权确认不代表历史隐私清理、程序验收或公开推送自动获得批准。

代码Apache-2.0、第三方条款完整性、素材来源、素材许可和个人隐私是独立门槛；一项通过不能替代其他项。本文件是工程证据说明，不提供不侵权或法律权属保证。
