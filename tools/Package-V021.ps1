param([string]$OutputDirectory='D:\Projects\yanji\releases\Review\v0.2.1')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
$release=Join-Path $repo 'releases/YanJi-v0.2.1-win-x64'
$commit=(git -C $repo rev-parse HEAD).Trim()
if((git -C $repo status --porcelain).Length -gt 0){throw 'Commit verified source first.'}
if((Get-Item "$release/YanJi.exe").VersionInfo.ProductVersion -ne "0.2.1+$commit"){throw 'Published identity must match clean delivery commit.'}
$native=Get-Content "$repo/artifacts/v021-implementation/final-native-path.txt"
$rows=Get-Content "$native/visual/results.json" -Raw|ConvertFrom-Json
if($rows.Count -lt 122 -or @($rows|Where-Object Status -ne 'PASS').Count){throw 'Native release regression incomplete.'}
$perf=Get-Content "$repo/artifacts/v021-performance/report/performance.json" -Raw|ConvertFrom-Json
if($perf.Count -ne 4 -or @($perf|Where-Object Status -ne 'PASS').Count){throw 'Performance sampling incomplete.'}
New-Item -ItemType Directory $OutputDirectory -Force|Out-Null
foreach($suffix in @('win-x64','source','evidence','Review')){if(Test-Path "$OutputDirectory/YanJi-v0.2.1-$suffix.zip"){throw 'Preserve existing delivery ZIP.'}}
foreach($doc in @('V021-DESIGN.md','V021-VALIDATION.md','V021-DELIVERY.md','V021-SKILL-USAGE.md','V02-USER-GUIDE.md')){Copy-Item "$repo/docs/$doc" $release}
Copy-Item "$repo/THIRD-PARTY-NOTICES.md" $release
$licenseSource="$repo/releases/YanJi-v0.2.0-win-x64/licenses"
if(!(Test-Path "$release/licenses")){Copy-Item $licenseSource "$release/licenses" -Recurse}
else{foreach($license in Get-ChildItem $licenseSource -Recurse -File){$relative=[IO.Path]::GetRelativePath($licenseSource,$license.FullName);if((Get-FileHash (Join-Path "$release/licenses" $relative)).Hash -ne (Get-FileHash $license.FullName).Hash){throw 'Preserved license hash mismatch.'}}}
@'
研迹 V0.2.1 · Windows x64 自包含
完整解压后双击 YanJi.exe；无需安装 SDK。
升级前导出并校验完整备份，明确退出旧版。默认数据 %LOCALAPPDATA%\YanJi。
限制和验证见 V021-VALIDATION.md，交付身份见 file-manifest.json。
'@|Set-Content "$release/开始使用.txt"
$files=@(Get-ChildItem $release -Recurse -File|Where-Object Name -ne 'file-manifest.json'|ForEach-Object{[pscustomobject]@{Path=[IO.Path]::GetRelativePath($release,$_.FullName);Bytes=$_.Length;Sha256=(Get-FileHash $_.FullName).Hash}})
if(@($files|Where-Object Path -match '\.(db|sqlite|yanjibak)$').Count){throw 'Runtime package contains data.'}
@{Version='0.2.1';SourceCommit=$commit;Files=$files}|ConvertTo-Json -Depth 6|Set-Content "$release/file-manifest.json"
$zip="$OutputDirectory/YanJi-v0.2.1-win-x64.zip"
[IO.Compression.ZipFile]::CreateFromDirectory($release,$zip,[IO.Compression.CompressionLevel]::Optimal,$true)
git -C $repo archive --format=zip --output="$OutputDirectory/YanJi-v0.2.1-source.zip" HEAD
if($LASTEXITCODE -ne 0){throw 'Source archive failed.'}
$extract="$repo/artifacts/v021-package-extracted-$($commit.Substring(0,8))"
if(Test-Path $extract){throw 'Fresh package extraction required.'}
[IO.Compression.ZipFile]::ExtractToDirectory($zip,$extract)
$unpacked="$extract/YanJi-v0.2.1-win-x64"
foreach($file in $files){if((Get-FileHash (Join-Path $unpacked $file.Path)).Hash -ne $file.Sha256){throw "Hash mismatch: $($file.Path)"}}
$env:DOTNET_ROOT="$extract/missing-shared-runtime";$env:DOTNET_ROOT_X64=$env:DOTNET_ROOT
$startups=@()
for($i=1;$i -le 3;$i++){
 $p=Start-Process -FilePath "$unpacked/YanJi.exe" -ArgumentList @('--data-dir',"$extract/data",'--startup-probe',"$extract/report-$i") -WindowStyle Hidden -PassThru
 $p.Id|Set-Content "$extract/pid-$i.txt"
 if(!$p.WaitForExit(30000)){throw "Owned startup still running PID $($p.Id)"}
 $s=Get-Content "$extract/report-$i/startup.json" -Raw|ConvertFrom-Json
 if($p.ExitCode -ne 0 -or $s.Status -ne 'PASS' -or [IO.Path]::GetFullPath($s.RuntimeDirectory).TrimEnd('\') -ne [IO.Path]::GetFullPath($unpacked).TrimEnd('\')){throw 'Extracted self-contained startup failed.'}
 $startups+=$s
}
@{Status='PASS';SourceCommit=$commit;ProductVersion=(Get-Item "$unpacked/YanJi.exe").VersionInfo.ProductVersion;VerifiedFiles=$files.Count;Startups=$startups;SharedRuntimeRequired=$false;PhysicalOffline='NOT TESTED'}|ConvertTo-Json -Depth 7|Set-Content "$OutputDirectory/package-validation.json"
$review="$repo/artifacts/v021-review"
if(!(Test-Path "$review/comparison.html")){throw 'Complete comparison review required.'}
[IO.Compression.ZipFile]::CreateFromDirectory($review,"$OutputDirectory/YanJi-v0.2.1-Review.zip")
$evidence="$repo/artifacts/v021-delivery-evidence"
New-Item -ItemType Directory $evidence|Out-Null
Copy-Item "$repo/artifacts/v021-implementation" "$evidence/implementation" -Recurse
Copy-Item "$repo/artifacts/v021-tests" "$evidence/tests" -Recurse
Copy-Item "$repo/artifacts/v021-performance" "$evidence/performance" -Recurse
Copy-Item $native "$evidence/native" -Recurse
$upgrade=Get-Content "$repo/artifacts/v021-implementation/compatibility-path.txt"
Copy-Item $upgrade "$evidence/upgrade" -Recurse
Copy-Item "$repo/artifacts/v021-completion.json" $evidence
Copy-Item "$OutputDirectory/package-validation.json" $evidence
foreach($log in Get-ChildItem $evidence -Recurse -File|Where-Object Extension -in @('.txt','.log')){
 if($log.Name -match '^context'){
  @('Sanitized engine execution excerpt: context --target src/YanJi.App/MainWindow.xaml; exit 0.','PRODUCT.md and DESIGN.md loaded. WPF platform/XAML unsupported; not a native engine PASS.','Complete raw CLI output retained locally, excluded from distribution.')|Set-Content -LiteralPath $log.FullName
 }else{
  $lines=Get-Content -LiteralPath $log.FullName
  $safe=@('Sanitized distribution copy; complete raw diagnostic retained locally.')+@($lines|ForEach-Object{
   if($_ -match '(?i)(authorization|bearer\s|access_token|refresh_token|id_token|client_secret|api_key|set-cookie|cookie\s*[:=])'){'[REDACTED credential-bearing line]'}
   else{$_ -replace 'C:\\Users\\[^\\\s]+\\Documents\\Codex\\[^\s"<>]*','<cached-toolchain-path>'}
  })
  $safe|Set-Content -LiteralPath $log.FullName
 }
}
'Only isolated synthetic data; no real user records. Before/after and failed attempts retained.'|Set-Content "$evidence/SYNTHETIC.txt"
[IO.Compression.ZipFile]::CreateFromDirectory($evidence,"$OutputDirectory/YanJi-v0.2.1-evidence.zip")
$packages=@(Get-ChildItem $OutputDirectory -Filter '*.zip'|ForEach-Object{[pscustomobject]@{Name=$_.Name;Path=$_.FullName;Bytes=$_.Length;Sha256=(Get-FileHash $_.FullName).Hash}})
@{Version='0.2.1';Commit=$commit;Branch=(git branch --show-current);Packages=$packages}|ConvertTo-Json -Depth 6|Set-Content "$OutputDirectory/delivery-manifest.json"
$packages|ForEach-Object{"$($_.Sha256)  $($_.Name)"}|Set-Content "$OutputDirectory/sha256.txt"
$completion=Get-Content "$repo/artifacts/v021-completion.json" -Raw|ConvertFrom-Json
$completion.status='complete';$completion.commit=$commit;$completion.packages=$packages
$completion|Add-Member -NotePropertyName packageValidation -NotePropertyValue "$OutputDirectory/package-validation.json" -Force
$completion|Add-Member -NotePropertyName deliveryManifest -NotePropertyValue "$OutputDirectory/delivery-manifest.json" -Force
$completion|ConvertTo-Json -Depth 14|Set-Content "$repo/artifacts/v021-completion.json"
$packages|Format-Table Name,Bytes
