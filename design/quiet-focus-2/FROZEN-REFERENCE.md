# Quiet Focus 2.0「澄境」· 冻结设计依据

2026-10-18：用户正式冻结A「澄境」RC2及最后一轮视觉抛光。不得把早期纯色HTML原型作为完整视觉基准；B不参与正式实现。

- 原始确认图：`concepts/A-inspiration.png`，1536×1024；SHA-256 `E9C309231C85596CE76F2611BF912DADC7416F6AD5B5A945D2DCCAB4AC231087`。它是项目设计阶段的AI生成概念参考，不是实际WPF截图，不包含真实学习事实，不作为运行时背景或应用Logo使用。
- 实际背景：`src/YanJi.App/Assets/Atmosphere/window-light-v1.png`，1536×1024；SHA-256 `524FCF84FCEF1114AA1DB50ECE0A0150B6180C708EAA47C9A580BE821A54D6CD`。专门生成的无文字室内窗景；生成标识、提示词及权利边界见同目录README.md。
- 已确认WPF效果：`artifacts/chengjing-polish/review/comparison.html`、`YanJi-Chengjing-Visual-Polish-Review.zip`，该ZIP SHA-256 `BEC7676467801C2AF60B0E75710BC533F284B5A22B3475703D7EC4DBFE561E1C`。只作为本地审核索引，不把PNG测试截图提交进Git。
- 字体：仅引用Windows系统SimSun、Microsoft YaHei UI、Georgia、Cambria、Segoe UI，不复制/分发字体。现有应用图标与第三方声明不变。

原始概念图与背景由项目AI设计工作产生。背景按项目现有Apache-2.0许可提供；概念图仅保留内部设计追溯用途，不将其中示例Logo或界面元素当成可独立授权素材。AI输出不保证独占性、版权适格性或第三方权利；[OpenAI Terms of Use](https://openai.com/policies/terms-of-use/)的Output归属规则不替代该边界。

早期`DESIGN.md`、`VALIDATION.md`、A/B HTML原型、浏览器截图及QA失败图原样留在本地设计目录，未混入本次冻结提交。最终规则以`docs/QUIET-FOCUS-2-FROZEN.md`及已审核WPF源码为准。此后普通文案问题进入后续清单，不继续调整布局和材质。
