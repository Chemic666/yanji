param([switch]$Empty,[string]$Executable,[string]$EvidenceName='render')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
$run=Join-Path $repo ('artifacts/v02-'+$EvidenceName+'-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $run | Out-Null
Set-Content (Join-Path $repo ('artifacts/latest-v02-'+$EvidenceName+'.txt')) $run
if($Executable){$file=[IO.Path]::GetFullPath($Executable);$arguments=@()}
else{$sdk=Get-Content (Join-Path $(if ($env:YANJI_TOOLCHAIN_ROOT) { $env:YANJI_TOOLCHAIN_ROOT } else { Join-Path $repo '.tools' }) 'sdk-path.txt');$file=Join-Path $sdk 'dotnet.exe';$arguments=@(('"'+(Join-Path $repo 'design/YanJi.DesignLab/bin/Release/net10.0-windows/YanJi.DesignLab.dll')+'"'))}
$arguments+=@('--data-dir',('"'+(Join-Path $run 'data')+'"'),'--capture-dir',('"'+(Join-Path $run 'screenshots')+'"'))
if($Empty){$arguments+='--empty'}
$process=Start-Process -FilePath $file -ArgumentList $arguments -WindowStyle Hidden -PassThru
Set-Content (Join-Path $run 'owned-pid.txt') $process.Id
if(!$process.WaitForExit(30000)){Write-Output "Own sample remains running PID $($process.Id). No termination. Evidence: $run";exit 2}
@{ExitCode=$process.ExitCode;ProcessId=$process.Id;Run=$run} | ConvertTo-Json | Set-Content (Join-Path $run 'execution.json')
Write-Output "Evidence: $run; ExitCode: $($process.ExitCode)"
if($process.ExitCode -ne 0){Get-Content (Join-Path $run 'data/visual-error.txt') -ErrorAction SilentlyContinue}
exit $process.ExitCode
