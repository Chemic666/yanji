param([switch]$Performance)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
$executable=Join-Path $repo 'releases\YanJi-v0.1.1-win-x64\YanJi.exe'
if(!(Test-Path -LiteralPath $executable)){throw 'V0.1.1 release is required.'}
$runRoot=Join-Path $repo ('artifacts\v011-desktop-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runRoot | Out-Null
Set-Content "$repo\artifacts\latest-v011-desktop.txt" $runRoot
$env:DOTNET_ROOT=Join-Path $runRoot 'no-shared-runtime'
function Invoke-Owned([string[]]$Arguments,[string]$Marker,[int]$Timeout=120000){
    $process=Start-Process -FilePath $executable -ArgumentList $Arguments -WindowStyle Hidden -PassThru
    Set-Content $Marker $process.Id
    if(!$process.WaitForExit($Timeout)){throw "Owned validation still running; inspect PID $($process.Id), marker $Marker. No process was terminated."}
    if($process.ExitCode -ne 0){throw "Owned validation exited $($process.ExitCode). Marker: $Marker"}
}
function Assert-Results([string]$Report){
    $rows=Get-Content "$Report\results.json" -Raw | ConvertFrom-Json
    if(!$rows.Count -or @($rows | Where-Object Status -ne 'PASS').Count){throw "Actual GUI checks failed: $Report"}
    Write-Output "$($rows.Count) PASS: $Report"
}
Invoke-Owned @('--data-dir',"$runRoot\smoke-data",'--smoke',"$runRoot\smoke") "$runRoot\smoke-pid.txt"
Assert-Results "$runRoot\smoke"
$startup=@()
for($i=1;$i -le 4;$i++){
    $report="$runRoot\startup-$i";$clock=[Diagnostics.Stopwatch]::StartNew()
    Invoke-Owned @('--data-dir',"$runRoot\startup-data",'--startup-probe',$report) "$runRoot\startup-$i-pid.txt"
    $item=Get-Content "$report\startup.json" -Raw | ConvertFrom-Json
    if($item.Status -ne 'PASS' -or $item.RuntimeDirectory.TrimEnd('\') -ne (Split-Path $executable)){throw 'Self-contained startup evidence mismatch.'}
    $startup+=@{Run=$i;UiReadyMs=$item.UiReadyMs;WholeProcessMs=$clock.Elapsed.TotalMilliseconds;RuntimeDirectory=$item.RuntimeDirectory;Status=$item.Status}
}
$startup | ConvertTo-Json -Depth 5 | Set-Content "$runRoot\startup-results.json"
if($Performance){Invoke-Owned @('--data-dir',"$runRoot\performance-data",'--performance',"$runRoot\performance") "$runRoot\performance-pid.txt" 360000;Assert-Results "$runRoot\performance"}
Write-Output "Evidence: $runRoot"
