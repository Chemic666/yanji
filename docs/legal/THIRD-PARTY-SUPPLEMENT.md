# RC1 源码分发第三方声明补充

本文件补充RC1源包中的开发辅助内容，不授权研迹本身，不改变原RC1 ZIP或任何原许可。

1. `.agents/skills/impeccable/scripts/modern-screenshot.umd.js`：modern-screenshot，Copyright (c) 2021-present wxm，MIT；完整原文见`third-party/modern-screenshot-MIT.txt`。上游 https://github.com/qq15725/modern-screenshot 。本地文件SHA-256 `BB36665889124A0B6E15F16045265737449C3BDCF2712CDB08AF3CFA01563E2B`，与Impeccable@245ad82def45831dc56a21b683c7a1ff530b06cc相同路径字节一致。未改动或执行。
2. `.agents/skills/impeccable/reference/ios.md`与`android.md`：依Impeccable的UPSTREAM-NOTICE，改写自ehmo/platform-design-skills，MIT。原许可Copyright (c) 2026未注明姓名，**保持原文，不代填作者**；完整文本见`third-party/platform-design-skills-MIT.txt`。原Impeccable Apache许可与NOTICE同时继续保留。
3. `xunit.abstractions`2.0.3仅为测试依赖，不在Windows运行ZIP。旧nuspec许可链接指向迁移前仓库，本轮核对当前官方冻结接口仓库的Apache-2.0声明，见`third-party/xunit-abstractions-Apache-2.0.txt`；完整Apache许可在既有`resources/licenses/SQLitePCLRaw-Apache-2.0.txt`。此补充不声称重建了2018包发布提交。

所有补充原文来自官方上游main快照，URL/字节/SHA记录在本轮证据`upstream-text/notices-fetch.json`。正式源码发行应把这些声明和原有上游许可放在一起；不能只把历史RC1 source ZIP单独作为“合规已完成”源包。
