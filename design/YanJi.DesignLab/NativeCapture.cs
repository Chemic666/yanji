using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
namespace YanJi.DesignLab;
internal static class NativeCapture
{
 [StructLayout(LayoutKind.Sequential)] private struct Rect {public int Left,Top,Right,Bottom;}
 [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr h,out Rect r);
 [DllImport("user32.dll")] private static extern bool PrintWindow(IntPtr h,IntPtr dc,uint flags);
 [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr h,out uint p);
 [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr h,int command);
 [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int cx,int cy,uint flags);
 public static void Present(Window window){var h=new WindowInteropHelper(window).Handle;GetWindowThreadProcessId(h,out var owner);if(owner!=Environment.ProcessId)throw new InvalidOperationException("Ownership mismatch.");ShowWindow(h,5);SetWindowPos(h,IntPtr.Zero,0,0,0,0,0x13);}
 public static void Save(Window window,string file){var handle=new WindowInteropHelper(window).Handle;GetWindowThreadProcessId(handle,out var owner);if(owner!=Environment.ProcessId||handle==IntPtr.Zero)throw new InvalidOperationException("Window ownership mismatch.");if(!GetWindowRect(handle,out var r))throw new InvalidOperationException("GetWindowRect failed.");using var image=new System.Drawing.Bitmap(r.Right-r.Left,r.Bottom-r.Top);using var g=System.Drawing.Graphics.FromImage(image);var dc=g.GetHdc();try{if(!PrintWindow(handle,dc,2))throw new InvalidOperationException("PrintWindow failed.");}finally{g.ReleaseHdc(dc);}image.Save(file,System.Drawing.Imaging.ImageFormat.Png);}
}
