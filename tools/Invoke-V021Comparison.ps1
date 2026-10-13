param([Parameter(Mandatory)][string]$Executable,[Parameter(Mandatory)][string]$Phase,[string]$Fixture='artifacts/v021-implementation/fixture.json')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot
if($Phase -notmatch '^[a-zA-Z0-9-]+$'){throw 'Invalid phase name.'}
$root=Join-Path $repo "artifacts/v021-implementation/$Phase"
$data=Join-Path $repo "artifacts/v021-implementation/$Phase-db"
if(Test-Path $root){throw 'Preserve earlier capture: phase already exists.'}
if(Test-Path $data){throw 'Fresh isolated data directory required.'}
$fixturePath=(Resolve-Path $Fixture).Path
$exe=(Resolve-Path $Executable).Path
New-Item -ItemType Directory $root | Out-Null
$source=@('MainWindow.xaml','Styles.xaml','StudyChart.cs','MainWindow.Presentation.cs')|ForEach-Object{[pscustomobject]@{File=$_;Sha256=(Get-FileHash (Join-Path $repo "src/YanJi.App/$_")).Hash}}
@{FixtureSha256=(Get-FileHash $fixturePath).Hash;ExecutableSha256=(Get-FileHash $exe).Hash;AssemblySha256=(Get-FileHash (Join-Path (Split-Path $exe) 'YanJi.dll')).Hash;Source=$source;Head=(git rev-parse HEAD)} |ConvertTo-Json -Depth 5|Set-Content "$root/provenance.json"
$p=Start-Process -FilePath $exe -ArgumentList @('--data-dir',$data,'--v021-capture',$root,'--v021-fixture',$fixturePath) -WindowStyle Hidden -PassThru
$p.Id|Set-Content "$root/pid.txt"
if(!$p.WaitForExit(55000)){throw "Owned capture still running PID $($p.Id); evidence retained."}
if($p.ExitCode -ne 0){throw 'Capture failed; inspect failure.txt/startup-failure.txt.'}
Get-Content "$root/result.json"
Get-ChildItem $root -Filter '*-native.png'|ForEach-Object{[pscustomobject]@{File=$_.Name;Sha256=(Get-FileHash $_.FullName).Hash;Bytes=$_.Length}}|ConvertTo-Json|Set-Content "$root/image-hashes.json"
