param([string]$OutputDirectory='')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
if (-not $PSBoundParameters.ContainsKey('OutputDirectory')) { $OutputDirectory = Join-Path $repo 'artifacts/legacy-deliveries/v0.1.0' }
$release=Join-Path $repo 'releases\YanJi-v0.1.0-win-x64'
$run=Get-Content (Join-Path $repo 'artifacts\latest-desktop-validation.txt')
$crash=Get-Content (Join-Path $repo 'artifacts\latest-crash-run.txt')
if(!(Test-Path "$release\YanJi.exe") -or !(Test-Path "$repo\docs\VALIDATION.md")){throw 'Verified release and report must exist first.'}
if((git -C $repo status --porcelain).Length -gt 0){throw 'Commit source changes before packaging.'}
$commit=(git -C $repo rev-parse HEAD).Trim()
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$names=@('YanJi-v0.1.0-win-x64.zip','YanJi-v0.1.0-source.zip','YanJi-evidence.zip','YanJi-validation-report.md','YanJi-user-guide.md','delivery-manifest.json','package-validation.json','sha256.txt','YanJi-today.png')
foreach($name in $names){if(Test-Path (Join-Path $OutputDirectory $name)){throw "Preserve existing delivery file: $name"}}
Copy-Item "$repo\docs\USER-GUIDE.md" "$release\USER-GUIDE.md"
Copy-Item "$repo\docs\VALIDATION.md" "$release\VALIDATION.md"
Copy-Item "$repo\THIRD-PARTY-NOTICES.md" "$release\THIRD-PARTY-NOTICES.md"
@'
研迹 YanJi V0.1.0 · Windows 11 x64

完整解压本目录后，双击 YanJi.exe。不要只复制 exe。
无需安装 .NET SDK。所有记录默认保存在 %LOCALAPPDATA%\YanJi。
详细操作和完整备份恢复：USER-GUIDE.md。
实际测试与未测场景：VALIDATION.md。
活动计时点击 X 会隐藏到系统托盘；明确退出请选择应用内“退出应用”。
睡眠或异常退出后的不确定时间需要核对，不会自动补算。
当前为本机验证 V0.1；真实电源、原生输入法、断网和干净机尚未完整验收。
'@ | Set-Content "$release\开始使用.txt" -Encoding utf8
$files=@(Get-ChildItem -LiteralPath $release -Recurse -File | Where-Object Name -ne 'file-manifest.json' | Sort-Object FullName | ForEach-Object { [pscustomobject]@{Path=[IO.Path]::GetRelativePath($release,$_.FullName);Bytes=$_.Length;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash} })
@{Version='0.1.0';SourceCommit=$commit;Files=$files} | ConvertTo-Json -Depth 6 | Set-Content "$release\file-manifest.json" -Encoding utf8
$runtimeZip=Join-Path $OutputDirectory $names[0]
[IO.Compression.ZipFile]::CreateFromDirectory($release,$runtimeZip,[IO.Compression.CompressionLevel]::Optimal,$true)
$sourceZip=Join-Path $OutputDirectory $names[1]
git -C $repo archive --format=zip --output=$sourceZip HEAD
if($LASTEXITCODE -ne 0){throw 'Source archive failed.'}
$evidence=Join-Path $repo ('artifacts\evidence-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path "$evidence\logs","$evidence\tests","$evidence\desktop\smoke","$evidence\desktop\performance","$evidence\crash" | Out-Null
foreach($name in @('delivery-final-build.log','delivery-final-publish.log','delivery-final-tests.log','delivery-desktop.log','manifest-red.log','manifest-green.log')){Copy-Item "$repo\artifacts\$name" "$evidence\logs\$name"}
foreach($name in @('delivery-final.trx','manifest-red.trx','manifest-green.trx')){Copy-Item "$repo\artifacts\test-results\$name" "$evidence\tests\$name"}
Copy-Item "$repo\artifacts\validation-matrix.csv" "$evidence\validation-matrix.csv"
Copy-Item "$repo\docs\VALIDATION.md" "$evidence\VALIDATION.md"
Copy-Item "$run\smoke\*.json","$run\smoke\*.png" "$evidence\desktop\smoke"
Copy-Item "$run\startup-results.json" "$evidence\desktop"
for($i=1;$i -le 4;$i++){Copy-Item "$run\startup-$i\startup.json" "$evidence\desktop\startup-$i.json"}
Copy-Item "$run\performance\*.json" "$evidence\desktop\performance"
Copy-Item "$crash\results.json","$crash\restore-results.json","$crash\ready.json" "$evidence\crash"
Copy-Item "$crash\restore-before\ready.json" "$evidence\crash\restore-before-ready.json"
Copy-Item "$crash\restore-after\ready.json" "$evidence\crash\restore-after-ready.json"

# Exercise the delivered archive, using an entirely new owned data path.
$extract=Join-Path $repo ('artifacts\package-check-'+[Guid]::NewGuid().ToString('N'))
[IO.Compression.ZipFile]::ExtractToDirectory($runtimeZip,$extract)
$extractedRelease=Join-Path $extract 'YanJi-v0.1.0-win-x64'
$manifest=Get-Content "$extractedRelease\file-manifest.json" -Raw | ConvertFrom-Json
foreach($file in $manifest.Files){$path=Join-Path $extractedRelease $file.Path;if((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $file.Sha256){throw "Extracted hash mismatch: $($file.Path)"}}
$report=Join-Path $extract 'report'
$env:DOTNET_ROOT=Join-Path $extract 'missing-shared-runtime'
$process=Start-Process -FilePath "$extractedRelease\YanJi.exe" -ArgumentList @('--data-dir',"$extract\data",'--startup-probe',$report) -WindowStyle Hidden -PassThru
Set-Content "$extract\owned-pid.txt" $process.Id
$process.WaitForExit()
$startup=Get-Content "$report\startup.json" -Raw | ConvertFrom-Json
if($process.ExitCode -ne 0 -or $startup.Status -ne 'PASS' -or $startup.RuntimeDirectory.TrimEnd('\') -ne $extractedRelease){throw 'Extracted self-contained archive did not pass startup verification.'}
$packageResult=@{Status='PASS';VerifiedFileHashes=$manifest.Files.Count;ProcessId=$process.Id;ExitCode=$process.ExitCode;Startup=$startup;SourceCommit=$commit}
$packageResult | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $OutputDirectory 'package-validation.json') -Encoding utf8
Copy-Item (Join-Path $OutputDirectory 'package-validation.json') "$evidence\package-validation.json"
[IO.Compression.ZipFile]::CreateFromDirectory($evidence,(Join-Path $OutputDirectory 'YanJi-evidence.zip'))
Copy-Item "$repo\docs\VALIDATION.md" (Join-Path $OutputDirectory 'YanJi-validation-report.md')
Copy-Item "$repo\docs\USER-GUIDE.md" (Join-Path $OutputDirectory 'YanJi-user-guide.md')
Copy-Item "$run\smoke\page-0.png" (Join-Path $OutputDirectory 'YanJi-today.png')
$deliveries=@(Get-ChildItem -LiteralPath $OutputDirectory -File | Where-Object { $_.Name -in $names -and $_.Name -notin @('delivery-manifest.json','sha256.txt') } | ForEach-Object {[pscustomobject]@{Name=$_.Name;Bytes=$_.Length;Sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash}})
$releaseFiles=@(Get-ChildItem -LiteralPath $release -Recurse -File)
@{Version='0.1.0';SourceCommit=$commit;SourceDirectory=$repo;Executable="$release\YanJi.exe";ReleaseDirectoryBytes=($releaseFiles | Measure-Object Length -Sum).Sum;ReleaseFileCount=$releaseFiles.Count;UnitTestsPassed=44;DesktopChecksPassed=25;OriginalMatrix=@{PASS=27;FAIL=0;NOT_TESTED=14};PackageCheck='PASS';FormalReleaseAcceptance='CONDITIONAL: real power, IME, offline, clean-machine and manual flows unverified';Artifacts=$deliveries} | ConvertTo-Json -Depth 7 | Set-Content (Join-Path $OutputDirectory 'delivery-manifest.json') -Encoding utf8
$deliveries | ForEach-Object {"$($_.Sha256)  $($_.Name)"} | Set-Content (Join-Path $OutputDirectory 'sha256.txt') -Encoding utf8
Write-Output "Delivered: $OutputDirectory"
Write-Output "Source commit: $commit"
Write-Output "Release bytes: $(($releaseFiles | Measure-Object Length -Sum).Sum)"
$deliveries | Select-Object Name,Bytes
