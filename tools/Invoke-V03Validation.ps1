param([Parameter(Mandatory)][string]$Exe,[Parameter(Mandatory)][string]$Data,[Parameter(Mandatory)][string]$Output,[ValidateSet('layout','capture','reopen')][string]$Mode='layout')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$dataPath=[IO.Path]::GetFullPath((Join-Path $root $Data));$outputPath=[IO.Path]::GetFullPath((Join-Path $root $Output));$owned=Join-Path $root 'artifacts/'
if(!$dataPath.StartsWith($owned,[StringComparison]::OrdinalIgnoreCase)-or !$outputPath.StartsWith($owned,[StringComparison]::OrdinalIgnoreCase)){throw 'Owned project artifacts required'}
if(Test-Path $outputPath){throw 'Fresh output required'}
if($Mode -eq 'reopen'){if(!(Test-Path (Join-Path $dataPath 'v03-isolated.fixture'))){throw 'Marked synthetic data required'}}elseif(Test-Path $dataPath){throw 'Fresh data required'}
$env:LOCALAPPDATA=Join-Path $root 'artifacts/v03/localappdata'
$process=Start-Process -FilePath (Resolve-Path $Exe).Path -ArgumentList @('--data-dir',$dataPath,('--v03-'+$Mode),$outputPath) -WorkingDirectory $root -WindowStyle Hidden -PassThru
$record=[pscustomobject]@{PID=$process.Id;Exe=(Resolve-Path $Exe).Path;Data=$dataPath;Output=$outputPath;Mode=$Mode;Started=[DateTimeOffset]::UtcNow.ToString('o')}
$record|ConvertTo-Json|Set-Content (Join-Path $root "artifacts/v03/process-$($process.Id).json")
$record|ConvertTo-Json
