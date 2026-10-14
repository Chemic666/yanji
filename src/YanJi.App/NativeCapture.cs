using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
namespace YanJi.App;
internal sealed class NativeHoverUnavailableException(string message):InvalidOperationException(message);
/// <summary>Captures only this validation process's HWND, including its real native frame.</summary>
internal static class NativeCapture
{
 [StructLayout(LayoutKind.Sequential)] private struct Rect {public int Left,Top,Right,Bottom;}
 [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr h,out Rect r);
 [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr h,IntPtr dc,uint flags);
 [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr h,out uint p);
 [StructLayout(LayoutKind.Sequential)] internal struct CursorPoint {public int X,Y;}
 [DllImport("user32.dll")] private static extern bool GetCursorPos(out CursorPoint point);
 [DllImport("user32.dll")] private static extern bool SetCursorPos(int x,int y);
 [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(CursorPoint point);
 [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
 internal static object PointerObservation()
 {var available=GetCursorPos(out var current);var under=available?WindowFromPoint(current):IntPtr.Zero;GetWindowThreadProcessId(under,out var underOwner);GetWindowThreadProcessId(GetForegroundWindow(),out var foregroundOwner);return new{available,current.X,current.Y,underOwner,foregroundOwner,process=Environment.ProcessId};}
 internal sealed record PointerRestore(CursorPoint Cursor,Window Window,bool Topmost);
 internal static PointerRestore MoveToOwned(FrameworkElement element,Point local)
 {
  GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);
  if(foreground!=0)
  {
   using var process=System.Diagnostics.Process.GetProcessById((int)foreground);
   if(process.ProcessName.Equals("LockApp",StringComparison.OrdinalIgnoreCase))throw new NativeHoverUnavailableException("Windows LockApp is foreground; real pointer hover NOT TESTED. No unlock or system setting change attempted.");
  }
  var window=Window.GetWindow(element)??throw new InvalidOperationException("No owned chart window.");var h=new WindowInteropHelper(window).Handle;GetWindowThreadProcessId(h,out var p);if(p!=Environment.ProcessId)throw new InvalidOperationException("Pointer ownership mismatch.");
  if(!GetCursorPos(out var original))throw new InvalidOperationException("Owned hover movement unavailable.");
  var restore=new PointerRestore(original,window,window.Topmost);
  try
  {
   // Validation windows must receive real input rather than be covered by another app.
   window.Topmost=true;window.Activate();window.UpdateLayout();
   var target=element.PointToScreen(local);if(!GetWindowRect(h,out var r)||target.X<r.Left||target.X>=r.Right||target.Y<r.Top||target.Y>=r.Bottom)throw new InvalidOperationException("Chart point is outside owned window.");
   if(!SetCursorPos((int)target.X,(int)target.Y)||!GetCursorPos(out var current))throw new InvalidOperationException("Owned hover movement unavailable.");
   GetWindowThreadProcessId(WindowFromPoint(current),out var owner);if(owner!=Environment.ProcessId)throw new InvalidOperationException("Owned chart remains occluded; real hover cannot be verified.");
   return restore;
  }
  catch{RestoreCursor(restore);throw;}
 }
 internal static void RestoreCursor(PointerRestore restore)
 {restore.Window.Topmost=restore.Topmost;SetCursorPos(restore.Cursor.X,restore.Cursor.Y);}
 internal static void SavePopup(FrameworkElement element,string file)=>SaveHandle((PresentationSource.FromVisual(element) as HwndSource)?.Handle??IntPtr.Zero,file);
 public static void Save(Window window,string file)
 =>SaveHandle(new WindowInteropHelper(window).Handle,file);
 private static void SaveHandle(IntPtr handle,string file)
 {GetWindowThreadProcessId(handle,out var owner);if(owner!=Environment.ProcessId||handle==IntPtr.Zero)throw new InvalidOperationException("Capture ownership mismatch.");if(!GetWindowRect(handle,out var r))throw new InvalidOperationException("GetWindowRect failed.");using var image=new System.Drawing.Bitmap(r.Right-r.Left,r.Bottom-r.Top);using var g=System.Drawing.Graphics.FromImage(image);var dc=g.GetHdc();try{if(!PrintWindow(handle,dc,2))throw new InvalidOperationException("PrintWindow failed.");}finally{g.ReleaseHdc(dc);}image.Save(file,System.Drawing.Imaging.ImageFormat.Png);}
}
