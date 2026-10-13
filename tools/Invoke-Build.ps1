param([Parameter(ValueFromRemainingArguments=$true)][string[]]$Arguments)
$ErrorActionPreference='Stop'
$taskRoot = if ($env:YANJI_LEGACY_TASK_ROOT) { $env:YANJI_LEGACY_TASK_ROOT } else { Split-Path $PSScriptRoot -Parent }
$sdkRoot=Get-Content "$taskRoot\work\phase1\toolchain\sdk-path.txt"
$env:DOTNET_ROOT=$sdkRoot
$env:DOTNET_HOST_PATH="$sdkRoot\dotnet.exe"
$env:DOTNET_CLI_HOME="$taskRoot\work\phase1\toolchain\cli-home"
$env:APPDATA="$env:DOTNET_CLI_HOME\AppData"
$env:NUGET_PACKAGES="$taskRoot\work\phase1\toolchain\nuget-packages"
$env:NUGET_HTTP_CACHE_PATH="$taskRoot\work\phase1\toolchain\nuget-http"
$env:NUGET_PLUGINS_CACHE_PATH="$taskRoot\work\phase1\toolchain\nuget-plugins"
$env:LOCALAPPDATA="$taskRoot\work\phase1\toolchain\localappdata"
$env:DOTNET_CLI_TELEMETRY_OPTOUT='1'
$env:DOTNET_CLI_USE_MSBUILD_SERVER='0'
$env:MSBUILDDISABLENODEREUSE='1'
& "$sdkRoot\dotnet.exe" @Arguments
exit $LASTEXITCODE
