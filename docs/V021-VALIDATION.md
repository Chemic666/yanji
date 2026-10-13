# 研迹 V0.2.1 验证记录

终稿校正：将单会话审查的限制准确表述为本轮执行方式，撤回“用户明确禁止子代理”的错误归因；README 更新到当前交付入口。这些校正不改变产品源码、测试源码或设计资源。配对截图仍保留 2026-10-13 的原始合成 fixture 与捕获提交，未在午夜后补造同日截图；其产品源码与最终交付提交相同，源码等价核查和最终程序集身份见交付证据。文档校正后重新构建自包含程序、运行原生回归并完成解压启动与哈希检查。

本报告只计本次恢复实施后的实际执行。所有数据来自新建隔离合成目录；未读取真实用户记录。原生窗口 Windows 11 x64 / .NET 10.0.12，DPI=192（真实200%），实际900×600、1200×800 DIP。PNG像素尺寸不能当作DPI模拟。

## 自动验证

| 门禁 | 实际结果 | 证据 |
|---|---|---|
| 核心/数据测试 | 70 PASS，0失败、0跳过 | artifacts/v021-tests/v021-final.trx；core-tests-final.log |
| WPF原生操作 | 最终122项门禁：旧90+新增32；发布程序实际结果见completion.json与final-native-path.txt | artifacts/v021-implementation/native-checks-scope-fix.log；发布程序results.json |
| 同fixture事实 | 38次学习，原始记录字段/片段、任务、章节、薄弱点、感想、设置、排除与修订事实一致 | facts-comparison.json；before/final canonical.json；隔离SQLite |
| V0.1.1→0.2.1兼容 | 全表不变；旧新完整备份校验恢复、片段排除恢复往返；444次记录，完整性ok，migration none | compatibility-path.txt指向本轮compatibility.json，ReaderVersion=0.2.1.0 |
| V0.2.0UI基线事实 | 本轮before由原HEAD产品UI+新隔离捕获辅助生成；与after配对事实相同 | before与final；基线源码来自bfe616e，不把旧截图当本轮before |
| 构建 | 单线程、禁节点复用/共享编译，最终exit0，0警告0错误 | build-*.log；publish.log |
| 发布验证 | 程序 ProductVersion、源提交、ZIP SHA、解压所有文件hash、3次无共享运行时启动 | delivery-manifest.json / package-validation.json；最终结果以实际文件为准 |

正常命令：`tools/Invoke-Build.ps1 -Arguments @('build','YanJi.slnx','-c','Release','--no-restore','-m:1','-nr:false','-p:UseSharedCompilation=false')`。测试同样串行。发布RID restore只使用已有缓存，`--locked-mode --source artifacts/v021-implementation/empty-nuget-feed`成功，无新增下载或安装。

原生行为沿用旧90项并扩展：900/1200今日三动作首屏、独立内容双栏、主图轴/口径完整、无主横溢出、compact详情命名关闭与Escape回原焦点、Ranking每科目单hit、Enter和owned pointer routed mouse传原范围/科目、贡献和原值sum一致、真实Tooltip毫秒与筛选清旧tip、全零/1ms/极大值、自然时长0/1/1555/59000/60000/61001/3600001ms显示层精度；900长文编辑器浅深错误状态保持保存/取消可见和多行内容原样保存。物理鼠标点击不是注入测试，routed mouse明确标为路由事件；真实鼠标移动/原生popup已实际执行。

## 原生视觉与失败修复

before60张＝七页×浅深×两窗口×空/长文56，加详情4。最终配对数量以screenshots.json、image-hashes.json为准，comparison.html包含120个本地PNG依赖。源数据完全合成，纯时长没有伪造时间位置，跨日10+20分仍同一整次30分。CreateAt、revisions.at/id与revision JSON中的创建时间由数据库生成，显式排除后比较；原始审计文件保留，不能声称逐字节相同。

第一轮批量观察全部页面；第二轮确认后技术失败继续按证据修复：

1. 初版捕获器缺显式using，修复后编译并成功获取before；没有重复插入入口。
2. PowerShell `-p:`歧义改参数数组；沙箱启动残留进程仅核验本轮PID/EXE路径后清理，文件占用失败日志保留。
3. 旧“900今日必须纵向”断言与批准新布局冲突；更新为“导航折叠、今日双栏、统计堆叠”，新增实际视口检查，不减弱业务测试。
4. 新版900计时底部、图轴/口径裁切：修复窄计时字段布局与MeasureOverride不服从available.Height；工具栏预设与页头合并、指标组收紧。真实边界断言先失败后通过。
5. 薄弱点单行搜索显式40DIP，增加实际高度检查。无新增动画、方向探索或无限美化。

最终 view_image 检查采用原生主页面原图与其余页面分组contact sheet；长文/空态/浅深、900/1200和详情都纳入观察。contact sheet仅审查索引，不宣称它的缩放代表系统DPI；原始PNG完整交付。1200今日至少3条摘要动作、回顾、七日绘图区可用；900计时全部动作与统计完整主图/轴/口径可见。统计科目横条、周月节奏/热力/章节自然往下滚动；长文通过详情和滚动保留。

## 性能（默认渲染，实际四样本）

| 状态/检查点 | 秒 | 单核CPU | 工作集峰值MiB | 平均计时保存ms |
|---|---:|---:|---:|---:|
| idle | 60.30 | 1.76% | 231.69 | 0 |
| timer / 2秒 | 60.61 | 16.94% | 256.24 | 9.55 |
| timer / 15秒 | 60.63 | 1.83% | 250.71 | 10.96 |
| timer / 60秒 | 62.67 | 1.40% | 251.86 | 10.41 |

四样本Status均PASS，意为采样完整未被计时异常中断，不是跨设备性能阈值认证。2秒检查点更高CPU如实保留，不声称整体速度提高；没有逐帧GPU或输入延迟测量。证据 artifacts/v021-performance/report/performance.json。

当前浅深20组语义正文/元信息/动作token对比度计算全部≥4.5，最低5.24，证据contrast-current.json；不是对每个像素或全部状态的认证。

## NOT TESTED / 支持边界

- 其他实际DPI、多屏、系统文本放大、物理IME、Narrator、真实锁屏/睡眠/休眠/系统时钟与高对比切换、干净机物理断网；未改变这些系统设置。
- 近十年逐日图密度仍有限制；保留周期趋势与原始记录路径，不宣称所有点都有40DIP鼠标目标。
- 视觉截图为software rendering的真实HWND PrintWindow；性能为默认渲染。没有Web DOM/CSS或iOS/Android验收。
- Impeccable context实际exit0并加载新文档，但不识别WPF平台/XAML；没有完整native引擎认证。当前单模型审查按用户要求未启动独立代理。
- 本轮before保存前未单独记录完整EXE hash；已保留源码HEAD、实际抓取顺序、fixture与raw事实；after与发布阶段有源码/二进制hash。此项不补造历史hash。
