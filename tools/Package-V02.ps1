param([string]$OutputDirectory='')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
if (-not $PSBoundParameters.ContainsKey('OutputDirectory')) { $OutputDirectory = Join-Path $repo 'artifacts/legacy-deliveries/v0.2-quiet-focus' }
$release=Join-Path $repo 'releases/YanJi-v0.2.0-win-x64'
$desktop=Get-Content "$repo/artifacts/latest-v02-desktop.txt"
$upgrade=Get-Content "$repo/artifacts/latest-v02-upgrade.txt"
$performance=Get-Content "$repo/artifacts/latest-v02-final-performance.txt"
if((git -C $repo status --porcelain).Length -gt 0){throw 'Commit verified source before packaging.'}
$commit=(git -C $repo rev-parse HEAD).Trim()
$productVersion=(Get-Item "$release/YanJi.exe").VersionInfo.ProductVersion
if($productVersion -ne "0.2.0+$commit"){throw 'Rebuild from the clean delivery commit so embedded provenance matches source.'}
$rows=Get-Content "$desktop/visual/results.json" -Raw | ConvertFrom-Json
if($rows.Count -lt 90 -or @($rows | Where-Object Status -ne 'PASS').Count){throw 'Final WPF checks are incomplete.'}
if((Get-Content "$upgrade/compatibility.json" -Raw | ConvertFrom-Json).Status -ne 'PASS'){throw 'Compatibility check missing.'}
if(@((Get-Content "$performance/report/performance.json" -Raw | ConvertFrom-Json) | Where-Object Status -ne 'PASS').Count){throw 'Performance sampling interrupted.'}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
foreach($file in @('YanJi-v0.2.0-win-x64.zip','YanJi-v0.2.0-source.zip','YanJi-v0.2.0-evidence.zip','delivery-manifest.json','package-validation.json')){
 if(Test-Path -LiteralPath (Join-Path $OutputDirectory $file)){throw "Existing delivery preserved: $file"}
}
Copy-Item "$repo/docs/V02-USER-GUIDE.md" "$release/USER-GUIDE.md"
Copy-Item "$repo/docs/V02-VALIDATION.md" "$release/VALIDATION.md"
Copy-Item "$repo/docs/V02-DESIGN.md" "$release/DESIGN.md"
Copy-Item "$repo/docs/V02-SKILLS.md" "$release/SKILLS.md"
Copy-Item "$repo/THIRD-PARTY-NOTICES.md" "$release/THIRD-PARTY-NOTICES.md"
$sourceLicenses="$repo/releases/YanJi-v0.1.1-win-x64/licenses"
if(!(Test-Path "$release/licenses")){Copy-Item $sourceLicenses "$release/licenses" -Recurse}
else{foreach($license in Get-ChildItem -LiteralPath $sourceLicenses -Recurse -File){$target=Join-Path "$release/licenses" ([IO.Path]::GetRelativePath($sourceLicenses,$license.FullName));if(!(Test-Path -LiteralPath $target) -or (Get-FileHash -LiteralPath $target).Hash -ne (Get-FileHash -LiteralPath $license.FullName).Hash){throw 'Retained runtime license differs; inspect before packaging.'}}}
@'
研迹 YanJi V0.2 · Windows 11 x64
完整解压文件夹后双击 YanJi.exe；无需安装 .NET SDK。
升级前在旧版导出并校验完整备份，退出旧版，再启动新版。
默认数据：%LOCALAPPDATA%\YanJi。发布包不包含测试记录和数据库。
浅深主题、学习记录、编辑/软删除/片段恢复、原始记录下钻见 USER-GUIDE.md。
真实设备验证边界见 VALIDATION.md；Skills 使用与限制见 SKILLS.md。
'@ | Set-Content "$release/开始使用.txt" -Encoding utf8
$manifest=@(Get-ChildItem -LiteralPath $release -Recurse -File | Where-Object Name -ne 'file-manifest.json' | Sort-Object FullName | ForEach-Object {
 [pscustomobject]@{Path=[IO.Path]::GetRelativePath($release,$_.FullName);Bytes=$_.Length;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}
})
if(@($manifest | Where-Object Path -Match '\.(sqlite|yanjibak|db)$').Count){throw 'Runtime package unexpectedly contains data.'}
@{Version='0.2.0';SourceCommit=$commit;Files=$manifest} | ConvertTo-Json -Depth 6 | Set-Content "$release/file-manifest.json" -Encoding utf8
$runtimeZip=Join-Path $OutputDirectory 'YanJi-v0.2.0-win-x64.zip'
[IO.Compression.ZipFile]::CreateFromDirectory($release,$runtimeZip,[IO.Compression.CompressionLevel]::Optimal,$true)
git -C $repo archive --format=zip --output="$OutputDirectory/YanJi-v0.2.0-source.zip" HEAD
if($LASTEXITCODE -ne 0){throw 'Source archive failed.'}
$extract=Join-Path $repo ('artifacts/v02-package-'+[Guid]::NewGuid().ToString('N'))
[IO.Compression.ZipFile]::ExtractToDirectory($runtimeZip,$extract)
$unpacked=Join-Path $extract 'YanJi-v0.2.0-win-x64'
foreach($file in $manifest){if((Get-FileHash -LiteralPath (Join-Path $unpacked $file.Path) -Algorithm SHA256).Hash -ne $file.Sha256){throw "Hash mismatch: $($file.Path)"}}
$env:DOTNET_ROOT=Join-Path $extract 'missing-shared-runtime'
$startups=@()
for($i=1;$i -le 3;$i++){
 $process=Start-Process -FilePath "$unpacked/YanJi.exe" -ArgumentList @('--data-dir',"$extract/data",'--startup-probe',"$extract/report-$i") -WindowStyle Hidden -PassThru
 Set-Content "$extract/pid-$i.txt" $process.Id
 if(!$process.WaitForExit(30000)){throw "Owned package startup still running PID $($process.Id), not terminated."}
 $startup=Get-Content "$extract/report-$i/startup.json" -Raw | ConvertFrom-Json
 if($process.ExitCode -ne 0 -or $startup.Status -ne 'PASS' -or $startup.RuntimeDirectory.TrimEnd('\') -ne $unpacked){throw 'Extracted offline self-contained startup failed.'}
 $startups+=$startup
}
@{Status='PASS';SourceCommit=$commit;VerifiedFileHashes=$manifest.Count;Startups=$startups;SharedRuntimeRequired=$false;PhysicalOffline='NOT TESTED'} | ConvertTo-Json -Depth 7 | Set-Content "$OutputDirectory/package-validation.json" -Encoding utf8
$evidence=Join-Path $repo ('artifacts/v02-evidence-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path "$evidence/screenshots","$evidence/desktop","$evidence/tests","$evidence/upgrade","$evidence/skills","$evidence/performance" | Out-Null
Copy-Item "$desktop/visual/*.png" "$evidence/screenshots"
Copy-Item "$desktop/visual/*.json" "$evidence/desktop"
Copy-Item "$repo/artifacts/v02-final-tests/v02-final.trx" "$evidence/tests"
Copy-Item "$upgrade/compatibility.json" "$evidence/upgrade"
Copy-Item "$repo/artifacts/v02-skills/*.json" "$evidence/skills"
Copy-Item "$performance/report/performance.json","$performance/report/results.json" "$evidence/performance"
Copy-Item "$repo/docs/V02-VALIDATION.md","$repo/docs/V02-DESIGN.md","$repo/docs/V02-SKILLS.md","$repo/docs/V02-REVIEW.md","$OutputDirectory/package-validation.json" $evidence
[IO.Compression.ZipFile]::CreateFromDirectory($evidence,"$OutputDirectory/YanJi-v0.2.0-evidence.zip")
$files=@(Get-ChildItem -LiteralPath $OutputDirectory -File | Where-Object Extension -eq '.zip' | Sort-Object Name | ForEach-Object { [pscustomobject]@{Name=$_.Name;Bytes=$_.Length;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash} })
@{Version='0.2.0';SourceCommit=$commit;WpfPass=$rows.Count;CorePass=70;SchemaMigration='none';NativeDpi=192;Package='PASS';ReleaseBytes=($manifest | Measure-Object Bytes -Sum).Sum;Files=$files} | ConvertTo-Json -Depth 7 | Set-Content "$OutputDirectory/delivery-manifest.json" -Encoding utf8
$files | ForEach-Object {"$($_.Sha256)  $($_.Name)"} | Set-Content "$OutputDirectory/sha256.txt" -Encoding utf8
Write-Output "PASS: $($manifest.Count) archive hashes; 3 extracted self-contained starts. $OutputDirectory"
