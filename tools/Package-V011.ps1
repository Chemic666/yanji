param([string]$OutputDirectory='')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
if (-not $PSBoundParameters.ContainsKey('OutputDirectory')) { $OutputDirectory = Join-Path $repo 'artifacts/legacy-deliveries/v0.1.1' }
$release=Join-Path $repo 'releases\YanJi-v0.1.1-win-x64'
$desktop=Get-Content "$repo\artifacts\latest-v011-desktop.txt"
$perfRoot=Get-Content "$repo\artifacts\latest-v011-performance.txt"
$upgrade=Get-Content "$repo\artifacts\latest-v011-upgrade.txt"
$crash=Get-Content "$repo\artifacts\latest-v011-crash.txt"
if((git -C $repo status --porcelain).Length -gt 0){throw 'Commit reviewed source before packaging.'}
$commit=(git -C $repo rev-parse HEAD).Trim()
$rows=Get-Content "$desktop\smoke\results.json" -Raw | ConvertFrom-Json
if(@($rows | Where-Object Status -ne 'PASS').Count -or $rows.Count -lt 63){throw 'Final actual GUI verification is incomplete.'}
if((Get-Content "$upgrade\compatibility.json" -Raw | ConvertFrom-Json).Status -ne 'PASS'){throw 'Old data compatibility verification missing.'}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$runtimeZip=Join-Path $OutputDirectory 'YanJi-v0.1.1-win-x64.zip'
$sourceZip=Join-Path $OutputDirectory 'YanJi-v0.1.1-source.zip'
$evidenceZip=Join-Path $OutputDirectory 'YanJi-v0.1.1-evidence.zip'
foreach($name in @('YanJi-v0.1.1-win-x64.zip','YanJi-v0.1.1-source.zip','YanJi-v0.1.1-evidence.zip','delivery-manifest.json','package-validation.json')){if(Test-Path -LiteralPath (Join-Path $OutputDirectory $name)){throw "Preserve existing output: $name"}}
Copy-Item "$repo\docs\V011-USER-GUIDE.md" "$release\USER-GUIDE.md"
Copy-Item "$repo\docs\V011-VALIDATION.md" "$release\VALIDATION.md"
Copy-Item "$repo\docs\V011-DESIGN.md" "$release\DESIGN.md"
Copy-Item "$repo\THIRD-PARTY-NOTICES.md" "$release\THIRD-PARTY-NOTICES.md"
Copy-Item "$repo\releases\YanJi-v0.1.0-win-x64\licenses" "$release\licenses" -Recurse
@'
研迹 YanJi V0.1.1 · Windows 11 x64
完整解压文件夹后双击 YanJi.exe；无需安装 .NET SDK。
默认数据：%LOCALAPPDATA%\YanJi。发布包不包含测试学习数据。
升级前先在旧版导出并校验完整备份，关闭旧版再启动新版。
记录明细提供编辑、软删除与恢复；详情可排除和恢复原始片段。
活动计时点 X 隐藏到托盘。明确退出使用“退出应用”。
说明：USER-GUIDE.md；实测与限制：VALIDATION.md；组件规范：DESIGN.md。
本机验证版本；真实电源、中文输入法、跨屏 DPI 与干净机仍有未测项。
'@ | Set-Content "$release\开始使用.txt" -Encoding utf8
$files=@(Get-ChildItem -LiteralPath $release -Recurse -File | Where-Object Name -ne 'file-manifest.json' | Sort-Object FullName | ForEach-Object {[pscustomobject]@{Path=[IO.Path]::GetRelativePath($release,$_.FullName);Bytes=$_.Length;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}})
@{Version='0.1.1';SourceCommit=$commit;Files=$files} | ConvertTo-Json -Depth 6 | Set-Content "$release\file-manifest.json" -Encoding utf8
[IO.Compression.ZipFile]::CreateFromDirectory($release,$runtimeZip,[IO.Compression.CompressionLevel]::Optimal,$true)
git -C $repo archive --format=zip --output=$sourceZip HEAD
if($LASTEXITCODE -ne 0){throw 'Source archive failed.'}
$evidence=Join-Path $repo ('artifacts\v011-evidence-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path "$evidence\screenshots","$evidence\desktop","$evidence\tests","$evidence\upgrade","$evidence\crash","$evidence\logs" | Out-Null
Copy-Item "$desktop\smoke\*.png" "$evidence\screenshots"
Copy-Item "$desktop\smoke\*.json","$desktop\startup-results.json","$perfRoot\performance\performance.json" "$evidence\desktop"
Copy-Item "$perfRoot\performance\results.json" "$evidence\desktop\performance-results.json"
$prior=Get-Content "$repo\artifacts\v011-performance-prior.txt"
Copy-Item "$prior\performance\sampling-audit.json" "$evidence\desktop"
Copy-Item "$repo\artifacts\test-results\v011-final.trx" "$evidence\tests"
Copy-Item "$upgrade\compatibility.json","$upgrade\compatibility-interrupted.json" "$evidence\upgrade"
Copy-Item "$crash\results.json","$crash\restore-results.json","$crash\ready.json" "$evidence\crash"
Copy-Item "$repo\artifacts\v011-final-tests.log","$repo\artifacts\v011-final-build.log","$repo\artifacts\v011-final-publish.log","$repo\artifacts\v011-desktop.log","$repo\artifacts\v011-compatibility.log","$repo\artifacts\v011-crash.log" "$evidence\logs"
Copy-Item "$repo\docs\V011-VALIDATION.md","$repo\docs\V011-DESIGN.md" $evidence
Copy-Item "$repo\artifacts\v011-validation-matrix.csv" $evidence
$extract=Join-Path $repo ('artifacts\v011-package-'+[Guid]::NewGuid().ToString('N'))
[IO.Compression.ZipFile]::ExtractToDirectory($runtimeZip,$extract)
$extractedRelease=Join-Path $extract 'YanJi-v0.1.1-win-x64'
$manifest=Get-Content "$extractedRelease\file-manifest.json" -Raw | ConvertFrom-Json
foreach($file in $manifest.Files){if((Get-FileHash -LiteralPath (Join-Path $extractedRelease $file.Path) -Algorithm SHA256).Hash -ne $file.Sha256){throw "Extracted file mismatch: $($file.Path)"}}
$env:DOTNET_ROOT=Join-Path $extract 'missing-shared-runtime'
$process=Start-Process -FilePath "$extractedRelease\YanJi.exe" -ArgumentList @('--data-dir',"$extract\data",'--startup-probe',"$extract\report") -WindowStyle Hidden -PassThru
Set-Content "$extract\owned-pid.txt" $process.Id
if(!$process.WaitForExit(30000)){throw "Owned package startup still running, PID $($process.Id); no process terminated."}
$startup=Get-Content "$extract\report\startup.json" -Raw | ConvertFrom-Json
if($process.ExitCode -ne 0 -or $startup.Status -ne 'PASS' -or $startup.RuntimeDirectory.TrimEnd('\') -ne $extractedRelease){throw 'Archive self-contained startup failed.'}
$packageResult=@{Status='PASS';VerifiedFileHashes=$manifest.Files.Count;ExitCode=$process.ExitCode;Startup=$startup;SourceCommit=$commit}
$packageResult | ConvertTo-Json -Depth 6 | Set-Content "$OutputDirectory\package-validation.json" -Encoding utf8
Copy-Item "$OutputDirectory\package-validation.json" $evidence
[IO.Compression.ZipFile]::CreateFromDirectory($evidence,$evidenceZip)
Copy-Item "$repo\docs\V011-VALIDATION.md" "$OutputDirectory\YanJi-v0.1.1-validation.md"
Copy-Item "$repo\docs\V011-USER-GUIDE.md" "$OutputDirectory\YanJi-v0.1.1-user-guide.md"
Copy-Item "$repo\docs\V011-DESIGN.md" "$OutputDirectory\YanJi-v0.1.1-design.md"
New-Item -ItemType Directory -Path "$OutputDirectory\screenshots" | Out-Null
Copy-Item "$desktop\smoke\*.png" "$OutputDirectory\screenshots"
$deliverables=@(Get-ChildItem -LiteralPath $OutputDirectory -File | Sort-Object Name | ForEach-Object {[pscustomobject]@{Name=$_.Name;Bytes=$_.Length;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}})
@{Version='0.1.1';SourceCommit=$commit;GuiPass=$rows.Count;DataTestPass=59;Compatibility='PASS';Package='PASS';ReleaseBytes=($files | Measure-Object Bytes -Sum).Sum;Files=$deliverables;Limitations=@('Real IME and physical keyboard','Actual DPI changes and cross-monitor','Real sleep/hibernate/lock/clock changes','Physical offline and clean Windows','Unsigned portable archive; no installer validation')} | ConvertTo-Json -Depth 7 | Set-Content "$OutputDirectory\delivery-manifest.json" -Encoding utf8
$deliverables | ForEach-Object {"$($_.Sha256)  $($_.Name)"} | Set-Content "$OutputDirectory\sha256.txt" -Encoding utf8
Write-Output "PASS: $($manifest.Files.Count) extracted hashes, actual self-contained startup; $OutputDirectory"
