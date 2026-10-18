param([Parameter(Mandatory=$true)][string]$Executable,[string]$RunName='native')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
$run=Join-Path $repo ('artifacts\runtime-location\'+$RunName)
if(Test-Path -LiteralPath $run){throw 'Preserve existing evidence; choose a new RunName.'}
$exe=(Resolve-Path -LiteralPath $Executable).Path
New-Item -ItemType Directory -Path $run | Out-Null
$data=Join-Path $run 'data'
foreach($phase in @('first','reopen')){
    $output=Join-Path $run $phase
    $arguments=@('--test-mode','--data-dir',('"'+$data+'"'),'--runtime-check',('"'+$output+'"'))
    $p=Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $repo -WindowStyle Hidden -PassThru
    if(!$p.WaitForExit(35000)){throw "Owned test process $($p.Id) exceeded bound; not terminated automatically."}
    if($p.ExitCode -ne 0){throw "Isolated process failed; inspect $output"}
    $result=Get-Content -LiteralPath (Join-Path $output 'result.json') -Raw | ConvertFrom-Json
    if($result.Status -ne 'PASS'){throw 'Native validation failed.'}
    $result | Select-Object Status,Reopened,RawMs,DataDirectory,Title
}
