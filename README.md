# 研迹 YanJi V0.1

Windows 11 x64 本地考研备考管理客户端。2028 指入学年份，考试日期可留空。无账号、服务器、云同步或 AI。

正式源码：`D:\Projects\yanji`。自包含程序放在 `releases\YanJi-v0.1.0-win-x64\YanJi.exe`；完整解压发布包后双击启动，无需安装 .NET SDK。日常使用不需要阅读代码。

- 使用、计时、补录、备份恢复：[使用说明](docs/USER-GUIDE.md)
- 实际执行结果与未测边界：[验证报告](docs/VALIDATION.md)
- 模块、事实来源与可靠性：[架构与决策](docs/ARCHITECTURE.md)
- 开发进度与恢复点：[开发记录](docs/DEVELOPMENT.md)

## 实现范围

今日首页，计时/暂停/继续/结束，纯时长及明确区间补录，记录修正/软删除/恢复/历史，学习日历与每日感想，科目/章节归档，独立的进度和掌握状态，简单任务，薄弱点及复习/验收状态，日/周/月统计，SQLite，完整备份/校验/恢复，CSV，托盘及单实例，异常时间核对。

当前交付是本机验证的可用 V0.1。真实锁屏、Modern Standby、休眠、系统时间调整、物理断网和干净 Windows 环境仍未测试；不宣称无条件正式发布验收已完成。

## 开发构建（可选）

使用 .NET SDK 10.0.401 或 global.json 允许的 .NET 10 SDK。所有依赖来自项目 NuGet.Config 中的官方源；版本记录于 Directory.Packages.props 和 packages.lock.json。

```powershell
dotnet restore YanJi.slnx --configfile NuGet.Config
dotnet test tests/YanJi.Tests/YanJi.Tests.csproj -c Release --no-restore
dotnet publish src/YanJi.App/YanJi.App.csproj -c Release -r win-x64 --self-contained true -p:RestoreConfigFile=NuGet.Config -o artifacts/publish
```

当前机器使用任务内便携 SDK，没有修改系统 .NET、PATH 或注册表。`tools/Invoke-Build.ps1` 优先使用 `YANJI_DOTNET_ROOT`、当前任务便携 SDK 或已安装的 dotnet，并为构建进程隔离配置与缓存；调用示例：

```powershell
./tools/Invoke-Build.ps1 -Arguments @('test','tests/YanJi.Tests/YanJi.Tests.csproj','-c','Release','--no-restore','-m:1','-nr:false','-p:UseSharedCompilation=false')
```

## 隔离验证入口

不要用正式数据目录运行验证；下列目录必须是新的实验目录。

```powershell
YanJi.exe --data-dir D:/Projects/yanji/artifacts/new-run/data --smoke D:/Projects/yanji/artifacts/new-run/report
YanJi.exe --data-dir D:/Projects/yanji/artifacts/new-perf/data --performance D:/Projects/yanji/artifacts/new-perf/report
dotnet tools/YanJi.Probe/bin/Release/net10.0/YanJi.Probe.dll --run D:/Projects/yanji/artifacts/new-crash-run
```

Smoke 是 WPF 与命令集成测试；Performance 在真实进程采样；Probe 仅强制终止自己创建、核对 PID 和实验路径的子进程。它们不主动改变系统电源、时钟或网络。原始数据和构建缓存不进入 Git。
