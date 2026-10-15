# M2 知识包工作区设计

继续使用 Quiet Focus 的 Canvas/CardBackground/Ink/Muted/Accent/SelectionSurface/Line 资源，不新增配色框架。字体、FocusComboBox/FocusTextBox、Primary/QuietButton、RecordItem 列表选择及键盘焦点沿用正式页面。

入口：计划与进度的知识目录右上角“知识包”。独立原生WPF窗口，1200×800，最小900×600；不会切换应用的计时状态。三页签只承担三类任务：浏览与文件导入、已安装版本及历史、差异与确认。

- 浏览采用240px模板列表+自适应说明区，科目筛选和明确的档案/科目目标。长说明、来源和树在独立纵向滚动区；主操作保持可见。
- 树复用从M1提取的 KnowledgeTreeStyles.xaml，虚拟化/回收与懒展开；预览只展示参考内容，不伪造学习状态。
- 差异采用变更列表+冲突选择两栏；默认保留本地。历史/本地内容必须保留的项目不可选破坏性操作。选择后可重新检查合并图，提交前再次确认目标与影响。
- 确认使用现有ConfirmDialog，新增可选普通主操作样式；原有危险操作保持默认危险样式。
- 状态、失败说明与原始技术异常分离：格式/编码/版本/结构错误为中文提示，不展示堆栈。程序内部不吞掉未预期编程异常。
- ViewModel独立Busy，后台读取、验证、三方规划和数据库提交；不占用主计时器的Busy。应用目录变更时存在活动计时会被数据层拒绝。

Skill实际使用：本会话读取项目 `.agents/skills/yanji-domain-rules/SKILL.md`，采用其原始时长、进度独立、完整备份及隔离数据要求；读取 `docs/skills/dotnet-wpf.instructions.md` 作为Copilot来源参考，而非自动加载声明。实际执行项目UI/UX Pro Max `search.py 'tree import preview virtualization' --stack wpf`，采用返回的TreeView虚拟化和上层路由事件指导。本任务未运行Web设计引擎，不把HTML截图或Web无障碍检查作为WPF验收。

真实截图见审核包comparison.html，只是原生HWND图片索引；不是HTML产品原型。已检查双主题、1200×800/900×600、中文长说明、树/列表选择、确认与导入错误。真实DPI192（200%）；其他DPI/物理输入法未验证。
