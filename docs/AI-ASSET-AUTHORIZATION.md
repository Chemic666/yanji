# YanJi AI素材来源与有限权利授权说明（维护者授权记录）

日期：2026-10-18。此文依据项目维护者的明确授权记录，不是新的权属调查结论，不修改图片、程序资源或已有发行包。软件代码继续采用项目现有Apache-2.0；第三方独立许可保持有效。

## 可用于公开材料的声明

维护者确认：下列研迹应用图标、原始A「澄境」概念图与窗景背景由AI工具生成，未采用第三方网站图片。维护者允许在其**依法有权许可的范围内**，将这些素材及所列派生资源按[Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0)随研迹公开、使用、修改和再分发，并遵守该许可要求。

此授权不保证AI生成内容在所有法域具备著作权适格性，不主张输出独占性、唯一性，不构成对尚未确认的第三方权利的授权或不侵权担保。不将AI图像标为人工原创，也不把AI工具名称当成版权许可本身。商标、系统字体及其他第三方资源不因本声明一并改用Apache-2.0。

真实WPF截图是软件实际运行的记录，不是AI设计图；对截图中项目依法可许可的界面及图像部分，维护者允许按相同范围随项目提供。此许可不延伸到截图中可能独立受保护的第三方内容、用户数据或字体软件本身。下表README截图只使用隔离合成数据，不表示真实个人学习记录或新Beta运行结果。

## 精确资源清单

| 文件 | 类型与实际用途 | 来源/许可依据及限制 |
|---|---|---|
| `design/branding/yanji-icon-v1.png` | AI生成图标源图 | 维护者来源确认及本轮有限权利Apache授权；未独立找到可绑定本文件的完整生成回执，准确模型、账号及当时适用条款尚未独立核实。 |
| `src/YanJi/Assets/Icons/YanJi.png` | 上项缩放导出的标准PNG，非另一张AI作品 | 继承源图在可许可范围内的Apache授权。 |
| `src/YanJi/Assets/Icons/YanJi.ico` | 16/24/32/48/64/128/256px派生Windows图标，EXE/主窗/托盘 | 同上；工程接入或哈希校验不构成独立权属认证。 |
| `design/quiet-focus-2/concepts/A-inspiration.png` | AI生成原始A「澄境」概念参考 | 不是WPF截图，也不是程序实际背景；维护者来源确认及本轮授权。完整原始调用回执未独立绑定，不编造生成平台或模型。 |
| `src/YanJi.App/Assets/Atmosphere/window-light-v1.png` | AI生成的静态窗景背景，WPF实际使用 | 同目录README记录2026-10-17项目OpenAI图像工具生成、提示词和输出名；A概念图用作氛围输入。无实时生成或网络图片加载。具体底层模型/账户未知。 |
| `docs/screenshots/today.png`、`progress.png`、`data.png`、`mini.png` | 真实Windows WPF HWND截图，README展示 | 来自`0.4.0-rc.quiet.2+ui-copy.1`，显式隔离合成数据，保留原图与“隔离测试”标记，不是当前Beta二进制新采集结果；来源和哈希见同目录`provenance.json`。 |

### 当前内容指纹

| 文件/资源 | SHA-256 |
|---|---|
| 图标源PNG | `07D610BAD2E7AA46E8DD6BA98B4CCB901E16A010F72D18886AEC5DEAB26A36DA` |
| 派生PNG | `71F93F33D4EA1A8D4DEF335D9C67B2D0C8B7FEE0C27BD1DBE40C6FE816EEB2FF` |
| 派生ICO | `5390EB4629A383397820457937CE483ACF8C6BD32D2A785DDADE99037CAF2010` |
| A概念图 | `E9C309231C85596CE76F2611BF912DADC7416F6AD5B5A945D2DCCAB4AC231087` |
| 窗景背景 | `524FCF84FCEF1114AA1DB50ECE0A0150B6180C708EAA47C9A580BE821A54D6CD` |

哈希只是来源追溯标识，不证明独占权利。未来文件内容如改变应更新指纹和来源说明，而不能在旧证据内悄悄替换。

## 第三方、条款与证据边界

- 项目未随上述素材分发Windows或Apple字体二进制；SimSun、Microsoft YaHei UI、Georgia、Cambria、Segoe等是系统字体族引用，不授予复制这些字体的权利。
- 知识包的CC0-1.0目录元数据及NuGet/其他第三方许可分别管理，不因本记录统一改标Apache；不授权商业教材、题库、讲义或官方大纲正文。
- 窗景工具记录见`src/YanJi.App/Assets/Atmosphere/README.md`。原始A和图标来源当前依据是维护者确认及既有项目记录；缺少完整回执本身不是已证实侵权，也不能据此虚构当时服务账号和适用条款。
- 若实际使用OpenAI服务，其Output规则仍以生成时适用服务、地区和账号协议为准；参考[个人使用条款](https://openai.com/policies/terms-of-use/)、[其他地区个人条款](https://openai.com/policies/row-terms-of-use/)及[服务协议](https://openai.com/policies/services-agreement/)。当前可查条款不能倒推未知的历史合同。此记录不依赖“所有AI图天然具有同一种版权”的假设。
- 复用既有未变化图片检查记录：PNG文本/EXIF块及ICO帧未发现身份/GPS元数据，截图中未见个人路径或真实学习数据；未做隐写检测。本轮不重编码图片来伪装正式环境，不新增用户身份或签名信息。


## 授权与变更范围

维护者已明确确认允许在依法可许可范围按Apache-2.0随项目公开、修改和再分发上述AI图标、派生资源及A概念图。此普通文档提交整理该决定，不修改图像或程序，不保证独占性或所有法域的著作权适格性，不构成GitHub上传授权。
