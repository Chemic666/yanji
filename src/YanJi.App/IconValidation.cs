using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
namespace YanJi.App;

// Only called by the existing isolated --startup-probe; no normal-runtime work.
internal static class IconValidation
{
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam);
    internal static void Capture(MainWindow window,string output)
    {
        window.UpdateLayout();
        if(window.Icon is not BitmapSource bitmap)throw new InvalidOperationException("Window icon did not decode.");
        using var resource=Application.GetResourceStream(new Uri("pack://application:,,,/Assets/Icons/YanJi.ico"))!.Stream;
        using var bytes=new MemoryStream();resource.CopyTo(bytes);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using(var file=new FileStream(Path.Combine(output,"window-resource.png"),FileMode.CreateNew))encoder.Save(file);
        object Save(System.Drawing.Icon icon,string name)
        {
            using var image=icon.ToBitmap();image.Save(Path.Combine(output,name),System.Drawing.Imaging.ImageFormat.Png);
            return new{File=name,image.Width,image.Height};
        }
        var hwnd=new WindowInteropHelper(window).Handle;
        object Native(int kind,string name)
        {
            var handle=SendMessage(hwnd,0x007F,new IntPtr(kind),IntPtr.Zero); // WM_GETICON, owned window only.
            if(handle==IntPtr.Zero)throw new InvalidOperationException("Native window icon is missing: "+name);
            using var borrowed=System.Drawing.Icon.FromHandle(handle);
            using var owned=(System.Drawing.Icon)borrowed.Clone();return Save(owned,name);
        }
        using var shell=System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!)
            ??throw new InvalidOperationException("Shell icon extraction failed.");
        var result=new{Status="PASS",ProcessId=Environment.ProcessId,
            Assembly=typeof(App).Assembly.GetName().Version?.ToString(),
            BinarySha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(App).Assembly.Location))),
            EmbeddedIcoSha256=Convert.ToHexString(SHA256.HashData(bytes.ToArray())),
            WindowSource=window.Icon.ToString(),window.ShowInTaskbar,
            NativeSmall=Native(0,"window-small.png"),NativeLarge=Native(1,"window-large.png"),
            ShellAssociated=Save(shell,"shell-associated.png"),Tray=Save(window.TrayIconForValidation,"tray-icon.png"),
            PhysicalTaskbarAndTrayAppearance="Requires actual shell screenshot; handle/resource checks alone do not prove shell cache appearance."};
        File.WriteAllText(Path.Combine(output,"icons.json"),JsonSerializer.Serialize(result,new JsonSerializerOptions{WriteIndented=true}));
    }
}
