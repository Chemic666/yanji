# YanJi 应用图标接入

结果：图标资源与WPF/EXE/发布接入完成；Windows系统栏的实际栏位外观未单独截图，明确保留验收限制。未访问真实学习数据库，未修改Schema、计时、统计、知识包或学习数据语义。

## 文件与接入

- 原设计 `design/branding/yanji-icon-v1.png`，1254×1254，原文件SHA256 `07D610BAD2E7AA46E8DD6BA98B4CCB901E16A010F72D18886AEC5DEAB26A36DA`，未修改内容。
- `src/YanJi/Assets/Icons/YanJi.ico`：16/24/32/48/64/128/256七帧；`YanJi.png`：256×256。高质量缩小，不裁切、不重新设计。尺寸预览在artifacts/branding-icon/icon-sizes.png。
- 实际WPF工程为 `src/YanJi.App/YanJi.App.csproj`。ApplicationIcon指向指定目录的ICO；ICO/PNG通过Resource+Link嵌入为Assets/Icons，无需旁加载图标文件。
- `MainWindow.xaml`：Icon使用pack URI；`MainWindow.xaml.cs`：NotifyIcon从同一内嵌ICO按本机小图标尺寸加载，克隆后释放输入流，退出时释放持有图标。不再使用SystemIcons.Application。
- `tools/Generate-AppIcon.py`：可重复生成资源，需要已安装Pillow（本机12.3.0，仅开发工具，不增加客户端依赖）。
- `tools/Verify-AppIcon.py`：仅标准库、只读PE资源、禁止执行被检查文件；检查每个图标帧的原始数据SHA。当前 `tools/Package-Polish2.py` 的windows步骤已加入此门禁，未改历史发布脚本。
- `IconValidation.cs` + `SmokeRunner.cs`：复用已有--startup-probe，只在隔离探针采集实际窗口小/大图标、Shell提取图标、NotifyIcon和内嵌资源哈希，正常运行不进行诊断。
- `.gitignore`：仅增加设计源和指定PNG资源白名单，避免clone后丢图；其他截图仍忽略。

不存在现成NSIS/安装器或桌面/开始菜单快捷方式生成逻辑，本轮没有新增安装器或修改系统快捷方式。后续快捷方式可直接引用带内嵌图标的YanJi.exe。ApplicationIcon在常规dotnet publish中生效，资源并不依赖当前工作目录。

## 最小验证

Windows11、本机200%DPI、.NET10，使用现有SDK和NuGet缓存。一次自包含publish成功，包含编译；没有重复运行完整核心或业务测试。

| 项目 | 证据与结果 |
|---|---|
| ICO七帧 | 全部可解码；EXE RT_GROUP_ICON/RT_ICON七帧与ICO逐帧SHA相等，PASS |
| 本地程序 | 现有startup-probe仅2项检查PASS，退出码0；内嵌ICO SHA与源文件相等 |
| 主窗口 | 实际WPF标题栏截图显示新图标，PASS；32/64px原生WM_GETICON像素与ICO完全一致 |
| 任务栏 | ShowInTaskbar=true，任务栏使用的窗口小/大图标句柄像素相等；系统任务栏栏位最终外观NOT TESTED |
| EXE/资源管理器 | Shell提取64px像素相等，真实Explorer搜索YanJi.exe截图显示新图标，PASS |
| 托盘 | 实际NotifyIcon使用32px新图标，像素完全一致，PASS（资源/对象层）；系统托盘栏位外观NOT TESTED |
| ZIP解压独立启动 | CRC/517文件SHA通过并实际解压到唯一staging；再启动新隔离库，2项检查PASS、退出码0，各图标像素再次相等 |
| 原有材料 | 原RC-Polish.2包和恢复标签不改动；资源许可文件保留门禁PASS |

自动化工具未提供可操作的系统任务栏/托盘窗口，未伪称栏位截图验证通过；可在隔离候选运行时人工目视确认。当前窗口及Explorer已展示新图标，没有缓存异常证据，**不需要主动清理Windows图标缓存**。本轮未清缓存、未重启Explorer、未修改系统设置。

## 候选与追溯

Windows包：`releases/YanJi-icon-de82781e-02a379f0/YanJi-v0.4.0-rc.polish.2-icon-windows-x64.zip`。完整解压新目录再启动YanJi.exe；本候选沿用data-rc-polish2隔离默认目录，勿覆盖旧程序或选择真实库。

HEAD仍为 `de82781e8386a6a3596de21c55c7add5662a8325`，分支codex/v04-rc-polish2。本轮修改留在工作区，未暂存/提交、未新建Tag、未推送或公开发布。版本号保持0.4.0-rc.polish.2，不能仅凭HEAD代表新图标程序；包内BUILD-INFO明确WorkingTreeChanges=true，并记录完整编译输入SHA清单指纹与二进制SHA。source-state.json保留所有编译输入的逐文件哈希。

编译输入指纹 `02A379F09F4B24EE74ABDEBEAC9728A24CCBB0CF14056D43F6A3FD2B146E312E`；YanJi.dll SHA `391080F7C1866ED9754026FC04F28AC1B8376F81F4AB129AEF918E2D82B45C5D`。包SHA与证据在同目录SHA256SUMS及artifacts/branding-icon。只复用一个解压运行目录，没有清除旧版发行物或真实数据库。

Skill：computer-use用于真实WPF/Explorer操作及截图；verification-before-completion用于核对资源、封闭ZIP与真实结果。采用用户提供源图进行格式导出，没有调用图像模型重绘。
