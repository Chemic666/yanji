param([string]$Executable)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
if(!$Executable){$Executable=Join-Path $repo 'releases/YanJi-v0.2.0-win-x64/YanJi.exe'}
$old=Get-Content "$repo/artifacts/latest-v011-desktop.txt"
$oldPid=[int](Get-Content "$old/smoke-pid.txt")
if(Get-Process -Id $oldPid -ErrorAction SilentlyContinue){throw 'Old fixture must be closed before copying.'}
if(Test-Path "$old/smoke-data/yanji.sqlite-wal"){throw 'Checkpoint old fixture before copying, never omit WAL.'}
$run=Join-Path $repo ('artifacts/v02-upgrade-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory "$run/data" | Out-Null
Set-Content "$run/owned-v02-upgrade.marker" 'Owned disposable old-binary fixture; no real user database.'
Copy-Item -LiteralPath "$old/smoke-data/yanji.sqlite" -Destination "$run/data/yanji.sqlite"
Copy-Item -LiteralPath "$old/smoke/v011-upgrade.yanjibak" -Destination "$run/v011-full.yanjibak"
& "$repo/tools/Invoke-Build.ps1" -Arguments @('run','--project','tools/YanJi.Probe','--','--v02-compat-capture',$run)
if($LASTEXITCODE -ne 0){throw 'Capture failed.'}
$process=Start-Process -FilePath $Executable -ArgumentList @('--data-dir',"$run/data",'--startup-probe',"$run/startup") -WindowStyle Hidden -PassThru
Set-Content "$run/pid.txt" $process.Id
if(!$process.WaitForExit(30000)){throw "Owned upgrade still running PID $($process.Id). No termination."}
if($process.ExitCode -ne 0){throw 'New GUI failed to open old fixture.'}
& "$repo/tools/Invoke-Build.ps1" -Arguments @('run','--project','tools/YanJi.Probe','--','--v02-compat-verify',$run)
if($LASTEXITCODE -ne 0){throw 'Compatibility failed; preserve evidence.'}
Set-Content "$repo/artifacts/latest-v02-upgrade.txt" $run
Write-Output "Evidence: $run"
