# 设计 Skills：来源、安装与实际应用

本轮没有调用 Product Design 插件。六个用户指定的官方仓库均检查，固定提交而不是追踪可变main。GitHub REST限流后使用官方HTTPS Git和codeload归档；不申请账号、不执行npx/npm一键安装。

|来源|本轮实际版本 / 提交|授权 / 安装状态|
|---|---|---|
|[UI/UX Pro Max](https://github.com/nextlevelbuilder/ui-ux-pro-max-skill)|manifest2.15.0 / 7246805992a42b7bba37d4cac83b01416f0c6c44|MIT；已安装ui-ux-pro-max，WPF搜索实际运行|
|[Impeccable](https://github.com/pbakaus/impeccable)|4.5.2 / 245ad82def45831dc56a21b683c7a1ff530b06cc|Apache2.0；已安装impeccable文本/支持文件；引擎0.1.14未下载/未运行|
|[Taste-Skill](https://github.com/Leonxlnx/taste-skill)|manifest1.0.0 / d237dd399d57524945c0c81327da5da19f7a468d|MIT；已安装redesign-skill与soft-skill，名称分别redesign-existing-projects / high-end-visual-design|
|[Make Interfaces Feel Better](https://github.com/jakubkrehel/make-interfaces-feel-better)|无独立semver / 35545ea1512ad59fa463e6b1f95ca9c052981fe6|MIT；已安装make-interfaces-feel-better|
|[Frontend Design](https://github.com/anthropics/skills/tree/main/skills/frontend-design)|无独立semver / dbd4588f9e1033efb41dad4bef2f7947c8993d44|该目录LICENSE.txt为Apache2.0；辅助读取，未安装，避免与Impeccable重叠|
|[Web Design Guidelines](https://github.com/vercel-labs/agent-skills)|技能metadata1.0.0 / 063bee94c3f4df8453406c830b0a7df0f2860278|所选目录未找到LICENSE文件；辅助读取，未安装/不分发原文|

## 实际路径与加载证据

五个Skill目录位于 `D:\Projects\yanji\.agents\skills\<上述目录>`。使用已审查OpenAI skill-installer，显式 `--dest`、完整 `--ref` 与download模式，未修改全局Skill目录、PATH或config。

本机Codex CLI0.160.0 app-server实际 `skills/list` + `forceReload` 对D盘项目扫描：五个Skill均scope=repo、enabled=true，项目解析错误0。证据 `artifacts/v02-skills/discovery.json`。首次沙箱启动因Codex全局状态库权限失败；使用[官方sqlite_home参数](https://learn.chatgpt.com/docs/config-file/config-reference)把此探针状态库放到owned项目目录后成功，没有模型调用或新对话。

**当前聊天自动重载未证明**：本聊天cwd为C盘任务目录，不能把D项目扫描成功说成当前目录已自动注入。当前实际使用方式是显式读取D项目Skill文件，并运行已审查搜索工具/采用指南。项目根的新回合或打开D项目可发现；没有要求用户为继续工作重启。

来源/许可/路径/SKILL.md SHA256详见 `.agents/design-skill-sources.json`；上游授权文本随安装保留为UPSTREAM-LICENSE，Impeccable另有UPSTREAM-NOTICE.md。项目.gitattributes保留第三方原始字节；pycache不提交。

## 安全与适配审查

- 官方安装器只下载、验证路径和复制选定目录；未执行上游安装器。UIUX运行模块search/core/design_system/reasoning_contract为标准库，本轮search模式无网络/进程执行；design-system的持久化可写文件，未用force，也没有把营销模板当项目规范。
- Impeccable Windows launcher会下载并执行额外引擎且默认缓存于用户目录；已完整读取。以禁止下载的probe运行context，返回127。按其fallback读取已有设计上下文、Operate、layout/typeset/craft-floor；浏览器live/detect与引擎功能不计PASS。其native指南主要iOS/Android，不能当WPF专用验收。
- Taste和Make的选定Skill为文本指南；未安装其他品牌/图片/网页生成Skills。Frontend与Vercel只做末尾辅助审查，不并列决定视觉方向。

## 实际使用到实现的映射

|Skill|本轮采用的位置|明确拒绝或调整|
|---|---|---|
|UIUX Pro Max|Styles/ThemeService语义资源，XAML声明布局，Tab/AutomationProperties，PerMonitorV2，虚拟化与UI线程边界|不使用默认HTML+Tailwind；不盲增DI容器、消息框架、第三方UI包；可变主题用DynamicResource而不是一律StaticResource|
|Taste redesign / high-end|今日的主计时/账本不对称、统计主图与次信息、减少等权卡/重复标题、固定中性颜色族|拒绝海量留白、双套边框、玻璃、随机布局、电影滚动、假装高级字体已装；暖纸绿来自用户确认，不随机再设计|
|Impeccable|Operate优先任务效率；字体角色、密度和导航状态；空/长文本/深浅/小窗集中审查，限定两轮视觉收敛|craft-floor通用网页的强制外部display字体/边框禁令服从Operate与用户桌面约束；不运行Web检测器、React浏览器样板|
|Make Interfaces Feel Better|控件40/44DIP点击面积、等宽数字、图标同一描边与光学位置、hover/focus/disabled稳定边界|不直接复制CSS半径公式到相距24DIP以上独立表面；不对高频按钮/滚动加0.96缩放、blur、stagger|
|Frontend Design|中文操作文案、用户语言、结构服务信息、少装饰、方向完成后实际截图自检|不加网页hero、品牌字体下载或营销夸张声明|
|Web Guidelines|显式标签、键盘与可见focus、破坏操作确认、长内容、虚拟列表及筛选下钻|ARIA/DOM/CSS/URL/hydration由WPF语义与绑定替代；不把Web规则当Windows验证|

实际UIUX检索：`ResourceDictionary DynamicResource theme`、`keyboard focus modal`、`virtualization DPI layout`，均显式`--stack wpf`，得到有效WPF条目；原始JSON在artifacts/v02-skills。产品级搜索识别Productivity Tool但输出Web营销结构和Plus Jakarta字体，明确拒绝；参考间距4/8/16/24/32与快捷键原则，项目DESIGN才是最终规范。

最终应用/视觉证据、修复位置与未验证项继续记录在V02-LEDGER与最终验收报告，不用安装数量代替质量。
