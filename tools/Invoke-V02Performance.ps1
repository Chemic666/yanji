$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
$run=Join-Path $repo ('artifacts/v02-performance-final-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $run | Out-Null
Set-Content "$repo/artifacts/latest-v02-final-performance.txt" $run
$process=Start-Process -FilePath "$repo/releases/YanJi-v0.2.0-win-x64/YanJi.exe" -ArgumentList @('--data-dir',"$run/data",'--performance',"$run/report") -WindowStyle Hidden -PassThru
Set-Content "$run/pid.txt" $process.Id
if(!$process.WaitForExit(330000)){throw "Owned performance still running PID $($process.Id); not terminated."}
$samples=Get-Content "$run/report/performance.json" -Raw | ConvertFrom-Json
if($process.ExitCode -ne 0 -or $samples.Count -ne 4 -or @($samples | Where-Object Status -ne 'PASS').Count){throw 'Performance sample did not complete.'}
Write-Output "PASS: four actual samples, default rendering; $run"
