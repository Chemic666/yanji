# 安装器源码与固定成品追溯

本次采用现有 Beta Release 增补安装器。原公开历史、原三附件和版本标签保持不变；远程操作须单独授权。

| 内容 | 精确来源 |
|---|---|
| 原公开 main、原手工完整 source.zip | `dc1038e62b8046584d7d7dd5f48f17275ce98c44` |
| 原绿色 ZIP SHA-256 | `b0bec5ba1fb85984b44c6ca726bc7492116298b6acc5e4c5fc06de5e2e2f576b` |
| 安装器精确构建输入提交 | `d0f7acf5a194e6e8c9fdb9bfd7f82c721fe62978` |
| 安装器 SHA-256 | `fc33564333de81cdc2471ec62adf74f5da3f69a293a2bf233d1c7c5e55d8cce5` |
| 安装器字节数 | 56,721,342 |
| 原始最终75项验收JSON SHA-256 | `76db02d4ad4cd011450a98da6337805a8987c37f2d0e8ae1964a1d70a8236c0c` |

精确构建输入提交中的9个输入文件，与原嵌入式INSTALLER-PROVENANCE记录的SHA-256逐项一致。安装器在提交前构建，因此内部只记录当时基线及输入哈希；新建提交是对这些精确输入的事后归档，不冒充原二进制内已嵌入的新提交号。

当前源码在该提交之后，只补充发布文案、字节保持规则和离线追溯核查。后续文档的提交SHA由发布准备侧的`INSTALLER-RELEASE-PROVENANCE.json`写明，避免在Git提交内容中自引用本提交SHA。所有成品继续使用相同安装器字节，不重新编译。

`installer/verified-beta1/build-inputs/`保留构建时全部9个输入的原始字节，包含当时文档的本地候选状态；它们是历史证明，不是当前下载说明。当前README与Release正文是安装体验增补后的发布文案；既有安装器内的说明不回写。编译后更新文档与重新打包是两回事，不能把新文档提交称为这份安装器的原始构建提交。

原应用及运行时478个EXE/DLL复用原绿色ZIP，未改业务代码、数据结构或冻结视觉。原BUILD-INFO、原source-inputs、原RELEASE-PROVENANCE和原嵌入式INSTALLER-PROVENANCE均保留，不改写旧证明。

复核边界：安装/升级安装/卸载自身不启动应用，不读取、迁移、写入或删除`%LOCALAPPDATA%\YanJi`学习数据；卸载仅删除登记的程序文件和入口。普通应用由用户主动启动后的正常学习读写不属于安装器操作。75项验收只使用显式隔离数据目录，合成低版本安装器仅验证安装机制，不验证旧应用或Schema迁移。

未签名与SmartScreen警告、干净机离线、实体输入法/其他DPI/真实电源恢复及耐久缺口继续保留。路径防护不承诺抵御同一Windows用户篡改卸载日志、硬链接或检查后的恶意竞态。

## 离线核查

```cmd
python tools\Verify-InstallerRelease.py --setup "<交付目录>\YanJi-v0.4.0-beta.1-Setup.exe" --require-clean --output "<核查JSON>"
```

该工具不安装、不启动应用、不访问正式学习目录、不联网、不执行Git写入。它检查固定成品哈希、原始验收、原始输入字节与Git构建提交，并明确列出当前发布文档与构建时文档的差异。
