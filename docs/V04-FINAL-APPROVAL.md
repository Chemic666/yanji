# V0.4 RC1 发布前最后验收

候选包与审核材料状态：**READY FOR FINAL APPROVAL**。尚未满足“立即公开发行”的全部条件；根许可证、最低人工验收与最终发布授权仍待所有者决定。本轮不升级真实学习数据、不覆盖正式安装、不公开Release、不进入V0.5。

## 本轮结论

- 推荐Apache-2.0；比较依据、全部21个依赖、字体/图标/设计资源/七知识包审查见[V04-FINAL-LICENSE-REVIEW.md](V04-FINAL-LICENSE-REVIEW.md)。没有添加项目根LICENSE或代填版权人。
- 原RC1四ZIP外层哈希、CRC、每条目SHA、程序集来源和Git Tag再次一致。程序e0e710c；文档HEAD ce217d3；Tag v0.4.0-rc.1指向程序。未重建产品、未修改原ZIP。
- 431个跟踪文件、运行包未发现字体/图标/图片二进制资源。七运行许可文本保持一致；原有197核心/37真实WPF/9连续计时采样证据复用，未重复跑全量。
- 开发Skill中两个嵌套MIT声明缺口已准备完整补充，见[第三方补充](legal/THIRD-PARTY-SUPPLEMENT.md)。**历史RC1 source ZIP单独公开分发：BLOCKED**；正式源码发行须并入补充和用户决定的根许可再归档。Windows运行包不含这些Skill。

## 本轮Windows补充观察

已在Windows真实进程、既有RC1已发布DLL、独立SQLite目录上执行约900.3秒后台观察；没有显示窗口、发送键鼠或抢焦点。它使用原MainViewModel、WindowsClock和WPF Dispatcher/绑定，是**后台组件持续运行**，不能叫“完整前台应用长稳验收”。

| 项目 | 本轮证据 | 状态 |
|---|---|---|
| 持续自由计时与WPF绑定 | 891次调度、891次绑定核对；约3023ms暂停排除；保存897234ms，与独立Stopwatch差6.4ms | READY FOR FINAL APPROVAL |
| SQLite保存 | 实际文件完整性ok，独立只读SUM(parts.duration)（毫秒）与保存结果相同，仅1条合成记录 | READY FOR FINAL APPROVAL |
| 内存观察 | 约60秒私有内存159.4MiB，结束163.8MiB，采样工作集峰值246.7MiB；30个样本 | READY FOR FINAL APPROVAL（有限观察，不证明无泄漏） |
| 主/迷你、托盘/恢复、包/目录/计时/统计 | 复用同一程序提交的原RC1真实WPF证据，本轮无相关代码变化 | READY FOR FINAL APPROVAL（既有证据） |
| 200%DPI、900×600/1200×800、浅深主题 | 复用原RC1真实窗口与7图；本轮未冒称新增截图 | READY FOR FINAL APPROVAL（既有证据） |
| 微软拼音组合输入/候选窗、物理焦点快捷键 | 自动物理键鼠会干扰当前桌面；留最小人工清单 | NOT TESTED |
| 其他实际DPI、跨屏输入位置 | 未修改系统DPI，没有自动改设置 | NOT TESTED |
| 干净Win11与实际离线启动 | 没有准备好的授权测试机/VM；未安装或启用Sandbox，未断开主机网络；静态没有网络客户端不等于离线实测 | NOT TESTED |
| 多小时/多日完整应用稳定性、真实电源恢复 | 15分钟后台观察与状态机测试不能替代 | NOT TESTED |

原始补充证据在`artifacts/v04-final-approval`。未访问真实用户数据库。后台观察不包含知识树大负载或可见窗口渲染，不把其内存数据冒充完整应用占用。

## 剩余事项与严重程度

| 项目 | 级别与影响 | 当前状态/下一步 |
|---|---|---|
| 根许可证与权利人决定 | 公开开源分发决策门槛 | BLOCKED：用户选择后才可加入最终LICENSE；推荐Apache-2.0 |
| 原源码ZIP的嵌套声明 | P1，仅源包公开分发 | BLOCKED：修正材料已齐，正式源包须并入；原证据保持不变 |
| 物理IME、键盘焦点、干净机/离线 | P2/验收缺口，未知兼容问题可能影响实际用户 | NOT TESTED：按15～30分钟清单取得实际记录后决定是否阻断 |
| 其他DPI、长期/电源、SmartScreen | P2，覆盖/发行体验限制 | NOT TESTED；不得声称全硬件长期稳定认证 |
| Windows签名 | P2，可能触发下载/启动提示 | 可执行文件实际NotSigned；SmartScreen具体表现NOT TESTED，不宣称已被阻止或已放行 |

当前没有发现新的P0/P1**运行功能**缺陷。正式版不能仅凭自动化或本报告宣布通过：先完成必要人工验收，选择许可证并确认版权授权，决定剩余未测风险是否可接受，再单独授权正式版本号/Tag、源码归档与发布。当前程序确实还是0.4.0-rc.1，不冒称0.4.0正式版；真实数据升级仍需独立授权。

人工操作见[V04-FINAL-MANUAL-ACCEPTANCE.md](V04-FINAL-MANUAL-ACCEPTANCE.md)：通常15～25分钟，有现成干净机才额外执行离线检查。本机入口`artifacts/v04-final-approval/manual/Start-RC1-Manual.cmd`只使用隔离数据。

采用verification-before-completion与项目领域规则读取指导，按范围复核而未重跑未受影响的测试。Computer Use仅用于审查输入权限边界，本轮未执行物理键鼠验收。新文档/声明未自动提交，HEAD/Tag不变，产品源码与迁移未改。
