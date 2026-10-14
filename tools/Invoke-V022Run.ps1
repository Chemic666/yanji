param([Parameter(Mandatory)][string]$Exe,[Parameter(Mandatory)][string]$Data,[Parameter(Mandatory)][string]$Output,[string]$Mode='--v022-capture')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$taskTools = if ($env:YANJI_TOOLCHAIN_ROOT) { $env:YANJI_TOOLCHAIN_ROOT } else { Join-Path (Split-Path $PSScriptRoot -Parent) '.tools' }
$env:DOTNET_ROOT=(Get-Content "$taskTools\sdk-path.txt" -Raw).Trim()
$env:DOTNET_HOST_PATH=Join-Path $env:DOTNET_ROOT 'dotnet.exe'
$env:DOTNET_CLI_HOME=Join-Path $root 'artifacts\v022\cli-home'
$env:LOCALAPPDATA=Join-Path $root 'artifacts\v022\localappdata'
$exePath=(Resolve-Path $Exe).Path
$dataPath=[IO.Path]::GetFullPath((Join-Path $root $Data))
$outputPath=[IO.Path]::GetFullPath((Join-Path $root $Output))
if(!$dataPath.StartsWith("$root\artifacts\",[StringComparison]::OrdinalIgnoreCase) -or !$outputPath.StartsWith("$root\artifacts\",[StringComparison]::OrdinalIgnoreCase)){throw 'Owned artifacts paths required'}
if((Test-Path $dataPath) -or (Test-Path $outputPath)){throw 'Fresh data and output required'}
$p=Start-Process -FilePath $exePath -ArgumentList @('--data-dir', $dataPath, $Mode, $outputPath) -WorkingDirectory $root -WindowStyle Hidden -PassThru
$record=[pscustomobject]@{PID=$p.Id;Exe=$exePath;Data=$dataPath;Output=$outputPath;Mode=$Mode;Started=[DateTimeOffset]::Now.ToString('o')}
$record | ConvertTo-Json | Set-Content (Join-Path $root "artifacts/v022/process-$($p.Id).json")
$record | ConvertTo-Json
