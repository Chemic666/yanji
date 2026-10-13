param([Parameter(Mandatory=$true)][string]$Destination)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
$destinationPath=[IO.Path]::GetFullPath($Destination)
if(Test-Path -LiteralPath $destinationPath){throw 'Destination already exists; refusing to overwrite existing delivery.'}
$regular=(Get-Content (Join-Path $repo 'artifacts/latest-v02-final-release.txt') -Raw).Trim()
$empty=(Get-Content (Join-Path $repo 'artifacts/latest-v02-final-empty.txt') -Raw).Trim()
$baseline=(Get-Content (Join-Path $repo 'artifacts/latest-v02-baseline.txt') -Raw).Trim()
foreach($evidence in @($regular,$empty)){
  $result=Get-Content (Join-Path $evidence 'execution.json') -Raw | ConvertFrom-Json
  if($result.ExitCode -ne 0){throw 'Selected evidence did not exit successfully.'}
}
$checks=Get-Content (Join-Path $regular 'screenshots/interaction-results.json') -Raw | ConvertFrom-Json
if($checks.Count -ne 28 -or @($checks | Where-Object Status -ne 'PASS').Count){throw 'Runtime checks not complete.'}
$working=(& git -C $repo status --porcelain)
if($LASTEXITCODE -ne 0 -or $working){throw 'Commit the reviewed source before packaging.'}
$commit=(& git -C $repo rev-parse HEAD).Trim()
$release=Join-Path $repo 'releases/YanJi-v0.2-visual-preview-win-x64'
if(!(Test-Path (Join-Path $release 'YanJi.DesignLab.exe'))){throw 'Preview executable missing.'}
New-Item -ItemType Directory -Path $destinationPath | Out-Null
$images=New-Item -ItemType Directory -Path (Join-Path $destinationPath 'images')
$evidenceDir=New-Item -ItemType Directory -Path (Join-Path $destinationPath 'evidence')
foreach($pair in @(@($regular,'normal'),@($empty,'empty'),@($baseline,'baseline'))){
  $source=Join-Path $pair[0] 'screenshots'
  $target=New-Item -ItemType Directory -Path (Join-Path $images.FullName $pair[1])
  Copy-Item -Path (Join-Path $source '*.png') -Destination $target.FullName
  $receipt=New-Item -ItemType Directory -Path (Join-Path $evidenceDir.FullName $pair[1])
  Get-ChildItem -LiteralPath $source -File | Where-Object Extension -eq '.json' | Copy-Item -Destination $receipt.FullName
  if(Test-Path (Join-Path $pair[0] 'execution.json')){Copy-Item -LiteralPath (Join-Path $pair[0] 'execution.json') -Destination $receipt.FullName}
  if(Test-Path (Join-Path $pair[0] 'exit-verification.json')){Copy-Item -LiteralPath (Join-Path $pair[0] 'exit-verification.json') -Destination $receipt.FullName}
}
Copy-Item -LiteralPath (Join-Path $repo 'artifacts/v02-tests-final/v02-final.trx') -Destination $evidenceDir.FullName
Copy-Item -LiteralPath (Join-Path $repo 'docs/design/v02') -Destination (Join-Path $destinationPath 'docs') -Recurse
$program=Join-Path $destinationPath 'YanJi-visual-preview-win-x64'
Copy-Item -LiteralPath $release -Destination $program -Recurse
Copy-Item -LiteralPath (Join-Path $repo 'THIRD-PARTY-NOTICES.md') -Destination $program
Copy-Item -LiteralPath (Join-Path $repo 'docs/design/v02/USER-GUIDE.md') -Destination (Join-Path $program '使用说明.md')
@'
研迹 V0.2 双方案视觉样板（非正式升级）

双击 YanJi.DesignLab.exe；在顶部切换 A/B 与浅深主题，再打开今日或统计。
全部记录来自独立示例数据库，不连接或迁移正式学习数据。
请保留整个文件夹。窗口关闭暂停并保存后退出，样板不提供托盘。
完整说明见 使用说明.md；正式日常学习继续使用 V0.1.1。
'@ | Set-Content -LiteralPath (Join-Path $program '开始比较.txt') -Encoding utf8
$manifest=@{Version='0.2.0-preview.1';SourceCommit=$commit;BaselineCommit='f8e5cd8eb2fca333c2d1988f78cae8e3c771194b';FormalCoreChanges=$false;RegularEvidence=$regular;EmptyEvidence=$empty;NativeWindowImages=29;RuntimeAssertions=28;RegressionTests=59;UserDirection='PENDING';Files=@()}
$manifest.Files=@(Get-ChildItem -LiteralPath $program -File -Recurse | ForEach-Object { @{Path=[IO.Path]::GetRelativePath($program,$_.FullName);Bytes=$_.Length;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash} })
$manifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $destinationPath 'manifest.json') -Encoding utf8
$zip=Join-Path $destinationPath 'YanJi-v0.2-visual-preview-win-x64.zip'
Compress-Archive -LiteralPath $program -DestinationPath $zip -CompressionLevel Optimal
$sourceZip=Join-Path $destinationPath 'YanJi-v0.2-visual-source.zip'
& git -C $repo archive --format=zip ('--output='+$sourceZip) HEAD
if($LASTEXITCODE -ne 0){throw 'Source archive failed.'}
@'
<!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>研迹 · 双方案比较</title>
<style>body{margin:0;background:#f5f4ef;color:#29352e;font:16px "Segoe UI","Microsoft YaHei UI",sans-serif}header,main{max-width:1580px;margin:auto;padding:28px}h1{font-size:28px;margin:0 0 8px}p{line-height:1.65}nav{display:flex;gap:10px;flex-wrap:wrap;margin:22px 0}button{font:inherit;border:1px solid #a4b5a8;border-radius:8px;background:white;padding:10px 20px;color:inherit;cursor:pointer}button[aria-pressed=true]{background:#456653;color:white}button:focus-visible,a:focus-visible{outline:3px solid #6c87a0;outline-offset:3px}.pair{display:grid;grid-template-columns:1fr 1fr;gap:22px}figure{margin:0}h2{font-size:20px}img{display:block;width:100%;border:1px solid #d9ded3;border-radius:5px}small{display:block;margin:10px 0;color:#69766a}a{color:#456653}aside{border-top:1px solid #d9ded3;margin-top:26px;padding-top:16px}select{font:inherit;padding:8px;background:white;color:inherit}details{margin-top:24px}summary{cursor:pointer}.thumbnails{display:grid;grid-template-columns:repeat(2,1fr);gap:16px;margin-top:14px}@media(max-width:1000px){.pair{grid-template-columns:1fr}header,main{padding:20px}}</style></head>
<body><header><h1>研迹 · V0.2 视觉方向比较</h1><p>两套真实 WPF 窗口样板。推荐 A 静桌：暖纸色、鼠尾草、学习与回顾优先。B 工作台更紧凑，日志与趋势优先。</p>
<nav aria-label="比较页面"><button id="today" aria-pressed="true">今日学习</button><button id="statistics" aria-pressed="false">统计与复盘</button><button id="theme" aria-pressed="false">切换为深色</button><label>状态 <select id="state"><option value="normal">示例记录</option><option value="empty">空数据</option><option value="small">900 × 600</option></select></label></nav>
<small>截图由发布版自己的 Windows HWND 捕获，200% DPI。点击图片打开原始 PNG。这个 HTML 是离线图册，不是应用实现。</small></header>
<main><section class="pair"><figure><h2>A 静桌 · Quiet Desk</h2><a id="linkA"><img id="imageA" alt="A 静桌实际 WPF 窗口"></a><small>柔和、主次明确；文字导航占宽，小窗次要内容纵向滚动。</small></figure><figure><h2>B 专注工作台 · Focus Studio</h2><a id="linkB"><img id="imageB" alt="B 专注工作台实际 WPF 窗口"></a><small>更宽日志和趋势；图标需要熟悉，科目比例没有环形直观。</small></figure></section>
<aside><p><a href="YanJi-visual-preview-win-x64/开始比较.txt">启动说明</a> · <a href="docs/AUDIT-AND-SPEC.md">审计与设计规范</a> · <a href="docs/VALIDATION.md">验证报告与限制</a> · <a href="docs/COMPARISON.md">完整方案比较</a> · <a href="YanJi-v0.2-visual-preview-win-x64.zip">Windows 程序包</a></p><p>只操作独立示例数据；正式 V0.1.1 数据和计时实现保留。59 项业务回归与 28 项样板运行检查通过；其他 DPI、物理输入、真实休眠仍未验证。最后只需选择 A 或 B，之后才推广六个页面。</p></aside>
<details><summary>原 V0.1.1 实际窗口（用于对照审计）</summary><div class="thumbnails"><img src="images/baseline/baseline-today-light.png" alt="V0.1.1 今日浅色"><img src="images/baseline/baseline-statistics-light.png" alt="V0.1.1 统计浅色"></div></details></main>
<script>let page='today',dark=false;const state=document.getElementById('state');function update(){if(state.value==='small'&&dark){dark=false}for(const scheme of ['A','B']){let folder=state.value==='empty'?'empty':'normal';let suffix=state.value==='empty'?(dark?'dark-empty':'light-empty'):state.value==='small'?'900x600':dark?'dark':'light';let url=`images/${folder}/${scheme}-${page}-${suffix}.png`;document.getElementById('image'+scheme).src=url;document.getElementById('link'+scheme).href=url;}document.getElementById('today').setAttribute('aria-pressed',String(page==='today'));document.getElementById('statistics').setAttribute('aria-pressed',String(page==='statistics'));document.getElementById('theme').setAttribute('aria-pressed',String(dark));document.getElementById('theme').textContent=dark?'切换为浅色':'切换为深色';document.getElementById('theme').disabled=state.value==='small';}document.getElementById('today').onclick=()=>{page='today';update()};document.getElementById('statistics').onclick=()=>{page='statistics';update()};document.getElementById('theme').onclick=()=>{dark=!dark;update()};state.onchange=update;update();</script></body></html>
'@ | Set-Content -LiteralPath (Join-Path $destinationPath 'comparison.html') -Encoding utf8
@'
# 研迹 V0.2 视觉探索交付

先打开 comparison.html 比较两套真实 WPF 页面；或进入 YanJi-visual-preview-win-x64 双击 YanJi.DesignLab.exe。

- 原生今日/统计，各有浅深主题与空态；可实际计时、补录、修订备注、软删除恢复及图表下钻。
- docs：问题审计、双方向规范、组件映射、验证报告、使用说明。
- evidence：28项运行断言、59项业务回归TRX、环境和捕获记录；不含数据库。
- images：实际原生截图及辅助 visual-tree，另有V0.1.1对照。
- 程序 ZIP 为自包含Windows x64；source ZIP 为完整源码恢复点；manifest记录源码提交与发布文件SHA256。

本轮推荐 A 静桌；等待你最终选择 A 或 B。它是隔离设计样板，六页面正式升级还未执行。
'@ | Set-Content -LiteralPath (Join-Path $destinationPath 'README.md') -Encoding utf8
@($zip,$sourceZip) | ForEach-Object { Get-FileHash -LiteralPath $_ -Algorithm SHA256 } | Select-Object Path,Hash | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $destinationPath 'package-hashes.json') -Encoding utf8
Get-ChildItem -LiteralPath $destinationPath -File | Select-Object Name,Length
