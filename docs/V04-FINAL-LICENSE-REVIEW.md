# V0.4 RC1 开源许可与版权审查

审查对象：HEAD `ce217d3839e1a181884335c7a110e6f98e568269`的431个跟踪文件、全部锁文件及四个不变的RC1 ZIP。程序基线仍为`e0e710c`。状态：**READY FOR FINAL APPROVAL**（供所有者选择许可证），不是已完成开源合规声明。

## 推荐：Apache-2.0

| 方案 | 对研迹的意义 | 代价/限制 | 建议 |
|---|---|---|---|
| MIT | 简短、宽松，利于复用和商业集成 | 需保留版权与许可；没有Apache式明确专利授权条款 | 若目标仅为最少许可管理，可选 |
| Apache-2.0 | 宽松分发及修改；明确贡献者专利授权和专利诉讼终止条款，适合持续接受贡献 | 保留许可及适用NOTICE、版权声明；修改文件须标明修改；不授予商标权 | **首选**，与当前MIT/Apache依赖组合相容 |
| GPL-3.0 | 分发受GPL约束的组合/修改版时，要求按相同条件提供对应源码 | 下游闭源集成受限；对应源码和分发条件管理更多 | 仅当“下游修改分发仍须开源”是核心政策时选；还须决定only或or-later |

这是对当前项目的适用性判断，不能代替所有者授权。[Apache原文第3、4、6节](https://www.apache.org/licenses/LICENSE-2.0.txt)、[MIT原文](https://opensource.org/license/mit)、[GPLv3原文及分发条件](https://choosealicense.com/licenses/gpl-3.0/)、[Apache对GPLv3兼容性的说明](https://www.apache.org/licenses/GPL-compatibility)支持以上比较。根许可证只覆盖所有者有权授权的内容，不能把第三方MIT/Apache文件改称完全自有，也不应追溯改变知识包既有许可。

**本轮没有添加根LICENSE、填写版权人或选择GPL的具体变体，没有宣称已完成开源合规。** 用户确认后再生成根许可证及相应声明，并重新生成正式源码归档；旧RC1证据不改写。

## 全部NuGet依赖

21个唯一ID/版本均从实际项目锁文件及RC运行包清单取出；详细UsedBy、nuspec哈希、原许可链接和包内许可文本哈希见`artifacts/v04-final-approval/nuget-all-dependencies.json`。以下不是仅列直接依赖。

| 包 | 版本 | 许可 | 分发范围 |
|---|---|---|---|
| CommunityToolkit.Mvvm | 8.4.2 | MIT | 运行 |
| Microsoft.Data.Sqlite / Microsoft.Data.Sqlite.Core | 各10.0.12 | MIT | 运行 |
| Microsoft.NETCore.App.Runtime.win-x64 / Microsoft.WindowsDesktop.App.Runtime.win-x64 | 各10.0.12 | MIT与各自第三方声明 | 自包含运行 |
| SQLitePCLRaw.bundle_e_sqlite3 / core / provider.e_sqlite3 / lib.e_sqlite3 | 各2.1.12 | Apache-2.0 | 运行 |
| Microsoft.NET.Test.Sdk / CodeCoverage / TestPlatform.ObjectModel / TestPlatform.TestHost | 各18.10.1 | MIT | 测试，不在RC运行包 |
| xunit / assert / core / extensibility.core / extensibility.execution | 各2.9.3 | Apache-2.0 | 测试 |
| xunit.analyzers | 1.18.0 | Apache-2.0 | 构建/测试 |
| xunit.runner.visualstudio | 4.0.1 | Apache-2.0 | 测试 |
| xunit.abstractions | 2.0.3 | Apache-2.0，旧nuspec只给链接 | 测试 |

最后一项的原始nuspec结果仍原样保留为缺少表达式；本轮另核对[官方冻结接口仓库](https://github.com/xunit/abstractions.xunit)及其[许可声明](https://raw.githubusercontent.com/xunit/abstractions.xunit/main/license.txt)，保存补充证据，不回写旧审计为“原本已核实”。上游main文本快照与2018包构建提交不等同。

原生SQLite核心是[公有领域](https://www.sqlite.org/copyright.html)，不能把它与SQLitePCLRaw包装层许可混为一谈。RC实际SQLite3.53.3；不发布上游构建脚本。自包含包的.NET第三方集合必须保留完整`DotNet-ThirdPartyNotices.txt`，不能因主项目选Apache而删去其中的其他许可。

RC运行ZIP内七份`licenses/*`已再次按原清单核对。没有增加运行依赖，没有随运行包分发xUnit或开发Skill。

## 字体、图标和视觉资源

- 对全部跟踪文件及运行ZIP检查：未发现TTF/OTF/WOFF、SVG/ICO/PNG/JPG等独立资源文件；图表由`StudyChart.cs`绘制，导航图标为`FocusIcon.cs`中原生几何路径。
- `Styles.xaml`、计时器及图表引用Segoe UI Variable、Segoe UI、Microsoft YaHei UI。仅调用用户Windows系统字体，没有复制字体文件；[微软字体FAQ](https://learn.microsoft.com/en-us/typography/fonts/font-faq)允许应用使用系统字体，不能据此取得字体再分发权。
- 托盘调用`SystemIcons.Application`，未提取或打包Windows系统图标。现有通用几何图标的提交来源可追溯，但静态审查不能证明每个简单路径在世界范围内独创或无商标冲突。
- Google字体和Phosphor目录仅存在于设计Skill的JSON索引，未带字体、图标矢量或渲染资产；不能据这些目录的元数据宣称全部字体许可已被研迹获取。
- 未发现商业教材图像、课程截图、题库附件或外部设计图片。此结论针对可检查的仓库和RC范围，不是对所有历史来源的法律保证。

## 开发Skill和源码归档：需要单独保留的声明

| 来源 | 已有版本证据 | 许可/须保留文本 |
|---|---|---|
| UI/UX Pro Max | 7246805，manifest2.15.0 | MIT，`.agents/skills/ui-ux-pro-max/UPSTREAM-LICENSE` |
| Taste：redesign-skill、soft-skill | d237dd3，manifest1.0.0 | MIT，各UPSTREAM-LICENSE；docs设计参考也保留同来源 |
| Make Interfaces Feel Better | 35545ea | MIT，原版权与许可 |
| Impeccable | 245ad82，4.5.2；本地脚本VERSION是引擎0.1.14，不能当Skill版本 | Apache-2.0及UPSTREAM-NOTICE.md |
| run-tests、coverage-analysis | dotnet/skills@9de888b | MIT，各LICENSE.txt |
| winui-ui-testing | microsoft/winappCli@d399e31 | MIT，LICENSE.txt |
| breakdown-feature-prd、WPF参考指令 | awesome-copilot@f25421d | MIT，目录内及docs/skills许可 |
| skill-creator参考许可 | openai/skills@49f948f | Apache-2.0，docs/skills原许可；并非运行依赖 |

**本轮确认两处嵌套声明缺口**：Impeccable的`modern-screenshot.umd.js`没有嵌入MIT声明；ios/android指南的上游ehmo MIT只在NOTICE里标了来源，没有完整MIT文本。不能仅保留Impeccable Apache文件便认为足够。

补充原文保存在`docs/legal/third-party/modern-screenshot-MIT.txt`和`platform-design-skills-MIT.txt`；归属与对应文件见`docs/legal/THIRD-PARTY-SUPPLEMENT.md`。脚本字节与记录的Impeccable固定提交**完全一致**，哈希见embedded-script-origin.json；未执行下载脚本。补充许可证来自官方原文main快照，记录内容SHA而不伪造获取到精确上游提交（GitHub API查询遭403限流，原始失败保留）。

原RC1 source ZIP保持不变，单独公开分发它之前必须并入这些声明或随包附上本轮补充材料；正式源码应在最终许可决定后重新归档。**原RC1源码包单独公开分发：BLOCKED**；运行包不含这些Skill，未发现对应的运行包漏声明问题。

## 七个知识包

`resources/knowledge-packages`全部七包保留PackageId、版本、来源、哈希和CC0-1.0声明。408为145个独立组织的参考目录节点，数学一45、数学二33、英语一/二各17、政治11、空白0。按节点标题/备注抽查，未发现复制教材正文或题目；不能据此声称经过逐字跨互联网相似度检索。

来源及未核实范围见`V04-M2-TEMPLATE-SOURCES.md`。CC0仅针对自编目录元数据，外部参考站点/课程材料不因此获得再分发权。必须继续标注“参考目录，非2028官方大纲”，特别是政治和数学适用年度/范围缺口。正式开源前由所有者确认自编内容授权权利；后续社区包须各自保留来源/许可，不能默认覆盖为项目Apache许可。

## 风险与所有者决定

| 等级 | 项目 | 当前处理 |
|---|---|---|
| 发布决策阻断 | 根许可和版权人尚未决定 | 等待用户正式选择；不猜姓名、不填空白身份 |
| P1源码分发声明缺口 | 两处开发Skill嵌套MIT缺失 | 原文与归属补充已准备；正式源包须并入，旧证据不重写 |
| P2内容适用性 | 参考模板不能认证最新年度大纲 | 来源/边界保留，禁止营销为官方完整大纲 |
| P2追溯边界 | 部分上游只有main许可快照，未获取精确嵌套构建版本；无法证明所有原创权属 | 保留SHA、上游链接及未知项，用户确认其有权授权自身贡献 |

建议选择Apache-2.0用于项目代码及自有文档，保留第三方原许可和知识包既有CC0边界。随后一次性生成最终许可证/声明和正式归档。本轮未作法律认证、在线漏洞审计或商标注册检索。
