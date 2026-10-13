param([string]$Executable)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
if(!$Executable){$Executable=Join-Path $repo 'releases\YanJi-v0.2.0-win-x64\YanJi.exe'}
if(!(Test-Path -LiteralPath $Executable)){throw 'Build V0.2 release first.'}
$runRoot=Join-Path $repo ('artifacts\v02-desktop-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runRoot | Out-Null
Set-Content "$repo\artifacts\latest-v02-desktop.txt" $runRoot
$process=Start-Process -FilePath $Executable -ArgumentList @('--data-dir',"$runRoot\data",'--v02-visual',"$runRoot\visual") -WindowStyle Hidden -PassThru
Set-Content "$runRoot\pid.txt" $process.Id
if(!$process.WaitForExit(180000)){throw "Owned validation still running: PID $($process.Id). No process terminated."}
if(Test-Path "$runRoot\visual\results.json"){
 $rows=Get-Content "$runRoot\visual\results.json" -Raw | ConvertFrom-Json
 Write-Output "PASS $(@($rows | Where-Object Status -eq 'PASS').Count); FAIL $(@($rows | Where-Object Status -eq 'FAIL').Count)"
}
Write-Output "Evidence: $runRoot"
if($process.ExitCode -ne 0){throw "Owned validation exited $($process.ExitCode). Inspect saved evidence."}
