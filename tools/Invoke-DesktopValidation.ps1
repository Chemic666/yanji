param([Parameter(Mandatory=$true)][string]$Executable,[switch]$Performance)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path $PSScriptRoot
$runRoot=Join-Path $repoRoot ('artifacts\desktop-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runRoot | Out-Null
Set-Content (Join-Path $repoRoot 'artifacts\latest-desktop-validation.txt') $runRoot
$env:DOTNET_ROOT=Join-Path $runRoot 'no-shared-runtime'
function Invoke-OwnedApp([string[]]$AppArguments,[string]$Marker){
    $process=Start-Process -FilePath $Executable -ArgumentList $AppArguments -PassThru -WindowStyle Hidden
    Set-Content $Marker $process.Id
    $process.WaitForExit()
    if($process.ExitCode -ne 0){throw "Owned application exited with code $($process.ExitCode); PID $($process.Id)."}
}
function Assert-Results([string]$Report){
    $rows=Get-Content (Join-Path $Report 'results.json') -Raw | ConvertFrom-Json
    if(@($rows | Where-Object Status -ne 'PASS').Count){throw "Desktop validation failed: $Report"}
    $rows | Select-Object Name,Status
}
$smoke=Join-Path $runRoot 'smoke'
Invoke-OwnedApp @('--data-dir',(Join-Path $runRoot 'smoke-data'),'--smoke',$smoke) (Join-Path $runRoot 'smoke-pid.txt')
Assert-Results $smoke
$startup=@()
for($i=1;$i -le 4;$i++){
    $report=Join-Path $runRoot "startup-$i"
    $clock=[Diagnostics.Stopwatch]::StartNew()
    Invoke-OwnedApp @('--data-dir',(Join-Path $runRoot 'startup-data'),'--startup-probe',$report) (Join-Path $runRoot "startup-$i-pid.txt")
    $item=Get-Content (Join-Path $report 'startup.json') -Raw | ConvertFrom-Json
    if($item.Status -ne 'PASS'){throw 'Startup probe failed.'}
    $startup+=@{Run=$i;UiReadyMs=$item.UiReadyMs;WholeProcessMs=$clock.Elapsed.TotalMilliseconds;RuntimeDirectory=$item.RuntimeDirectory;Status=$item.Status}
}
$startup | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $runRoot 'startup-results.json')
if($Performance){
    $report=Join-Path $runRoot 'performance'
    Invoke-OwnedApp @('--data-dir',(Join-Path $runRoot 'performance-data'),'--performance',$report) (Join-Path $runRoot 'performance-pid.txt')
    Assert-Results $report
}
Write-Output "Evidence: $runRoot"
