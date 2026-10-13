# V0.2 进度与裁决

- 起点 fd12f36；工作区干净；旧v0.1.1及两个视觉恢复标签存在。
- 用户确认A并授权吸收B及更好的布局，不再进行方向投票或阶段审批。
- 已创建codex/v0.2-quiet-focus；先读取源码、原验收与实际截图证据，再进入实现。
- 原业务本轮基线59 PASS / 0 FAIL / 0 SKIP，TRX已保存。
- 六个设计源已固定提交；UI/UX Pro Max2.15.0有56条WPF指南，实际检索成功。
- Ruling：官方安装器使用 --dest D:/Projects/yanji/.agents/skills，复制经审查文本与支持文件；不执行上游npx/npm全局安装、hooks或远程一键脚本。
- Impeccable4.5.2需要额外引擎0.1.14，安全探测context返回127；禁止隐式下载，全局配置不改，采用已读取Operate/审查/craft-floor文本。引擎功能不计PASS。
- Codex发现初次探测因全局SQLite运行时目录权限失败；改用官方sqlite_home命令行覆盖到owned目录，实际发现5个repo/enabled入口，解析错误0，证据v02-skills/discovery.json。没有修改CODEX_HOME、全局config或账号。
- Ruling：采用有界两轮视觉检查与缺陷驱动回归；不为无关样式反复截图。用户自主授权优先于Skills的逐步确认建议。
- Ruling：原项目与发布路径用户已指定，保留同目录独立分支及恢复标签；不引入第二份交付路径。

## 实现与本轮可靠证据

- 自然紧凑时长先RED（缺少Compact的编译错误），后GREEN；最终70 PASS / 0 FAIL / 0 SKIP，v02-final-tests/v02-final.trx。原Format、毫秒聚合和schema不变。
- 本轮旧V0.1.1真实窗口基线63 PASS，v011-desktop-f7322b8bba1343d494bb2e227e4ec0f1；从其已关闭测试库复制升级夹具，不读取真实用户库。
- 正式六模块及学习记录明细完成统一布局、主题、矢量图标、展示与编辑资源；复用原handler与事务，不使用DesignLab替代正式程序。
- 七日趋势原来可能继承统计科目而造成值/下钻范围不一致，改为显式传入subjectScope。抽屉隔离底层操作、循环Tab、Escape返回；原生窗口运行断言。
- 首轮v02-desktop-aefb...：78 PASS + 1校验失败（并记录Unhandled）；异步Automation Invoke尚未完成便检查，等待后修复。失败证据保留，不算通过。
- 集中目视修复：说明文字真实换行、记录选择统一sage、过滤器显式标签、紧凑校验/确认对话框。
- 选择器仍异常的运行诊断：模板内Toggle从系统Fluent继承Left对齐，实际只有24×6 DIP；显式Style=null/Stretch后实际整框命中及菜单打开验证PASS。这是操作可用性缺陷修复，不继续无关外观迭代。
- 原生DatePicker在ApplyTemplate后用内部Calendar替换Popup.Child；验证改查真实内部Calendar，保留日期解析/交互，而不是对已被替换的模板对象作断言。
- 最终候选窗口v02-desktop-b2b30b52646d41cea7c425706954dfab：84 PASS / 0 FAIL，包含原63及新增21；实际DPI192/200%。Native PrintWindow与RTB文件分开，软件渲染只在--v02-visual启用。
- 升级v02-upgrade-b21954762fd244e2b2f16e8924a74ad7：实际旧二进制所写444条记录；新版WPF打开后逐表比较记录、片段、audit、settings等完全不变；新旧完整备份分别校验、实际恢复；Integrity=ok，migration=none。
- 升级测试首轮错误地把较早备份与较晚的活库状态比较（末尾主题审计已有变化）；修正为各备份自己的原始snapshot全表比较。未修改业务恢复机制。
- 一次发布遇到仍运行的owned性能进程锁定DLL，发布失败未算成功；改用独立candidate目录验证，不终止测试或修改其他进程。待测试自然退出再发布最终目录。
- 400行真实列表仅实现3个容器，完整刷新/布局样本238.3ms；截图过程资源数字不作为空闲性能。
- 最终独立审查、最终发布包哈希/实际启动、性能证据和交付报告待补齐。

## 独立审查后

- 独立review为Critical 0 / Important 2；两项均修复：月历有效选中/核对边框，选中高对比文字与背景分离。具体裁决及未判事项见V02-REVIEW.md；原生状态与模拟brush验证补齐。
- 整理XAML格式，降低模板优先级错误的审查成本；没有更换框架或数据库。
- 新增真实指针悬停验证曾发现默认Tooltip仅赋值却未出现；统一由StudyChart管理原生WPF提示。鼠标移出/数据筛选更新关闭旧提示，键盘与悬停共享精确描述。
- 正式发布路径最终90 PASS / 0 FAIL，v02-desktop-bc7218132f9c452ca6f4d5a5ec47763a，实际Tooltip HWND已捕获；包括原63、新展示/焦点/原生菜单/真实悬停24、高对比/月历3。
- 最终核心70 PASS，旧库兼容复核v02-upgrade-5a3cf33bb47145d1a505b891f307e651 PASS；真实用户库仍未读取。
- 所有owned测试进程均等待自然结束；最终默认渲染资源采样正在单独执行，发布包ZIP完成后由manifest与package-validation核实。
