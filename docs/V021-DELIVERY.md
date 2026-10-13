# 研迹 V0.2.1 本机交付

程序目录：`D:\Projects\yanji\releases\YanJi-v0.2.1-win-x64\YanJi.exe`。完整解压发布 ZIP 后双击 YanJi.exe，不只复制 EXE；win-x64 自包含运行时，无需安装 .NET SDK。

交付目录：`D:\Projects\yanji\releases\Review\v0.2.1`。

| 文件 | 用途 |
|---|---|
| YanJi-v0.2.1-win-x64.zip | 本机运行，完整依赖、许可证与指南；不含数据库 |
| YanJi-v0.2.1-source.zip | 最终交付提交的 Git source archive |
| YanJi-v0.2.1-evidence.zip | 本轮隔离证据、原生图、日志与合成数据库；含 synthetic 标记，不含真实用户记录 |
| YanJi-v0.2.1-Review.zip | 小型审查包：文档、配对图和 comparison.html，无 EXE/数据库 |
| delivery-manifest.json / sha256.txt | 精确版本、源提交、分支、各包 SHA256 与路径 |
| package-validation.json | 解压后文件 SHA、程序集身份、自包含启动验证 |

程序集 ProductVersion 与 `0.2.1+<source commit>` 一致；具体提交以 delivery-manifest.json 和 artifacts/v021-completion.json 为准。Git 分支 `codex/v0.2.1-skill-workflow`；未推送远端。旧 V0.2.0 包和恢复标签保留。

升级前在旧版「设置与数据」导出并校验**完整备份**，明确退出旧版，再启动新版。默认数据路径仍为 `%LOCALAPPDATA%\YanJi`；本轮验收只使用 artifacts 新建的隔离库。恢复完整备份会整体替换，不合并；校验后自动保存当前库与回退副本。CSV 不可替代完整备份。不要同时用两个版本修改同一数据目录。

本机原生验收为 Windows 11 x64、真实192 DPI（200%）；其他 DPI、物理 IME、Narrator、真实睡眠/休眠/锁屏/系统时钟修改、真实系统高对比切换及干净机断网均未测试。截图采用 HWND PrintWindow，视觉矩阵软件渲染；性能采样采用默认渲染。Impeccable 引擎能加载项目上下文，但不支持识别 WPF XAML；没有 WPF 引擎认证。

具体新执行的次数和失败修复记录见 V021-VALIDATION.md；五 Skill 的当前会话匹配与采用边界见 V021-SKILL-USAGE.md。
