# 来源、依赖与许可核查

运行依赖未升级或新增。实际NuGet元数据、固定版本仓库和本机安装文本均已核对，完整审计见证据包 `dependency-license-audit.json`。

| 组件 | 版本 | 声明与随包材料 |
|---|---|---|
| .NET Runtime / WindowsDesktop | 10.0.12 | MIT及第三方声明，来自已安装对应运行包 |
| CommunityToolkit.Mvvm | 8.4.2 | MIT与ThirdPartyNotices，来自安装包 |
| Microsoft.Data.Sqlite / Core | 10.0.12 | MIT；固定版本完整文本已纳入 |
| SQLitePCLRaw core/provider/bundle/native包 | 2.1.12 | Apache-2.0，固定版本全文已纳入 |
| xUnit / runner / Test.Sdk | 2.9.3 / 4.0.1 / 18.10.1 | 安装包许可声明已记录；测试组件不随运行包分发 |

完整运行许可在源码 `resources/licenses`，通过项目配置实际复制至RC的 `licenses`。保留官方文本原文，包括第三方声明末尾空行，不为格式检查改写许可。

来源：[CommunityToolkit包](https://www.nuget.org/packages/CommunityToolkit.Mvvm/8.4.2)、[Sqlite包](https://www.nuget.org/packages/Microsoft.Data.Sqlite/10.0.12)、[EFCore固定许可](https://raw.githubusercontent.com/dotnet/efcore/v10.0.12/LICENSE.txt)、[SQLitePCLRaw固定许可](https://raw.githubusercontent.com/ericsink/SQLitePCL.raw/v2.1.12/LICENSE.TXT)。

七个内置知识包均保留版本、内容哈希、来源链接与许可。408含145节点，原创目录结构与元数据声明CC0-1.0；外部资料的正文或题目不随包复制。数学、英语、政治等按现有基础参考范围交付，不声称为官方2028大纲。详细来源沿用 `V04-M2-TEMPLATE-SOURCES.md`，本轮另生成 `template-audit.json`。

设计/开发Skill不作为运行时依赖，源码保留既有来源和许可；本轮复用领域规则、WPF参考及独立审查工作流，没有安装新Skill或执行远程安装脚本。

**尚无项目根LICENSE文件。** 项目本身的开源许可、署名和正式公开发布须由所有者确认，本轮不代选、不公开发布。此项不阻止本地候选包人工验收，但阻止将其当作已完成开源发行手续。未执行在线漏洞审计、签名或全面法律审查。
