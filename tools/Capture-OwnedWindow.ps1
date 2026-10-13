param([Parameter(Mandatory=$true)][string]$DataDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory,[Parameter(Mandatory=$true)][int]$ExpectedProcessId,[switch]$BaselineViews)
$ErrorActionPreference='Stop'
$release=Join-Path (Split-Path $PSScriptRoot) 'releases\YanJi-v0.1.1-win-x64'
[Reflection.Assembly]::LoadFrom("$release\System.Drawing.Common.dll") | Out-Null
Add-Type @'
using System; using System.Runtime.InteropServices;
public static class YanJiOwnedCapture {
 [StructLayout(LayoutKind.Sequential)] public struct Rect {public int Left,Top,Right,Bottom;}
 [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out Rect r);
 [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h,IntPtr dc,uint flags);
 [DllImport("user32.dll")] public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr c);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint p);
 [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h,uint m,IntPtr w,IntPtr l);
}
'@
$identity=Get-Content (Join-Path $DataDirectory 'window.id')
$handle=[IntPtr]::new([long]$identity[0]);$ownedId=[uint32]0
[YanJiOwnedCapture]::GetWindowThreadProcessId($handle,[ref]$ownedId) | Out-Null
if($ownedId -ne $ExpectedProcessId -or [int]$identity[1] -ne $ExpectedProcessId){throw 'Capture window ownership mismatch.'}
if(Test-Path -LiteralPath $OutputDirectory){throw 'Capture output must be new.'}
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
function Capture([string]$Name){
    $previous=[YanJiOwnedCapture]::SetThreadDpiAwarenessContext([IntPtr]::new(-4))
    try{
        $rect=[YanJiOwnedCapture+Rect]::new();[YanJiOwnedCapture]::GetWindowRect($handle,[ref]$rect) | Out-Null
        $bitmap=[Drawing.Bitmap]::new($rect.Right-$rect.Left,$rect.Bottom-$rect.Top)
        try{$graphics=[Drawing.Graphics]::FromImage($bitmap);try{$dc=$graphics.GetHdc();try{if(![YanJiOwnedCapture]::PrintWindow($handle,$dc,2)){throw 'Native PrintWindow failed.'}}finally{$graphics.ReleaseHdc($dc)}}finally{$graphics.Dispose()};$bitmap.Save((Join-Path $OutputDirectory "$Name.png"),[Drawing.Imaging.ImageFormat]::Png)}finally{$bitmap.Dispose()}
    }finally{[YanJiOwnedCapture]::SetThreadDpiAwarenessContext($previous) | Out-Null}
}
if($BaselineViews){
    [Reflection.Assembly]::LoadFrom("$release\UIAutomationTypes.dll") | Out-Null
    [Reflection.Assembly]::LoadFrom("$release\UIAutomationClient.dll") | Out-Null
    $root=[Windows.Automation.AutomationElement]::FromHandle($handle)
    $condition=[Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::AutomationIdProperty,'NavigationList')
    $nav=$root.FindFirst([Windows.Automation.TreeScope]::Descendants,$condition)
    if(!$nav){throw 'Owned navigation automation element missing.'}
    $items=$nav.FindAll([Windows.Automation.TreeScope]::Children,[Windows.Automation.Condition]::TrueCondition)
    function Select-Page([int]$Index){$items[$Index].GetCurrentPattern([Windows.Automation.SelectionItemPattern]::Pattern).Select();Start-Sleep -Milliseconds 350}
    Select-Page 0;Capture 'baseline-today-light'
    Select-Page 4;Capture 'baseline-statistics-light'
    Select-Page 5
    $theme=$root.FindFirst([Windows.Automation.TreeScope]::Descendants,[Windows.Automation.PropertyCondition]::new([Windows.Automation.AutomationElement]::NameProperty,'深色主题'))
    if(!$theme){throw 'Owned theme control missing.'}
    $theme.GetCurrentPattern([Windows.Automation.TogglePattern]::Pattern).Toggle();Start-Sleep -Milliseconds 400
    Select-Page 0;Capture 'baseline-today-dark'
    Select-Page 4;Capture 'baseline-statistics-dark'
}else{Capture 'native-window'}
@{Status='PASS';ExpectedProcessId=$ExpectedProcessId;Method='Native PrintWindow on verified owned HWND; physical window frame and pixels';Files=@([IO.Directory]::GetFiles($OutputDirectory,'*.png') | ForEach-Object {Split-Path $_ -Leaf})} | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $OutputDirectory 'capture.json')
if($BaselineViews){[YanJiOwnedCapture]::PostMessage($handle,0x10,[IntPtr]::Zero,[IntPtr]::Zero) | Out-Null}
