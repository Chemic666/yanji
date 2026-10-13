using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
namespace YanJi.DesignLab;
public partial class LabApp : Application
{
 protected override void OnStartup(StartupEventArgs e)
 {
  base.OnStartup(e);
  RenderOptions.ProcessRenderMode=RenderMode.SoftwareOnly;
  try{
   string? Arg(string name){var i=Array.IndexOf(e.Args,name);return i>=0&&i+1<e.Args.Length?e.Args[i+1]:null;}
   var data=Path.GetFullPath(Arg("--data-dir")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"YanJiVisualLab","v02"));
   var production=Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"YanJi"))+Path.DirectorySeparatorChar;
   if((data+Path.DirectorySeparatorChar).StartsWith(production,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("视觉样板禁止打开正式 YanJi 数据目录。");
   var marker=Path.Combine(data,"VISUAL-LAB-ONLY.txt");
   if(Directory.Exists(data)&&Directory.EnumerateFileSystemEntries(data).Any()&&!File.Exists(marker))throw new InvalidOperationException("样板目录必须为空或带样板标记；拒绝打开现有数据目录。");
   Directory.CreateDirectory(data);if(!File.Exists(marker))File.WriteAllText(marker,"YanJi V0.2 visual exploration. Isolated demo data. Not the production user database.");
   var window=new LabWindow(data,Arg("--capture-dir"),e.Args.Contains("--empty"));MainWindow=window;window.Show();
  }catch(Exception ex){MessageBox.Show(ex.Message,"视觉样板未启动");Shutdown(1);}
 }
}
