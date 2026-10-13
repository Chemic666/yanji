param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Arguments)
$ErrorActionPreference='Stop'
$taskRoot = if ($env:YANJI_LEGACY_TASK_ROOT) { $env:YANJI_LEGACY_TASK_ROOT } else { Split-Path $PSScriptRoot -Parent }
$sdkFile="$taskRoot\work\phase1\toolchain\sdk-path.txt"
if($env:YANJI_DOTNET_ROOT){$sdkRoot=$env:YANJI_DOTNET_ROOT}
elseif(Test-Path $sdkFile){$sdkRoot=Get-Content $sdkFile}
else{$sdkRoot=Split-Path (Get-Command dotnet -ErrorAction Stop).Source}
$cacheRoot=if(Test-Path $sdkFile){"$taskRoot\work\phase1\toolchain"}else{Join-Path (Split-Path $PSScriptRoot) '.tools'}
$env:DOTNET_ROOT=$sdkRoot
$env:DOTNET_HOST_PATH="$sdkRoot\dotnet.exe"
$env:DOTNET_CLI_HOME="$cacheRoot\cli-home"
$env:APPDATA="$env:DOTNET_CLI_HOME\AppData"
$env:NUGET_PACKAGES="$cacheRoot\nuget-packages"
$env:NUGET_HTTP_CACHE_PATH="$cacheRoot\nuget-http"
$env:NUGET_PLUGINS_CACHE_PATH="$cacheRoot\nuget-plugins"
$env:LOCALAPPDATA="$cacheRoot\localappdata"
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_CLI_USE_MSBUILD_SERVER='0'
$env:MSBUILDDISABLENODEREUSE='1'
& "$sdkRoot\dotnet.exe" @Arguments
exit $LASTEXITCODE
