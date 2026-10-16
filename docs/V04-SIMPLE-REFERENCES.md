# RC-Simple 原创参考目录与适用范围

本目录是 YanJi 原创学习主题结构，采用 CC0-1.0；不是官方考试大纲，也不是2028大纲、院校招生要求或考试预测。仅含主题名称与少量原创说明，不包含教材正文、试题、答案、讲义、受版权保护的词表。外部网页和扫描件的版权不在本目录的CC0授权内，也不随程序分发。

## 版本与身份

- 7个历史1.0.0资源全部保留，生成器不写入旧版本目录，并在运行前后逐文件校验原字节。
- 新增同PackageId的1.1.0：408（177节点）、math1/math2（各126节点）、english1/english2（各92节点）。政治与空白模板继续使用1.0.0。
- 408的原145个外部ID、父子关系和名称全部保留，新增32节点，含串、朴素匹配、KMP与next数组，以及四科其他补充主题。沿用原有三方预览、确认、备份、增量更新机制。
- 数学、英语各旧包的外部ID全部保留；每个新变体包都含同科共同与差异主题。同一安装内的范围切换只过滤展示，共同节点的数据库ID、学习进度、记录关联均不改变。不因切换安装另一个变体包，不自动合并历史分别安装的包。
- `Maintainer`和`Sources`的Id/Url集合原样保留：现有Data将其作为来源身份，不可在版本更新时增加来源而导致升级失败。下文是本次额外核对记录，并非包来源身份的变更。
- `index.json`每个PackageId只指向最新版本；旧包仍可用于历史升级与回滚验证。

## 内容与过滤范围

| 范围 | 原创目录内容 |
|---|---|
| 数学共同 | 极限与连续、微分学、积分学、多元函数与二重积分、微分方程；行列式与矩阵、向量与线性方程组、特征值与二次型 |
| 数学一扩展 | 空间解析几何与向量、三重积分、曲线曲面积分、级数、概率论与数理统计（分布、数字特征、极限定理、统计推断） |
| 数学二 | 显示数学共同主题；隐藏数学一扩展整棵子树，因此不含概率、空间几何、三重积分、曲线曲面积分和无穷级数；保留二重积分 |
| 英语共同 | 词汇、语法、长难句、完形填空、阅读、翻译基础、应用文写作、复习复盘；新题型小标题与概括句匹配 |
| 英语一侧重 | 新题型段落排序与选句填空、语篇划线句翻译、图画作文训练 |
| 英语二侧重 | 新题型多项匹配（信息对应）、段落翻译、图表作文训练 |

图画/图表是学习训练侧重点，不是对官方写作提示形式的排他断言。历史大纲的提示形式可能包括图画、图表或文字；本目录没有声称英语一只能考图画或英语二只能考图表。完整词表、分值、字数、当年调整及院校专属要求不由本目录提供。

## 外置范围接口

`resources/knowledge-packages/reference-scopes.json`为独立SchemaVersion=1清单，含436条节点映射。每条为`PackageId`、`ExternalNodeId`、可空`ParentId`和`Variants`。不向严格解析的知识包Schema添加字段。

`YanJi.Core.ReferenceScopes`接口：

```csharp
public static ReferenceScopes Load(string directory);
public bool Includes(string packageId, string externalNodeId, string variant);
public IReadOnlyList<ReferenceScopeEntry> Entries { get; }
public sealed record ReferenceScopeEntry(
    string PackageId, string ExternalNodeId, string? ParentId,
    IReadOnlyList<string> Variants);
```

`directory`为knowledge-packages根目录。`common`只选择共同节点；`math1/math2/english1/english2`选择共同节点及自身差异；`all`显示全部。空值/未知类别保守地显示全部。未映射包、未映射外部ID以及自定义节点始终可见。映射应使用安装绑定的外部ID，不可用本地显示名称推断，也不可将本地数据库节点ID误作外部ID。

读取器补足祖先闭包；缺父节点、环、过深目录、重复映射、非法类别和未知JSON字段报错。缺少可选清单返回空映射，保留全部展示；App应在清单损坏时向用户说明并保守展示。读取器仅返回筛选结果，无数据库写入或安装行为。

## 本次实际访问来源及限制

核对日期：2026-10-16。以下链接均实际访问；不据此断言2028官方范围已公布。

1. [教育部教育考试院硕士研究生考试大纲历史索引](https://yankao.neea.edu.cn/html1/category/1509/6235-2.htm)：页面列出2017数学、英语一、英语二、408入口（2016-10-27），可确认历史来源和发布时间。
2. [2017英语一官方入口](https://yankao.neea.edu.cn/html1/report/16103/1379-1.htm)、[2017英语二官方入口](https://yankao.neea.edu.cn/html1/report/16103/1374-1.htm)：正文只有三张图片。本次实际查看了各自第三张目录扫描页（[英语一](https://yankao.neea.edu.cn/res/Home/1610/161031378.jpg)、[英语二](https://yankao.neea.edu.cn/res/Home/1610/161031373.jpg)），确认其为历史出版物目录，未获取完整考试内容正文；不能以这些入口声称已逐条核验官方题型。
3. [2017数学官方入口](https://yankao.neea.edu.cn/html1/report/16103/1384-1.htm)、[2017计算机综合官方入口](https://yankao.neea.edu.cn/html1/report/16103/1332-1.htm)：实际打开历史页面，未声称完成扫描正文逐项比对。
4. [研招网2018英语二大纲解读](https://yz.chsi.com.cn/kyzx/en/201709/20170915/1628558459.html)：交叉核对英语二新题型为多项对应及小标题对应；这是历史解读，不能代替当年官方要求。
5. [中国教育在线2017英语一综述](https://kaoyan.eol.cn/kao_shi_da_gang/yingyu/201608/t20160826_1443047.shtml)：交叉核对新题型排序、填空、标题三个训练方向。该页为培训机构历史解读，非官方发布。
6. [中国教育在线2020英语二文字转载](https://kaoyan.eol.cn/kao_shi_da_gang/yingyu/201907/t20190716_1670862.shtml)：交叉核对多项对应/小标题、段落翻译及写作提示形式不止图表。转载有编辑错误可能，不据此复制分值、字数或声称当年完整准确。

旧包中的MIT、CMU等课程链接本次没有重新验证；保留是为了来源身份兼容，不表示本次核实课程现状。408补充目录依据计算机基础主题组织，不保证与任何年度大纲完全一致。

## 生成与验证

`tools/Generate-SimpleReferences.py`仅使用本地旧包与原创主题数据，不联网、不下载。使用语义ID追加节点，不重新编号历史ID。已存在1.1.0内容若不同会拒绝覆盖；再次执行相同内容不改文件。ContentHash使用现有规则：移除根ContentHash、对象键Ordinal顺序、数组保序、紧凑UTF-8 JSON的SHA-256大写表示。

本次静态验证证据：`artifacts/v04-rc-simple/references-static.json`（全部12个包哈希/结构、7个旧文件经Git文本换行归一化后与HEAD一致、436节点范围祖先闭包）；`references-generator.json`（生成器再次运行及旧包运行前后原字节不变）。旧工作区文件已有CRLF，与Git存储LF的字节区别没有被重写。

定向测试为`SimpleReferenceTests`（索引/旧ID/范围/真实文件SQLite408升级幂等、本地改名/学习事实/重开，以及同安装数学英语类别切换身份稳定）与`KnowledgePackageBuiltinTests`（全部包由生产解析器校验、latest分组及408安装）。隔离根目录固定为`artifacts/v04-rc-simple/references-test-data`，不访问用户库。

初次通过`tools/Invoke-Build.ps1`触发测试未产生dotnet输出或TRX，日志`references-red.log`保留；由根会话集中处理工具链和执行定向验收，最终TRX及日志以其实际结果为准。本子任务不据静态验证宣称SQLite测试已通过，不运行全量测试或自包含发布。
