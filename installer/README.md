# Windows 安装工程

仅对 clean main `dc1038e62b8046584d7d7dd5f48f17275ce98c44` 后续安装体验变更。业务源码、冻结视觉、原历史和标签不改。构建复用公开 Beta ZIP 的原始二进制，绝不从原始私有仓库打包或推送。

## 本地构建（Windows CMD）

需要 Python 3 标准库与获准使用的官方 Inno Setup 6.7.3。可使用官方便携模式，不需要全局安装或修改 PATH。本脚本不下载工具、还原依赖、运行应用或执行 Git/GitHub 写操作。

```cmd
python -m unittest discover -s tools -p "test_*.py"
python tools\Build-Installer.py --archive "<已发布的YanJi-v0.4.0-beta.1-windows-x64.zip>" --iscc "<Inno工具目录>\ISCC.exe" --output "<新的构建目录>"
```

脚本固定校验原ZIP SHA-256 `b0bec5ba1fb85984b44c6ca726bc7492116298b6acc5e4c5fc06de5e2e2f576b`，检查CRC、逐文件清单闭包、Windows异常路径、重复/大小写冲突、用户数据/调试文件及完整许可；使用显式文件表，不递归收集工作目录。所有EXE/DLL与原ZIP逐项相等。安装说明更新，原BUILD-INFO、source-inputs、RELEASE-PROVENANCE等构建证明保留。`INSTALLER-PROVENANCE.json` 单独记录新增安装工程/文档输入，安装版清单反映当前文档。旧ZIP直接复制，字节不改。

输出目录必须不存在；失败目录保留，不覆写历史证据。`build-result.json`仅证明构建成功，不能替代实际安装验证。安装程序未签名；官方编译器下载文件的有效签名不会传递给生成的YanJi安装器。

## 隔离验证

`Test-Installer.ps1`实际写入当前用户Programs目录、快捷方式及HKCU卸载登记，但所有**应用学习数据**必须使用仓库artifacts下全新独立目录、`--test-mode --data-dir`。它不访问正式学习目录，也不以改变环境变量伪造Windows标准数据路径。若已有安装、快捷方式、登记或YanJi进程，立即停止，不覆盖、不关闭用户程序。

```cmd
powershell -NoProfile -File tools\Test-Installer.ps1 -Setup "<构建目录>\YanJi-v0.4.0-beta.1-Setup.exe" -Payload "<构建目录>\payload" -TestRoot "<本工作副本>\artifacts\installer-test-new"
```

请只在授权的隔离验收环境执行。测试覆盖默认非提权安装、逐文件安装哈希、显式隔离启动/自包含运行时、快捷方式、卸载登记、异常路径/联接拒绝、运行中程序保护、同版重装、卸载数据保留及卸载后重装。测试留下隔离证据，不清理学习档案。首次使用弹窗只在核对测试进程与窗口归属后正常取消，不强杀进程。

可通过`-PriorSetup`提供同AppId、同payload、仅安装元数据为`0.4.0-beta.0`的**合成测试安装器**，验证就地升级机制。它不是历史发行版，不证明不同应用版本/Schema兼容，不作为附件发布。生产安装器必须始终是正常构建输出的beta.1。

路径策略是固定当前用户目录。`/DIR`重定向及LOADINF产生的非默认路径被拒绝；现存路径祖先与程序子树的reparse points被拒绝。安装前/卸载前检查，不包含递归删除数据的规则；运行中YanJi必须由用户自行结束并退出。它不承诺抵御同一Windows用户在检查后并发恶意篡改路径/卸载日志或硬链接的攻击；这不是权限隔离边界。

快捷方式目录从根到叶检查，发现联接后不继续探测子路径。桌面任务的历史归属保存在`HKCU\Software\YanJi\Installer`，避免被Inno重建卸载登记时清空；重装取消桌面任务仍保留归属，卸载仅在该登记表明曾创建桌面入口时检查桌面路径。未选择桌面的用户不因无关桌面路径而无法卸载。卸载删除自身标记，并仅在空目录项时清理自己的登记键，不清理用户的其他设置。

## 官方来源与许可

- [官方工具下载页](https://jrsoftware.org/isdl.php)；本轮使用6.7.3，官方安装文件签名Pyrsys B.V.已核对。工具仅放任务工具目录。
- [官方便携模式说明](https://jrsoftware.org/ishelp/topic_technotes.htm)。
- [非提权安装](https://jrsoftware.org/ishelp/topic_setup_privilegesrequired.htm)、[原生x64限制](https://jrsoftware.org/ishelp/topic_setup_architecturesallowed.htm)、[安装与卸载事件](https://jrsoftware.org/ishelp/topic_scriptevents.htm)。
- `ChineseSimplified.isl`原文来自[官方源码is-6_7_3标签的用户贡献翻译](https://github.com/jrsoftware/issrc/blob/is-6_7_3/Files/Languages/Unofficial/ChineseSimplified.isl)，SHA-256 `7d544b9bb1d142cfa11f2e5d3cc8abe2e55f8e066c5124e3772675aa236e1278`；保留原作者署名，未改写第三方归属。安装器版权与网址保持，原License.txt随程序放在`licenses/InnoSetup-LICENSE.txt`。

源码/本地输出准备完成不等于远程发布授权。本轮禁止push、tag、Release与附件写入。后续发布仅在获得单独授权后，复核原附件、main与标签，新增Setup和独立安装器校验文件；保留原ZIP/源码ZIP/SHA256SUMS，参照本次输出的新增附件方案。
