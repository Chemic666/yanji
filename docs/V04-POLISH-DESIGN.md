# RC-Polish · 局部交互规范

保留Quiet Focus与三主页面，不重组业务。暖纸色与鼠尾草绿、原有系统中文字体和8px间距沿用。

| 类型 | 资源 | 层次 |
|---|---|---|
| Primary | Primary | 鼠尾草绿实心、对比文字、SemiBold、44DIP最小高、14×8内边距 |
| Secondary | FocusButton / SecondaryButton | 柔和底色、1DIP细边框、40DIP最小高 |
| Ghost | QuietButton / GhostButton | 透明底、无边框、8×6内边距，更多/筛选/菜单 |
| Danger | DangerButton | 红色文字、浅红底和红细边，明确危险文案与确认 |
| Icon | IconButton | 32DIP最小目标、6DIP内边距，实际使用需说明或可访问名称 |

圆角8DIP，键盘焦点为外侧1.5DIP轮廓；Press减弱内容透明度，Hover/Press仅对轻量透明覆盖层做140ms过渡，不动画布局、字体、模糊或阴影。尊重SystemParameters.ClientAreaAnimation及高对比度；真实系统设置切换未执行。

Saving/Success使用共享ButtonFeedback附加状态，显式呈现“保存中…”/“已保存”，不只靠颜色；原绑定通过SetCurrentValue保留。EditDialog保存中禁用输入和重复提交、阻止中途关闭；失败保留值、重试清理旧错误提示；成功才结束有持久化回调的编辑。

SaveShortcuts集中处理WPF TextCompositionManager开始/更新/结束事件、IME处理键与同一输入轮次的提交屏障，并通过同一RoutedUICommand保存。单行Enter，多行Ctrl+Enter；多行Enter留给TextBox，搜索/下拉/只读场景排除。内联知识节点调用现有MVVM SaveCommand并在保存前更新绑定源。没有把模拟组合事件当成真实微软拼音验收。

高频编辑的CommitAsync覆盖科目、旧章节、任务、薄弱点、快速进度、补录、整次记录备注、结束后补充、考试档案和偏好；仍调用原有数据库服务。其余旧版升级/恢复路径选择及高级关联对话框属于确认流程，Enter仅确认，未重做其底层事务。

性能策略：每秒更新可信时间投影，后台检查点维持原间隔；同一运行状态不再Refresh整页。暂停、结束、自动完成、待核对、日期边界和其他明确修改仍刷新相应视图，原始事实和聚合规则保持不变。

Skill：实际读取yanji-domain-rules与dotnet-wpf参考，运行ui-ux-pro-max本地WPF查询“feedback focus animation”，落实绑定更新时机、MVVM、键盘可达与轻量反馈；systematic-debugging用于先测线程耗时再选修复。未安装新Skill/依赖，未调用Web设计引擎。

官方参考：[WPF组合输入事件](https://learn.microsoft.com/en-us/dotnet/api/system.windows.input.textcompositionmanager?view=windowsdesktop-10.0)、[系统动画偏好](https://learn.microsoft.com/en-us/dotnet/api/system.windows.systemparameters.clientareaanimation?view=windowsdesktop-10.0)。

最终定向验收发现并修正一个既存导航竞争：Navigation_Changed在OpenKnowledgeAsync的指定节点加载期间再次无目标Reload，覆盖选中状态。现有KnowledgeBusy为真时跳过重复导航加载；保留显式目标加载与稳定ID。原始失败、调用栈和修复后结果分别留存。
