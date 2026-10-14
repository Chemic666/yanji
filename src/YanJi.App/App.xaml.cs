using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace YanJi.App;
public partial class App : Application
{
    private Mutex? _instance;
    private string _dataDirectory="";
    public string? SmokePath {get;private set;}
    public bool Measure {get;private set;}
    public bool PerformanceOnly {get;private set;}
    public bool StartupOnly {get;private set;}
    public bool V02Visual {get;private set;}
    public string? V021Fixture {get;private set;}
    public bool V021Capture {get;private set;}
    public bool V022Capture {get;private set;}
    public bool V023Capture {get;private set;}
    public bool V03Capture {get;private set;}
    public bool V03Reopen {get;private set;}
    public bool V03Layout {get;private set;}
    internal bool ValidationFailed {get;set;}
    internal Stopwatch StartupClock {get;}=Stopwatch.StartNew();
    public uint ShowMessage {get;private set;}
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern uint RegisterWindowMessage(string name);
    [DllImport("user32.dll")]private static extern bool PostMessage(IntPtr hwnd,uint message,IntPtr wParam,IntPtr lParam);
    [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint processId);
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("zh-CN");CultureInfo.CurrentUICulture=CultureInfo.GetCultureInfo("zh-CN");
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),new FrameworkPropertyMetadata(XmlLanguage.GetLanguage("zh-CN")));
        try
        {
            string? explicitDirectory=null;
            for(var i=0;i<e.Args.Length;i++)
            {
                if(e.Args[i]=="--data-dir"&&i+1<e.Args.Length)explicitDirectory=e.Args[++i];
                else if(e.Args[i]=="--smoke"&&i+1<e.Args.Length)SmokePath=Path.GetFullPath(e.Args[++i]);
                else if(e.Args[i]=="--measure")Measure=true;
                else if(e.Args[i]=="--performance"&&i+1<e.Args.Length){SmokePath=Path.GetFullPath(e.Args[++i]);PerformanceOnly=true;}
                else if(e.Args[i]=="--startup-probe"&&i+1<e.Args.Length){SmokePath=Path.GetFullPath(e.Args[++i]);StartupOnly=true;}
                else if(e.Args[i]=="--v021-fixture"&&i+1<e.Args.Length)V021Fixture=Path.GetFullPath(e.Args[++i]);
                else if(e.Args[i]=="--v021-capture"&&i+1<e.Args.Length){SmokePath=Path.GetFullPath(e.Args[++i]);V021Capture=true;}
                else if(e.Args[i]=="--v022-capture"&&i+1<e.Args.Length){SmokePath=Path.GetFullPath(e.Args[++i]);V022Capture=true;}
                else if(e.Args[i]=="--v023-capture"&&i+1<e.Args.Length){SmokePath=Path.GetFullPath(e.Args[++i]);V023Capture=true;}
                else if(e.Args[i]=="--v03-capture"&&i+1<e.Args.Length){SmokePath=Path.GetFullPath(e.Args[++i]);V03Capture=true;}
                else if(e.Args[i]=="--v03-reopen"&&i+1<e.Args.Length){SmokePath=Path.GetFullPath(e.Args[++i]);V03Reopen=true;}
                else if(e.Args[i]=="--v03-layout"&&i+1<e.Args.Length){SmokePath=Path.GetFullPath(e.Args[++i]);V03Layout=true;}
                else if(e.Args[i]=="--v02-visual"&&i+1<e.Args.Length){SmokePath=Path.GetFullPath(e.Args[++i]);V02Visual=true;}
                else throw new ArgumentException("未知参数。支持 --data-dir <独立数据目录> 与 --smoke <报告目录>。");
            }
            if(SmokePath!=null&&explicitDirectory==null)throw new ArgumentException("自动化验证必须指定独立数据目录。");
            _dataDirectory=Path.GetFullPath(explicitDirectory??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"YanJi"));
            if(V021Capture && (V021Fixture==null||SmokePath==null||!SmokePath.StartsWith(Path.GetFullPath("artifacts")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!_dataDirectory.StartsWith(Path.GetFullPath("artifacts")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("V021 capture requires fixture and fresh project artifacts data/output.");
            if((V022Capture||V023Capture||V03Capture||V03Reopen||V03Layout)&&(!SmokePath!.StartsWith(Path.GetFullPath("artifacts")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)||!_dataDirectory.StartsWith(Path.GetFullPath("artifacts")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)))throw new ArgumentException("Capture requires owned project artifacts.");
            if(V02Visual||V021Capture||V022Capture||V023Capture||V03Capture||V03Layout)
            {
                var formal=Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"YanJi"));
                if(string.Equals(_dataDirectory,formal,StringComparison.OrdinalIgnoreCase)||Directory.Exists(_dataDirectory)&&Directory.EnumerateFileSystemEntries(_dataDirectory).Any())throw new ArgumentException("视觉验收必须使用新的独立数据目录。");
                System.Windows.Media.RenderOptions.ProcessRenderMode=System.Windows.Interop.RenderMode.SoftwareOnly;
            }
            var identity=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(_dataDirectory.TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant())))[..24];
            ShowMessage=RegisterWindowMessage("YanJi.Show."+identity);
            _instance=new Mutex(true,"Local\\YanJi."+identity,out var created);
            if(!created)
            {
                var hint=Path.Combine(_dataDirectory,"window.id");
                for(var retry=0;retry<10;retry++)
                {
                    try
                    {
                        var values=File.ReadAllLines(hint);var handle=new IntPtr(long.Parse(values[0]));var pid=uint.Parse(values[1]);
                        GetWindowThreadProcessId(handle,out var currentPid);
                        if(currentPid==pid){PostMessage(handle,ShowMessage,IntPtr.Zero,IntPtr.Zero);break;}
                    }
                    catch(IOException){}Thread.Sleep(100);
                }
                Shutdown();return;
            }
            var model=new MainViewModel(_dataDirectory);MainWindow=new MainWindow(model);if(PerformanceOnly||StartupOnly)MainWindow.ShowActivated=false;MainWindow.Show();
        }
        catch(Exception exception)
        {
            if(SmokePath!=null){Directory.CreateDirectory(SmokePath);File.WriteAllText(Path.Combine(SmokePath,"startup-failure.txt"),exception.ToString());}
            else MessageBox.Show("研迹未能启动："+exception.Message+"\n已有数据未被自动恢复或清除。","研迹",MessageBoxButton.OK,MessageBoxImage.Error);
            Shutdown(1);
        }
    }
    public void RecordWindow(IntPtr handle)=>File.WriteAllLines(Path.Combine(_dataDirectory,"window.id"),[handle.ToInt64().ToString(),Environment.ProcessId.ToString()]);
    protected override void OnExit(ExitEventArgs e)
    {if(ValidationFailed)e.ApplicationExitCode=1;if(_instance!=null){try{_instance.ReleaseMutex();}catch(ApplicationException){} _instance.Dispose();}base.OnExit(e);}
}
