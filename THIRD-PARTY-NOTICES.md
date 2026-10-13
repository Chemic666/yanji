# 第三方组件说明

研迹 V0.1.0 发布包包含以下运行依赖。组件许可证由对应上游提供，文本随发布包保存在 `licenses` 目录。本说明不代替完整许可证。

| 组件 | 版本 | 上游声明许可证 | 来源 |
|---|---|---|---|
| .NET / Windows Desktop Runtime | 10.0.12 | MIT 及随附第三方声明 | https://github.com/dotnet/runtime / https://github.com/dotnet/wpf |
| CommunityToolkit.Mvvm | 8.4.2 | MIT | https://github.com/CommunityToolkit/dotnet |
| Microsoft.Data.Sqlite / Core | 10.0.12 | MIT，Microsoft Corporation | https://github.com/dotnet/efcore |
| SQLitePCLRaw core / provider / bundle / native library package | 2.1.12 | Apache-2.0，SourceGear, LLC | https://github.com/ericsink/SQLitePCL.raw |

本机实际加载的原生 SQLite 引擎版本为 3.53.3；原生二进制由上述 SQLitePCLRaw NuGet 包提供。依赖精确版本见各项目 `packages.lock.json`。

发布包保留 CommunityToolkit 的 MIT 与第三方声明、.NET MIT 与第三方声明、WindowsDesktop MIT、SQLitePCLRaw Apache-2.0 文本。构建及测试依赖不随运行包打包，版本见 `Directory.Packages.props` 与测试项目锁文件。
