# 第三方组件、内容与许可清单

YanJi原创代码采用Apache-2.0；第三方内容及知识包保留各自条款。NOTICE保留适用上游声明，不能替代完整许可证。机器清单位于`resources/legal/third-party-licenses.json`；Windows包中为`third-party-licenses.json`，完整运行许可在`licenses/`。

“源码分发”指实际第三方组件材料随source ZIP提供，不把NuGet引用或许可证文本误计为组件源码。测试NuGet不随运行包发布。

| 组件 | 版本/提交 | 许可 | 源码分发 | 二进制分发 | 许可文件/依据 |
|---|---|---|---|---|---|
| communitytoolkit.mvvm | 8.4.2 | MIT | 否 | 是 | resources/licenses/CommunityToolkit-MIT.md<br>resources/licenses/CommunityToolkit-NOTICES.txt |
| microsoft.codecoverage | 18.10.1 | MIT | 否 | 否 | NuGet链接/元数据；无包内容再分发 |
| microsoft.data.sqlite | 10.0.12 | MIT | 否 | 是 | resources/licenses/EFCore-Sqlite-MIT.txt |
| microsoft.data.sqlite.core | 10.0.12 | MIT | 否 | 是 | resources/licenses/EFCore-Sqlite-MIT.txt |
| microsoft.net.test.sdk | 18.10.1 | MIT | 否 | 否 | NuGet链接/元数据；无包内容再分发 |
| microsoft.netcore.app.runtime.win-x64 | 10.0.12 | MIT | 否 | 是 | resources/licenses/DotNet-Runtime-MIT.txt<br>resources/licenses/DotNet-ThirdPartyNotices.txt |
| microsoft.testplatform.objectmodel | 18.10.1 | MIT | 否 | 否 | NuGet链接/元数据；无包内容再分发 |
| microsoft.testplatform.testhost | 18.10.1 | MIT | 否 | 否 | NuGet链接/元数据；无包内容再分发 |
| microsoft.windowsdesktop.app.runtime.win-x64 | 10.0.12 | MIT | 否 | 是 | resources/licenses/WindowsDesktop-MIT.txt<br>resources/licenses/DotNet-ThirdPartyNotices.txt |
| sqlitepclraw.bundle_e_sqlite3 | 2.1.12 | Apache-2.0 | 否 | 是 | resources/licenses/SQLitePCLRaw-Apache-2.0.txt |
| sqlitepclraw.core | 2.1.12 | Apache-2.0 | 否 | 是 | resources/licenses/SQLitePCLRaw-Apache-2.0.txt |
| sqlitepclraw.lib.e_sqlite3 | 2.1.12 | Apache-2.0 | 否 | 是 | resources/licenses/SQLitePCLRaw-Apache-2.0.txt |
| sqlitepclraw.provider.e_sqlite3 | 2.1.12 | Apache-2.0 | 否 | 是 | resources/licenses/SQLitePCLRaw-Apache-2.0.txt |
| xunit | 2.9.3 | Apache-2.0 | 否 | 否 | NuGet链接/元数据；无包内容再分发 |
| xunit.abstractions | 2.0.3 | Apache-2.0 | 否 | 否 | NuGet链接/元数据；无包内容再分发 |
| xunit.analyzers | 1.18.0 | Apache-2.0 | 否 | 否 | NuGet链接/元数据；无包内容再分发 |
| xunit.assert | 2.9.3 | Apache-2.0 | 否 | 否 | NuGet链接/元数据；无包内容再分发 |
| xunit.core | 2.9.3 | Apache-2.0 | 否 | 否 | NuGet链接/元数据；无包内容再分发 |
| xunit.extensibility.core | 2.9.3 | Apache-2.0 | 否 | 否 | NuGet链接/元数据；无包内容再分发 |
| xunit.extensibility.execution | 2.9.3 | Apache-2.0 | 否 | 否 | NuGet链接/元数据；无包内容再分发 |
| xunit.runner.visualstudio | 4.0.1 | Apache-2.0 | 否 | 否 | NuGet链接/元数据；无包内容再分发 |
| skill:ui-ux-pro-max | 7246805992a42b7bba37d4cac83b01416f0c6c44 | MIT | 是 | 否 | .agents/skills/ui-ux-pro-max/UPSTREAM-LICENSE |
| skill:impeccable | 245ad82def45831dc56a21b683c7a1ff530b06cc | Apache-2.0 | 是 | 否 | .agents/skills/impeccable/UPSTREAM-LICENSE<br>.agents/skills/impeccable/UPSTREAM-NOTICE.md<br>docs/legal/third-party/modern-screenshot-MIT.txt<br>docs/legal/third-party/platform-design-skills-MIT.txt |
| skill:redesign-skill | d237dd399d57524945c0c81327da5da19f7a468d | MIT | 是 | 否 | .agents/skills/redesign-skill/UPSTREAM-LICENSE |
| skill:soft-skill | d237dd399d57524945c0c81327da5da19f7a468d | MIT | 是 | 否 | .agents/skills/soft-skill/UPSTREAM-LICENSE |
| skill:make-interfaces-feel-better | 35545ea1512ad59fa463e6b1f95ca9c052981fe6 | MIT | 是 | 否 | .agents/skills/make-interfaces-feel-better/UPSTREAM-LICENSE |
| skill:run-tests | 9de888b8ce55da34e76a113bd7b2d463d6b3e1e6 | MIT | 是 | 否 | .agents/skills/run-tests/LICENSE.txt |
| skill:coverage-analysis | 9de888b8ce55da34e76a113bd7b2d463d6b3e1e6 | MIT | 是 | 否 | .agents/skills/coverage-analysis/LICENSE.txt |
| skill:breakdown-feature-prd | f25421d3f1b8b3bb217f78a7c0567cf7ec117cd8 | MIT | 是 | 否 | .agents/skills/breakdown-feature-prd/LICENSE.txt |
| skill:winui-ui-testing | d399e3151db3a6e549ca4ac9d9cbeefa21260ed8 | MIT | 是 | 否 | .agents/skills/winui-ui-testing/LICENSE.txt |
| Windows system fonts | Windows 11 system installation | Windows system-font usage terms; no font-file redistribution | 否 | 否 | 系统接口使用；无资源文件再分发 |
| Windows application system icon | SystemIcons.Application | Windows/.NET provided resource usage; no extracted icon file | 否 | 否 | 系统接口使用；无资源文件再分发 |
| org.yanji.reference.408 | 1.1.0 + historical 1.0.0 | CC0-1.0 | 是 | 是 | resources/licenses/KnowledgePack-CC0-1.0.txt |
| org.yanji.reference.blank | 1.0.0 | CC0-1.0 | 是 | 是 | resources/licenses/KnowledgePack-CC0-1.0.txt |
| org.yanji.reference.english1 | 1.1.0 + historical 1.0.0 | CC0-1.0 | 是 | 是 | resources/licenses/KnowledgePack-CC0-1.0.txt |
| org.yanji.reference.english2 | 1.1.0 + historical 1.0.0 | CC0-1.0 | 是 | 是 | resources/licenses/KnowledgePack-CC0-1.0.txt |
| org.yanji.reference.math1 | 1.1.0 + historical 1.0.0 | CC0-1.0 | 是 | 是 | resources/licenses/KnowledgePack-CC0-1.0.txt |
| org.yanji.reference.math2 | 1.1.0 + historical 1.0.0 | CC0-1.0 | 是 | 是 | resources/licenses/KnowledgePack-CC0-1.0.txt |
| org.yanji.reference.politics | 1.0.0 | CC0-1.0 | 是 | 是 | resources/licenses/KnowledgePack-CC0-1.0.txt |

上游来源、版权/NOTICE保留要求、精确文本SHA见机器清单；NuGet作者/版权元数据见源码`docs/legal/nuget-dependencies.json`。原生SQLite核心为公有领域，SQLitePCLRaw包装层为Apache-2.0，两者不同。
.NET完整集合第三方声明、CommunityToolkit NOTICE和Impeccable NOTICE保持全文。字体仅按名称使用，未打包字体文件；图表/导航为原生代码绘制，未替换已有应用图标。

知识包CC0只覆盖自编参考目录，不授权外部教材/课程/题库；408不是官方年度完整大纲。Skill只含字体/图标索引元数据，没有对应资产文件。用户数据库和备份不受代码许可影响，也不随发行分发。
