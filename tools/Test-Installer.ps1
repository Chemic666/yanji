param(
    [Parameter(Mandatory=$true)][string]$Setup,
    [Parameter(Mandatory=$true)][string]$Payload,
    [Parameter(Mandatory=$true)][string]$TestRoot,
    [string]$PriorSetup=''
)
$ErrorActionPreference = 'Stop'
$Setup = [IO.Path]::GetFullPath($Setup)
$Payload = [IO.Path]::GetFullPath($Payload)
$TestRoot = [IO.Path]::GetFullPath($TestRoot)
$repo = Split-Path $PSScriptRoot
$ownedBase = [IO.Path]::GetFullPath((Join-Path $repo 'artifacts')) + '\'
if (-not $TestRoot.StartsWith($ownedBase,[StringComparison]::OrdinalIgnoreCase)) { throw 'TestRoot must be a new owned repository artifacts directory.' }
if (Test-Path -LiteralPath $TestRoot) { throw 'TestRoot must be new. Preserve previous evidence.' }
$local = [Environment]::GetFolderPath('LocalApplicationData')
$install = Join-Path $local 'Programs\YanJi'
$formal = Join-Path $local 'YanJi' # String only. NEVER inspect this path.
$registry = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{BF9A7AA8-88EF-4470-A6D4-7A4E531E77B3}_is1'
$menu = Join-Path ([Environment]::GetFolderPath('Programs')) '研迹 YanJi'
$desktop = Join-Path ([Environment]::GetFolderPath('Desktop')) '研迹 YanJi.lnk'
foreach ($path in @($install,$registry,$menu,$desktop,'HKCU:\Software\YanJi\Installer')) {
    if (Test-Path -LiteralPath $path) { throw 'Existing installation/shortcut/registration detected. Stop without modifying it.' }
}
if (Get-Process -Name YanJi -ErrorAction SilentlyContinue) { throw 'Existing YanJi process detected. Stop; never close user processes.' }
New-Item -ItemType Directory -Path $TestRoot | Out-Null
$checks = [Collections.Generic.List[object]]::new()
$failure = $null
$script:sequence = 0
function Check([bool]$Condition,[string]$Name) {
    $checks.Add([pscustomobject]@{Name=$Name;Status=$(if($Condition){'PASS'}else{'FAIL'})})
    if (-not $Condition) { throw $Name }
}
function Run-Installer([string[]]$Extra=@(),[switch]$Uninstall,[string]$InstallerOverride='') {
    $script:sequence++
    $file = if ($Uninstall) { Join-Path $install 'unins000.exe' } else { $Setup }
    if ($InstallerOverride) { $file = [IO.Path]::GetFullPath($InstallerOverride) }
    $log = Join-Path $TestRoot ('operation-{0:00}.log' -f $script:sequence)
    $arguments = @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-',('/LOG="'+$log+'"')) + $Extra
    $process = Start-Process -FilePath $file -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(45000)) { throw 'Installer operation exceeded 45 seconds; inspect owned process before continuing.' }
    $process.Refresh()
    if ($Uninstall -and $process.ExitCode -eq 0) {
        # Inno's temporary self-delete helper can outlive the original process.
        $deadline = [DateTime]::UtcNow.AddSeconds(10)
        while ((Test-Path -LiteralPath $file) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 100 }
        if (Test-Path -LiteralPath $file) { throw 'Owned uninstaller self-cleanup did not finish within 10 seconds.' }
    }
    return $process.ExitCode
}
function Probe([string]$Name) {
    $data = Join-Path $TestRoot 'learning-test-profile'
    $report = Join-Path $TestRoot $Name
    $process = Start-Process -FilePath (Join-Path $install 'YanJi.exe') -ArgumentList @('--test-mode','--data-dir',('"'+$data+'"'),'--startup-probe',('"'+$report+'"')) -WindowStyle Hidden -PassThru
    if (-not $process.WaitForExit(30000)) { throw 'Isolated startup probe exceeded 30 seconds.' }
    $process.Refresh()
    Check ($process.ExitCode -eq 0) "$Name process exit"
    $result = Get-Content -LiteralPath (Join-Path $report 'startup.json') -Raw | ConvertFrom-Json
    Check ($result.Status -eq 'PASS' -and $result.DataDirectory -eq $data) "$Name explicit isolated database"
    Check ($result.RuntimeDirectory.TrimEnd('\') -eq $install) "$Name bundled runtime (no SDK/shared runtime)"
    $results = @(Get-Content -LiteralPath (Join-Path $report 'results.json') -Raw | ConvertFrom-Json)
    Check (@($results | Where-Object Status -eq 'FAIL').Count -eq 0) "$Name startup/SQLite assertions"
}
function Check-Payload {
    $manifest = Get-Content -LiteralPath (Join-Path $Payload 'manifest-sha256.json') -Raw | ConvertFrom-Json
    foreach ($row in $manifest.Files.PSObject.Properties) {
        $path = Join-Path $install $row.Name
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $row.Value.Sha256) { throw 'Installed payload hash mismatch: ' + $row.Name }
    }
    Check $true ('Installed payload exact hashes ('+@($manifest.Files.PSObject.Properties).Count+' entries)')
}
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class InstallerTestWindow {
    [DllImport("user32.dll")] public static extern IntPtr GetLastActivePopup(IntPtr window);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr window, uint msg, IntPtr w, IntPtr l);
}
'@
function Close-OwnedIdleApp($Process,[string]$Data) {
    $windowId = Get-Content -LiteralPath (Join-Path $Data 'window.id')
    if ([int]$windowId[1] -ne $Process.Id) { throw 'Test window process identity mismatch.' }
    $main = [IntPtr]::new([long]$windowId[0])
    $popup = [InstallerTestWindow]::GetLastActivePopup($main)
    $owned = [uint32]0
    [InstallerTestWindow]::GetWindowThreadProcessId($popup,[ref]$owned) | Out-Null
    if ($owned -ne $Process.Id) { throw 'Test popup process identity mismatch.' }
    # A fresh ordinary start shows the first-use profile dialog. Cancel that owned
    # modal before asking the idle main window to close; never terminate processes.
    if ($popup -ne $main) {
        [InstallerTestWindow]::PostMessage($popup,0x10,[IntPtr]::Zero,[IntPtr]::Zero) | Out-Null
        Start-Sleep -Milliseconds 400
    }
    [InstallerTestWindow]::PostMessage($main,0x10,[IntPtr]::Zero,[IntPtr]::Zero) | Out-Null
    if (-not $Process.WaitForExit(10000)) { throw 'Owned idle app did not close normally.' }
}
try {
    # Every rejected path is passed to Setup as a string; no formal data reads.
    $badPaths = @($formal,($formal+'\child'),$local,$TestRoot,'relative-path','\\server\share\YanJi','\\?\C:\YanJi',(Join-Path $TestRoot 'file.txt'))
    Set-Content -LiteralPath (Join-Path $TestRoot 'file.txt') -Value 'synthetic sentinel only'
    foreach ($bad in $badPaths) {
        $exitCode = Run-Installer -Extra @(('/DIR="'+$bad+'"'))
        Check ($exitCode -ne 0 -and -not (Test-Path -LiteralPath $install)) ('Reject abnormal path case '+[Array]::IndexOf($badPaths,$bad))
    }
    $sentinel = Join-Path $TestRoot 'junction-sentinel'
    New-Item -ItemType Directory -Path $sentinel | Out-Null
    Set-Content -LiteralPath (Join-Path $sentinel 'keep.txt') -Value 'synthetic junction target'
    $sentinelHash = (Get-FileHash -LiteralPath (Join-Path $sentinel 'keep.txt')).Hash
    New-Item -ItemType Junction -Path $menu -Target $sentinel | Out-Null
    try {
        Check ((Run-Installer) -ne 0 -and -not (Test-Path -LiteralPath $install)) 'Reject Start menu group junction before installation'
        Check (@(Get-ChildItem -LiteralPath $sentinel -File).Count -eq 1) 'No shortcut written through Start menu junction'
    } finally {
        if ((Get-Item -LiteralPath $menu -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { [IO.Directory]::Delete($menu) }
        else { throw 'Owned menu junction changed; do not remove anything.' }
    }
    New-Item -ItemType Junction -Path $install -Target $sentinel | Out-Null
    try {
        $exitCode = Run-Installer
        Check ($exitCode -ne 0) 'Reject default program directory junction'
        Check ((Get-FileHash -LiteralPath (Join-Path $sentinel 'keep.txt')).Hash -eq $sentinelHash) 'Junction target preserved'
    } finally {
        if ((Get-Item -LiteralPath $install -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
            [IO.Directory]::Delete($install) # Non-recursive; remove owned junction only.
        } else { throw 'Owned junction unexpectedly changed; do not remove anything.' }
    }
    Set-Content -LiteralPath $install -Value 'synthetic program-path file'
    try {
        Check ((Run-Installer) -ne 0) 'Reject file in place of default program directory'
        Check ((Get-Content -LiteralPath $install -Raw).Trim() -eq 'synthetic program-path file') 'Program-path file not overwritten'
    } finally {
        if ((Get-Content -LiteralPath $install -Raw).Trim() -eq 'synthetic program-path file') { Remove-Item -LiteralPath $install }
    }
    Check ((Run-Installer) -eq 0) 'First install exits successfully'
    Check-Payload
    Check ((Test-Path -LiteralPath (Join-Path $menu '研迹 YanJi.lnk')) -and -not (Test-Path -LiteralPath $desktop)) 'Start menu present; desktop opt-in defaults off'
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut((Join-Path $menu '研迹 YanJi.lnk'))
    Check ($shortcut.TargetPath -eq (Join-Path $install 'YanJi.exe') -and $shortcut.Arguments -eq '') 'Start menu targets installed EXE without data override'
    $entry = Get-ItemProperty -LiteralPath $registry
    Check ($entry.DisplayVersion -eq '0.4.0-beta.1' -and $entry.UninstallString -like '*unins000.exe*') 'Current-user Windows Settings uninstall registration'
    Check (-not (Test-Path -LiteralPath ($registry.Replace('HKCU:','HKLM:')))) 'No all-user uninstall registration'
    Probe 'first-start'
    New-Item -ItemType Junction -Path $desktop -Target $sentinel | Out-Null
    try {
        Check ((Run-Installer -Extra @('/TASKS="desktopicon"')) -ne 0) 'Reject desktop shortcut reparse path'
        Check ((Run-Installer -Uninstall) -eq 0) 'Uninstall ignores unowned desktop path when task was never selected'
        Check ((Run-Installer) -eq 0) 'Install without desktop task ignores unrelated desktop path'
        Check ((Get-FileHash -LiteralPath (Join-Path $sentinel 'keep.txt')).Hash -eq $sentinelHash) 'Desktop shortcut junction target preserved'
    } finally {
        if ((Get-Item -LiteralPath $desktop -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { [IO.Directory]::Delete($desktop) }
        else { throw 'Owned desktop junction changed; do not remove anything.' }
    }
    Check (@(Get-ChildItem -LiteralPath $install -Recurse -File | Where-Object { $_.Name -match '\.(sqlite|db|yanjibak)(-|$)' }).Count -eq 0) 'Program directory contains no learning database or backup'
    $innerLink = Join-Path $install 'owned-test-link'
    New-Item -ItemType Junction -Path $innerLink -Target $sentinel | Out-Null
    try {
        Check ((Run-Installer) -ne 0) 'Reinstall rejects program subtree junction'
        Check ((Run-Installer -Uninstall) -ne 0) 'Uninstall rejects program subtree junction'
        Check ((Get-FileHash -LiteralPath (Join-Path $sentinel 'keep.txt')).Hash -eq $sentinelHash) 'Nested junction target preserved'
    } finally {
        if ((Get-Item -LiteralPath $innerLink -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { [IO.Directory]::Delete($innerLink) }
        else { throw 'Owned nested junction changed; do not remove anything.' }
    }
    $testData = Join-Path $TestRoot 'learning-test-profile'
    $backup = Join-Path $testData 'backups\synthetic-preserve.yanjibak'
    New-Item -ItemType Directory -Path (Split-Path $backup) -Force | Out-Null
    Set-Content -LiteralPath $backup -Value 'synthetic backup sentinel; not a valid recovery package'
    $dataSnapshot = @(Get-ChildItem -LiteralPath $testData -Recurse -File | ForEach-Object {
        [pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash -LiteralPath $_.FullName).Hash}
    })
    # Also leave an unknown file in the program folder: uninstaller must not recurse-delete it.
    $unknown = Join-Path $install 'user-added-sentinel.txt'
    Set-Content -LiteralPath $unknown -Value 'synthetic unknown file'
    Check ((Run-Installer -Extra @('/TASKS="desktopicon"')) -eq 0) 'Same-version reinstall succeeds'
    Check-Payload
    foreach ($row in $dataSnapshot) { if ((Get-FileHash -LiteralPath $row.Path).Hash -ne $row.Hash) { throw 'Isolated learning data changed during reinstall.' } }
    Check $true 'Reinstall preserves isolated database and backup bytes'
    Check (Test-Path -LiteralPath $desktop) 'Opt-in desktop shortcut created'
    Check ((Get-ItemProperty -LiteralPath 'HKCU:\Software\YanJi\Installer').YanJiDesktopShortcutInstalled -eq 1) 'Desktop shortcut ownership registered outside Inno-managed uninstall key'
    Check ((Run-Installer -Extra @('/TASKS=""')) -eq 0) 'Reinstall can deselect desktop task normally'
    Check ((Get-ItemProperty -LiteralPath 'HKCU:\Software\YanJi\Installer').YanJiDesktopShortcutInstalled -eq 1) 'Desktop ownership survives later task deselection'
    $savedShortcut = Join-Path $TestRoot 'owned-desktop-shortcut.lnk'
    Move-Item -LiteralPath $desktop -Destination $savedShortcut
    New-Item -ItemType Junction -Path $desktop -Target $sentinel | Out-Null
    try {
        Check ((Run-Installer -Uninstall) -ne 0) 'Uninstall rejects replaced owned desktop shortcut junction'
        Check ((Run-Installer -Extra @('/TASKS=""')) -ne 0) 'Deselecting task does not bypass historical desktop ownership guard'
        Check ((Get-FileHash -LiteralPath (Join-Path $sentinel 'keep.txt')).Hash -eq $sentinelHash) 'Owned desktop junction target preserved'
    } finally {
        if ((Get-Item -LiteralPath $desktop -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { [IO.Directory]::Delete($desktop) }
        else { throw 'Owned desktop replacement changed; do not remove anything.' }
        Move-Item -LiteralPath $savedShortcut -Destination $desktop
    }
    Probe 'reinstalled-start'
    $runningData = Join-Path $TestRoot 'running-test-profile'
    $running = Start-Process -FilePath (Join-Path $install 'YanJi.exe') -ArgumentList @('--test-mode','--data-dir',('"'+$runningData+'"')) -WindowStyle Hidden -PassThru
    try {
        $deadline = [DateTime]::UtcNow.AddSeconds(15)
        while (-not (Test-Path -LiteralPath (Join-Path $runningData 'window.id')) -and [DateTime]::UtcNow -lt $deadline) { Start-Sleep -Milliseconds 150 }
        Check (-not $running.HasExited -and (Test-Path -LiteralPath (Join-Path $runningData 'window.id'))) 'Owned isolated application running'
        $dllBefore = (Get-FileHash -LiteralPath (Join-Path $install 'YanJi.dll')).Hash
        Check ((Run-Installer) -ne 0) 'Reinstall refuses while YanJi is running'
        Check ((Run-Installer -Uninstall) -ne 0) 'Uninstall refuses while YanJi is running'
        Check (-not $running.HasExited -and (Get-FileHash -LiteralPath (Join-Path $install 'YanJi.dll')).Hash -eq $dllBefore) 'No forced app termination or binary overwrite'
    } finally {
        if (-not $running.HasExited) {
            Close-OwnedIdleApp $running $runningData
        }
    }
    $dataSnapshot = @(Get-ChildItem -LiteralPath $testData -Recurse -File | ForEach-Object {
        [pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash -LiteralPath $_.FullName).Hash}
    })
    Check ((Run-Installer -Uninstall) -eq 0) 'Uninstall exits successfully'
    Check (-not (Test-Path -LiteralPath (Join-Path $install 'YanJi.exe')) -and -not (Test-Path -LiteralPath $registry)) 'Program executable and uninstall registration removed'
    Check (-not (Test-Path -LiteralPath 'HKCU:\Software\YanJi\Installer')) 'Installer-owned desktop tracking key removed'
    Check (-not (Test-Path -LiteralPath $menu) -and -not (Test-Path -LiteralPath $desktop)) 'Start menu and desktop entries removed'
    Check (Test-Path -LiteralPath $unknown) 'Unknown program-directory file preserved'
    foreach ($row in $dataSnapshot) { if ((Get-FileHash -LiteralPath $row.Path).Hash -ne $row.Hash) { throw 'Isolated learning data changed during uninstall.' } }
    Check $true 'Uninstall preserves isolated database and backup bytes'
    Remove-Item -LiteralPath $unknown # Only the exact synthetic file created by this test.
    if (Test-Path -LiteralPath $install) { [IO.Directory]::Delete($install) } # Non-recursive; fail if unexpected files remain.
    Check ((Run-Installer) -eq 0) 'Install again after uninstall succeeds'
    Probe 'after-uninstall-start'
    Check ((Run-Installer -Uninstall) -eq 0) 'Final uninstall succeeds'
    Check (-not (Test-Path -LiteralPath $install)) 'Owned program directory cleaned; isolated data retained'
    if ($PriorSetup) {
        Check ((Run-Installer -InstallerOverride $PriorSetup) -eq 0) 'Synthetic prior-version installer fixture installs'
        Check ((Get-ItemProperty -LiteralPath $registry).DisplayVersion -eq '0.4.0-beta.0') 'Prior fixture registered lower installer version'
        $dataSnapshot = @(Get-ChildItem -LiteralPath $testData -Recurse -File | ForEach-Object {
            [pscustomobject]@{Path=$_.FullName;Hash=(Get-FileHash -LiteralPath $_.FullName).Hash}
        })
        Check ((Run-Installer) -eq 0) 'Production Setup upgrades prior installer registration in place'
        Check ((Get-ItemProperty -LiteralPath $registry).DisplayVersion -eq '0.4.0-beta.1') 'Upgraded current-user registration version correct'
        Check-Payload
        foreach ($row in $dataSnapshot) { if ((Get-FileHash -LiteralPath $row.Path).Hash -ne $row.Hash) { throw 'Isolated learning data changed during installer upgrade.' } }
        Check $true 'Installer upgrade preserves isolated database and backup bytes'
        Probe 'upgraded-start'
        Check ((Run-Installer -Uninstall) -eq 0) 'Upgraded installation uninstalls successfully'
        Check (-not (Test-Path -LiteralPath $install)) 'No test installation left behind'
    }
} catch {
    $failure = $_.Exception.Message
    throw
} finally {
    $identity = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
    [pscustomobject]@{
        Status=$(if($failure){'FAIL'}else{'PASS'}); Failure=$failure;
        SetupSha256=(Get-FileHash -LiteralPath $Setup).Hash;
        WindowsVersion=[Environment]::OSVersion.Version.ToString();
        TestProcessElevated=$identity.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator);
        RealLearningDataAccessed=$false; ApplicationDataIsolation='--test-mode --data-dir (owned artifacts only)';
        Checks=@($checks); RemoteWrites=$false;
        UpgradeScope='Optional synthetic beta.0 installer fixture uses identical app binaries; tests installer upgrade mechanics only, never app/schema migration';
        NotTested=@('Downloaded-file SmartScreen reputation','Clean Windows offline machine','Physical IME/DPI/power compatibility','Different application-version/schema upgrades')
    } | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $TestRoot 'installer-validation.json') -Encoding utf8
}
