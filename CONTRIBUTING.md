# 参与研迹

先描述真实场景、复现步骤和预期行为；提交最小范围修复。不要把真实学习库、备份、凭据、个人路径或大型发行产物放进Issue或提交。

## 构建

使用Windows11 x64和微软官方.NET10 SDK，建议10.0.402或更新安全补丁；global.json允许同特征带补丁向前滚动。应用自包含运行时固定10.0.13，普通用户不需要SDK。

```cmd
dotnet restore src\YanJi.App\YanJi.App.csproj -r win-x64 --locked-mode
dotnet build src\YanJi.App\YanJi.App.csproj -c Release --no-restore
dotnet publish src\YanJi.App\YanJi.App.csproj -c Release -r win-x64 --self-contained true --no-restore -o artifacts\local-publish
```

从NuGet官方源恢复，遵循锁文件，不盲目升级。正常构建不需要重新生成图标或安装字体；Pillow只用于修改已批准图标时的导出。历史Invoke-Build.ps1兼容旧私有工具链，公开构建可直接用标准dotnet，不需要开发者个人路径。

## 定向验证

只运行受影响测试。测试使用`artifacts`内新隔离目录，绝不选择真实LocalAppData/YanJi或个人备份。先读测试的隔离要求，再执行。

```cmd
set YANJI_M1_TEST_ROOT=%CD%\artifacts\contributor-isolated-tests
dotnet test tests\YanJi.Tests\YanJi.Tests.csproj -c Release --filter "FullyQualifiedName~Timer"
```

不要改系统时钟、电源、DPI，或用模拟结果宣称实机通过。Quiet Focus2.0视觉已冻结，未经授权不改布局、字体、背景和按钮。保留毫秒事实、稳定节点ID、软删除修订及备份恢复语义；不将时长自动解释为掌握。

提交前检查暂存区，不提交bin/obj、数据库、诊断输出和发行ZIP。第三方内容附来源、许可与原声明。安全问题先阅读SECURITY.md。
