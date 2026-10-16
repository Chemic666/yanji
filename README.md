# 研迹 YanJi

面向考研日常学习的 Windows 11 x64 本地客户端：默认简洁，高级能力按需展开，无账号、服务器或云同步。

当前本地候选 **0.4.0-rc.simple.1**，尚未公开发布。主要页面：今日（计时/补录）、学习进度（学到哪里与下一步）、学习数据（真实投入）；任务、薄弱点、日历、修订和高级统计保留。

## 开始使用

完整解压程序包至新的可写目录后双击YanJi.exe，默认旁置data-rc-simple，不覆盖正式安装。首次可选择应用数学、英语、408参考目录；已有目录不会静默导入。参考内容不是2028官方完整大纲。

- [使用说明](docs/V04-SIMPLE-START.md)
- [界面设计](docs/V04-SIMPLE-DESIGN.md)
- [完整目录、范围和来源](docs/V04-SIMPLE-REFERENCES.md)
- [数据兼容](docs/V04-SIMPLE-COMPATIBILITY.md)

## 构建与验证

C#、WPF、.NET 10、CommunityToolkit.Mvvm、Microsoft.Data.Sqlite。构建需要Windows及.NET 10 SDK，发行包自包含，不要求用户安装SDK。

```powershell
.\tools\Invoke-Build.ps1 build YanJi.slnx -c Release
$env:YANJI_M1_TEST_ROOT = "$PWD\artifacts\local-core-data"
.\tools\Invoke-Build.ps1 test tests\YanJi.Tests\YanJi.Tests.csproj -c Release
```

工具优先复用本机项目SDK，可用YANJI_DOTNET_ROOT指定现有工具链。测试与截图必须指定全新隔离数据目录，不能选择真实学习库。实际程序提交、标签、原始测试及未测事项见发行清单与对应验证报告。

## License 与数据

YanJi **原创代码**及相应原创文档采用[Apache-2.0](LICENSE)，边界见[COPYRIGHT.md](COPYRIGHT.md)。第三方组件、开发Skill与系统资源引用保留各自许可及NOTICE，详见[清单](THIRD-PARTY-NOTICES.md)和resources/legal/third-party-licenses.json。源码保留嵌套MIT与上游NOTICE，Windows发行包包含适用运行许可原文。

参考知识包原创目录元数据CC0-1.0，不包含教材、题库、讲义正文；外部资料不因此获得授权。**用户数据属于用户自身，不会因开源而公开或上传。**

候选未签名；SmartScreen、物理输入法、其他物理DPI、干净机离线、电源恢复与多日运行的未测场景不得宣称通过。升级前须创建并校验完整备份；真实数据切换与公开发布需单独授权，不保证任意历史格式直接升级。旧版Tag、源码及发行证据完整保留。
