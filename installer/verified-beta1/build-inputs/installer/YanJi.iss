; Compile only through tools/Build-Installer.py (verified, explicit payload list).
#ifndef PayloadDir
  #error PayloadDir is required
#endif
#ifndef PayloadList
  #error PayloadList is required
#endif
#ifndef ArtifactDir
  #error ArtifactDir is required
#endif

[Setup]
AppId={{BF9A7AA8-88EF-4470-A6D4-7A4E531E77B3}
AppName=研迹 YanJi
AppVersion=0.4.0-beta.1
AppVerName=研迹 YanJi 0.4.0 Beta 1
AppPublisher=YanJi contributors
AppPublisherURL=https://github.com/Chemic666/yanji
AppSupportURL=https://github.com/Chemic666/yanji/issues
DefaultDirName={localappdata}\Programs\YanJi
DefaultGroupName=研迹 YanJi
PrivilegesRequired=lowest
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
MinVersion=10.0.22000
DisableDirPage=yes
DisableProgramGroupPage=yes
UsePreviousAppDir=no
UsePreviousGroup=no
AllowNoIcons=no
SetupMutex=YanJiInstallerFirstRelease
CloseApplications=no
RestartApplications=no
RestartIfNeededByRun=no
UninstallDisplayName=研迹 YanJi 0.4.0 Beta 1
UninstallDisplayIcon={app}\YanJi.exe
UninstallFilesDir={app}
UninstallLogMode=append
OutputDir={#ArtifactDir}
OutputBaseFilename=YanJi-v0.4.0-beta.1-Setup
SetupIconFile={#PayloadDir}\YanJi.ico
LicenseFile={#PayloadDir}\LICENSE
InfoBeforeFile={#PayloadDir}\INSTALLER-INFO.txt
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion=0.4.0.1
VersionInfoProductVersion=0.4.0.1
VersionInfoProductTextVersion=0.4.0-beta.1
SignedUninstaller=no
ChangesAssociations=no
ChangesEnvironment=no

[Languages]
Name: chinesesimp; MessagesFile: "ChineseSimplified.isl"

[Tasks]
Name: desktopicon; Description: "创建桌面快捷方式"; GroupDescription: "快捷方式："; Flags: unchecked

[Files]
#include PayloadList

[Icons]
Name: "{userprograms}\研迹 YanJi\研迹 YanJi"; Filename: "{app}\YanJi.exe"; WorkingDir: "{app}"
Name: "{userprograms}\研迹 YanJi\卸载研迹 YanJi"; Filename: "{uninstallexe}"
Name: "{userdesktop}\研迹 YanJi"; Filename: "{app}\YanJi.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Registry]
; Keep the flag across reinstalls which deselect the task: the previous link is
; still owned by the uninstall log. Do not inspect an unrelated desktop path.
Root: HKCU; Subkey: "Software\YanJi\Installer"; ValueType: dword; ValueName: "YanJiDesktopShortcutInstalled"; ValueData: "1"; Tasks: desktopicon; Flags: uninsdeletevalue uninsdeletekeyifempty

; Deliberately no Run, InstallDelete or UninstallDelete sections.
; Setup never starts the application or opens, migrates or deletes learning data.

[Messages]
FinishedLabel=研迹 YanJi 已安装。请从开始菜单启动。学习数据位于 %%LOCALAPPDATA%%\YanJi，卸载会保留学习记录和备份。
ConfirmUninstall=是否卸载研迹 YanJi 程序？学习记录和备份将保留，不会自动删除。

[Code]
function GetFileAttributesW(Name: String): Cardinal;
  external 'GetFileAttributesW@kernel32.dll stdcall';

function ExpectedDirectory: String;
begin
  Result := ExpandConstant('{localappdata}\Programs\YanJi');
end;

function PlainDirectoryChain(Path: String): Boolean;
var
  Attr: Cardinal;
  Parent: String;
begin
  Result := False;
  if Path = '' then exit;
  Parent := ExtractFileDir(Path);
  { Root-first: never probe a child through an unchecked ancestor junction. }
  if (Parent <> '') and (Parent <> Path) then
    if not PlainDirectoryChain(Parent) then exit;
  Attr := GetFileAttributesW(Path);
  if Attr <> $FFFFFFFF then begin
    if ((Attr and $400) <> 0) or ((Attr and $10) = 0) then exit;
  end else if DLLGetLastError <> 2 then begin
    if DLLGetLastError <> 3 then exit;
  end;
  Result := True;
end;

function PlainFile(Path: String): Boolean;
var
  Attr: Cardinal;
begin
  Result := False;
  if not PlainDirectoryChain(ExtractFileDir(Path)) then exit;
  Attr := GetFileAttributesW(Path);
  if Attr <> $FFFFFFFF then begin
    if ((Attr and $400) <> 0) or ((Attr and $10) <> 0) then exit;
  end else if DLLGetLastError <> 2 then begin
    if DLLGetLastError <> 3 then exit;
  end;
  Result := True;
end;

function SafeShellPaths(IncludeDesktop: Boolean): Boolean;
var
  Menu: String;
begin
  Menu := ExpandConstant('{userprograms}\研迹 YanJi');
  Result := PlainDirectoryChain(Menu);
  if Result then Result := PlainFile(Menu + '\研迹 YanJi.lnk');
  if Result then Result := PlainFile(Menu + '\卸载研迹 YanJi.lnk');
  if Result and IncludeDesktop then
    Result := PlainFile(ExpandConstant('{userdesktop}\研迹 YanJi.lnk'));
end;

function OwnsDesktopShortcut: Boolean;
var
  Flag: Cardinal;
begin
  Result := False;
  if RegQueryDWordValue(HKCU, 'Software\YanJi\Installer', 'YanJiDesktopShortcutInstalled', Flag) then
    Result := Flag = 1;
end;

function PlainTree(Path: String): Boolean;
var
  Entry: TFindRec;
begin
  Result := False;
  if not PlainDirectoryChain(Path) then exit;
  if FindFirst(AddBackslash(Path) + '*', Entry) then begin
    try
      repeat
        if (Entry.Name <> '.') and (Entry.Name <> '..') then begin
          if (Entry.Attributes and $400) <> 0 then exit;
          if (Entry.Attributes and $10) <> 0 then
            if not PlainTree(AddBackslash(Path) + Entry.Name) then exit;
        end;
      until not FindNext(Entry);
    finally
      FindClose(Entry);
    end;
  end;
  Result := True;
end;

function AllowedDirectory(Path: String): Boolean;
begin
  { Compare before filesystem access: rejected /DIR paths are never opened. }
  Result := CompareText(Path, ExpectedDirectory) = 0;
  if Result then Result := PlainTree(Path);
end;

function ApplicationStopped: Boolean;
var
  Locator, Service, Processes: Variant;
begin
  Result := False;
  try
    Locator := CreateOleObject('WbemScripting.SWbemLocator');
    Service := Locator.ConnectServer('', 'root\CIMV2');
    Processes := Service.ExecQuery('SELECT ProcessId FROM Win32_Process WHERE Name = ''YanJi.exe''');
    Result := Processes.Count = 0;
  except
    Log('Could not verify that YanJi is closed; refusing to modify program files.');
  end;
end;

function InitializeSetup: Boolean;
var
  Requested: String;
begin
  Requested := ExpandConstant('{param:DIR|}') ;
  Result := True;
  if Requested <> '' then Result := CompareText(Requested, ExpectedDirectory) = 0;
  if Result then Result := PlainTree(ExpectedDirectory);
  if Result then Result := SafeShellPaths(OwnsDesktopShortcut);
  if not Result then
    SuppressibleMsgBox('安装已停止：仅支持当前用户默认程序目录，且不能经过目录联接、符号链接或文件路径。未访问学习数据。', mbError, MB_OK, IDOK);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if not AllowedDirectory(ExpandConstant('{app}')) then
    Result := '程序目录无效或含目录联接/符号链接，已停止安装。'
  else if not SafeShellPaths(WizardIsTaskSelected('desktopicon') or OwnsDesktopShortcut) then
    Result := '快捷方式目录无效或含目录联接/符号链接，已停止安装。'
  else if not ApplicationStopped then
    Result := '请先在研迹中结束计时并退出应用，再重新安装。不自动关闭学习程序。';
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssInstall then
    if not AllowedDirectory(ExpandConstant('{app}')) then
      RaiseException('程序目录在安装前发生变化，已停止。')
    else if not SafeShellPaths(WizardIsTaskSelected('desktopicon') or OwnsDesktopShortcut) then
      RaiseException('快捷方式目录无效，已停止。')
    else if not ApplicationStopped then
      RaiseException('请先退出研迹，再安装。');
end;

function InitializeUninstall: Boolean;
begin
  Result := AllowedDirectory(ExpandConstant('{app}'));
  if Result then Result := SafeShellPaths(OwnsDesktopShortcut);
  if not Result then begin
    SuppressibleMsgBox('程序目录无效或含目录联接/符号链接，已停止卸载。学习数据不会被删除。', mbError, MB_OK, IDOK);
    exit;
  end;
  Result := ApplicationStopped;
  if not Result then
    SuppressibleMsgBox('请先在研迹中结束计时并退出应用，再卸载。不自动关闭学习程序，学习数据会保留。', mbError, MB_OK, IDOK);
end;
