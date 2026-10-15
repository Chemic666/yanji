# 研迹 YanJi V0.4.0 RC-Final

Windows 11 x64、离线优先的考研学习管理客户端。2028指入学年份，考试日期可空。当前版本`0.4.0-rc.final.1`是最终人工验收候选版，不是已公开发行的Stable。

## 使用

完整解压Windows x64 ZIP到一个新的可写目录，双击`YanJi.exe`，无需.NET SDK或账号。候选数据默认保存在程序旁`data-rc-v04`，不会自动接管旧版目录。切勿覆盖正式安装或把删除程序目录当作安全卸载。

首次设置考试档案和科目，安装参考知识包，然后从知识节点或任务开始学习。自由计时、倒计时、番茄钟均实时显示秒；暂停与休息不计入有效学习。支持迷你窗、托盘、纯时长补录、修订/软删/恢复、薄弱点、原始记录下钻和完整备份。

- [使用说明](docs/V04-RCFINAL-USER-GUIDE.md)
- [升级、备份和旧格式回退](docs/V04-RCFINAL-UPGRADE.md)
- [人工验收清单](docs/V04-RCFINAL-MANUAL.md)
- [发行说明草稿](docs/V04-RCFINAL-RELEASE-NOTES.md)
- [许可范围](COPYRIGHT.md)及[第三方清单](THIRD-PARTY-NOTICES.md)

旧版升级只对声明支持并通过校验的Schema1～4提供路径；先创建并校验完整备份，复制升级到全新目录。回退使用升级前原格式备份，不能让旧程序直接误写Schema2/3/4。本轮不操作真实用户数据库。

## License

YanJi的**原创代码采用Apache-2.0**，官方完整文本见仓库根`LICENSE`；Windows包同样携带`LICENSE`、`NOTICE`、`COPYRIGHT.md`及`licenses/`。第三方组件与外部Skill保留各自许可证；知识包内容许可与代码许可独立，当前内置自编目录元数据为CC0-1.0。用户数据属于用户/相应权利人，不因软件开源而公开、上传或改变许可。详见[COPYRIGHT.md](COPYRIGHT.md)。

## 构建与核查

使用`global.json`允许的.NET 10 SDK（当前基线10.0.401），固定版本见`Directory.Packages.props`与锁文件。Windows CMD：

```cmd
dotnet restore YanJi.slnx --locked-mode --configfile NuGet.Config
dotnet build src\YanJi.App\YanJi.App.csproj -c Release --no-restore
dotnet publish src\YanJi.App\YanJi.App.csproj -c Release -r win-x64 --self-contained true --no-restore -o artifacts\candidate
python tools\Check-ReleaseLicenses.py . --mode source
python tools\Check-ReleaseLicenses.py artifacts\candidate --mode binary
```

本机既有便携SDK也可通过`tools/Invoke-Build.ps1 -Arguments @(...)`调用，不修改系统PATH。发布前检查ZIP清单、SHA-256和许可证；只有带对应提交/程序集来源的归档可用于交付。不要把`bin/obj`、真实数据库、备份或临时证据加入Git。

## 验证与限制

程序尚未代码签名，SmartScreen实际表现未验证。物理微软拼音、其他物理DPI、干净机离线、真实电源恢复和多日稳定性未完成的部分明确为NOT TESTED；15分钟后台计时观察不等同完整前台应用长期认证。408为参考目录，非官方年度完整大纲。

没有账号、服务器、云同步、社交、AI评价或V0.5新功能。历史设计、恢复标签与原始失败证据保留；旧文档仅代表当时版本。正式发布、真实用户升级、远端推送及仓库可见性变更均需单独授权。
