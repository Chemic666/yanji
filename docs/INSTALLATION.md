# 安装研迹 YanJi（Windows 11 x64）

**普通用户：下载 Setup.exe → 双击安装 → 从开始菜单启动研迹。** 不需要手动解压、计算 SHA-256 或安装 .NET SDK/运行时。

v0.4.0-beta.1 已公开为 Pre-release，原绿色 ZIP 保留。安装版 `YanJi-v0.4.0-beta.1-Setup.exe` 与绿色 ZIP 使用[同一 Release](https://github.com/Chemic666/yanji/releases/tag/v0.4.0-beta.1) 的下载入口。请只从项目 Release 下载。

## 安装与启动

1. 在项目 Release 的 Assets 中下载 `YanJi-v0.4.0-beta.1-Setup.exe`。
2. 双击安装；阅读说明和许可。仅安装给当前 Windows 用户，通常不需要管理员权限。可选创建桌面快捷方式。
3. 安装完成后，从开始菜单打开“研迹 YanJi”，建立学习档案。安装器本身不自动启动应用。

程序固定安装在 `%LOCALAPPDATA%\Programs\YanJi`，学习数据库和备份仍在 `%LOCALAPPDATA%\YanJi`。为避免误选数据目录，此版本不提供自定义程序路径，也拒绝 `/DIR` 指定其他位置、符号链接或目录联接。安装器不会读取、复制、迁移或修改学习数据库。

**未签名说明：** YanJi 应用和安装器尚未进行代码签名，Windows SmartScreen 或其他安全软件可能提示“无法识别的应用”或未知发布者。本轮不宣称已获得 SmartScreen 信誉。先确认来自维护者的 GitHub Release；如仍不能确认来源，取消安装。不要关闭系统安全功能。SHA-256 是可选的文件完整性核对，不是安全认证或代码签名。

需要核对时，可自行使用：

```cmd
certutil -hashfile YanJi-v0.4.0-beta.1-Setup.exe SHA256
```

将结果与安装器附件的 `SHA256SUMS-INSTALLER.txt` 比较。普通用户无需此步骤。

## 重装、程序升级与卸载

重装前先在应用中结束计时并退出。运行同一 Setup 会在原位置更新其安装的程序文件并保留学习数据；检测到研迹仍在运行时，安装器会停止，不强制结束进程。这个安装器只交付 v0.4.0-beta.1；未来不同版本的升级需要该版本自己的验收，不能据本轮重装测试声称所有后续升级均兼容。

从 Windows 设置 → 应用 → 安装的应用 → 研迹 YanJi → 卸载，或从开始菜单运行卸载程序。卸载移除安装器登记的程序文件、快捷方式和卸载入口，**绝不自动删除 `%LOCALAPPDATA%\YanJi` 的数据库或备份**。程序目录中自行添加的未知文件也不会被递归清理。卸载不是清空学习记录；删除个人数据需由用户另行决定，本安装器不提供删除数据选项。

程序更新与数据库版本迁移是两回事。**旧版本数据库不自动迁移**，旧候选 data 目录不自动导入。已有真实学习库用户请先阅读 [升级与回退](UPGRADE-ROLLBACK.md)，用对应程序备份并校验；本次安装器测试没有访问任何真实学习数据。不要把旧库复制到新目录，也不要把安装程序指向学习数据目录。

## 绿色 ZIP 与隔离试用

保留 `YanJi-v0.4.0-beta.1-windows-x64.zip`。完整解压到全新可写目录后双击 `YanJi.exe`；不要只拷贝 EXE，也不要在 ZIP 内运行。ZIP 与 Setup 使用相同的已发布应用/运行时二进制；安装器另含更新的说明和自己的构建证明。ZIP 的原始字节、哈希与已有附件保持不变。

**只想试用而不打开正式学习数据时，不要点击普通快捷方式。** 显式运行：

```cmd
"%LOCALAPPDATA%\Programs\YanJi\YanJi.exe" --test-mode --data-dir "%TEMP%\YanJi-Isolated-Trial"
```

选择未使用过的独立本地绝对路径，不使用联接/符号链接。窗口会显示“隔离测试”。安装到另一程序目录或仅新建应用内档案，都不能替代显式数据隔离。

## Beta 与许可

这是 Beta，不代表 Stable。物理中文输入法、其他 DPI、真实睡眠/休眠、干净 Windows 离线和长期运行仍有未测限制；详见 [发行说明](RELEASE-V040.md)。安装验证不代替这些应用兼容性验收。

原创代码 Apache-2.0；第三方组件、参考目录和 AI 素材的有限授权继续遵循随包 LICENSE、NOTICE、THIRD-PARTY-NOTICES.md、ASSET-RIGHTS.md 等声明。安装器由 [Inno Setup](https://jrsoftware.org/) 生成，保留其版权信息，原文随安装文件放在 `licenses/InnoSetup-LICENSE.txt`。未新增联网安装步骤或付费服务。
