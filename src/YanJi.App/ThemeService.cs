using System;
using System.Windows;
using System.Windows.Media;
namespace YanJi.App;
public static class ThemeService
{
    public static bool Dark {get;private set;}
    private static bool _initialized;
    private static bool _highContrast;
    public static event Action? Changed;
    public static void Apply(bool dark)=>ApplyPalette(dark,SystemParameters.HighContrast);
    internal static void ApplyPalette(bool dark,bool highContrast)
    {
        if(_initialized&&Dark==dark&&_highContrast==highContrast)return;
        Dark=dark;
#pragma warning disable WPF0001
        Application.Current.ThemeMode=dark?ThemeMode.Dark:ThemeMode.Light;
#pragma warning restore WPF0001
        var colors=dark?new[]{"#1C201D","#272D28","#30382F","#3C473D","#E6ECE4","#B5C1B4","#A3C4A5","#344737","#F1A8A0","#472D2C","#E8C18C","#433A29","#222720","#C0CEC0","#354137","#D2DFD0","#798778","#17251B"}:
            new[]{"#F6F4EE","#FEFDF9","#EDEFE7","#D9DED3","#29352E","#57665A","#456653","#DFE8D9","#A13E36","#F7E8E3","#805817","#F4ECD9","#ECEEE5","#425448","#E1E6DA","#354D3F","#899286","#FFFFFF"};
        var keys=new[]{"Canvas","CardBackground","Soft","Line","Ink","Muted","Accent","AccentSoft","Danger","DangerSoft","Warning","WarningSoft","NavigationBackground","NavigationInk","Hover","Pressed","DisabledInk","AccentInk"};
        for(var i=0;i<keys.Length;i++)Application.Current.Resources[keys[i]]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i]));
        Application.Current.Resources["SelectedInk"]=Application.Current.Resources["Accent"];
        Application.Current.Resources["SelectionSurface"]=Application.Current.Resources["AccentSoft"];
        if(highContrast)
        {
            foreach(var key in new[]{"Canvas","CardBackground","Soft","NavigationBackground"})Application.Current.Resources[key]=SystemColors.WindowBrush;
            foreach(var key in new[]{"Ink","Muted","NavigationInk"})Application.Current.Resources[key]=SystemColors.WindowTextBrush;
            foreach(var key in new[]{"Line","Accent"})Application.Current.Resources[key]=SystemColors.HighlightBrush;
            Application.Current.Resources["AccentSoft"]=SystemColors.HighlightBrush;Application.Current.Resources["AccentInk"]=SystemColors.HighlightTextBrush;
            Application.Current.Resources["SelectedInk"]=SystemColors.HighlightTextBrush;
            Application.Current.Resources["SelectionSurface"]=SystemColors.WindowBrush;
            Application.Current.Resources["Hover"]=SystemColors.ControlBrush;
            Application.Current.Resources["DisabledInk"]=SystemColors.GrayTextBrush;
            foreach(var key in new[]{"Danger","Warning"})Application.Current.Resources[key]=SystemColors.WindowTextBrush;
            foreach(var key in new[]{"DangerSoft","WarningSoft"})Application.Current.Resources[key]=SystemColors.WindowBrush;
        }
        var mini=((SolidColorBrush)Application.Current.Resources["CardBackground"]).Color;
        mini.A=highContrast?(byte)255:(byte)230;
        Application.Current.Resources["MiniSurface"]=new SolidColorBrush(mini);
        Application.Current.Resources["MiniEdge"]=new SolidColorBrush(highContrast?SystemColors.WindowTextColor:dark?Color.FromArgb(110,150,175,153):Color.FromArgb(180,255,255,255));
        _highContrast=highContrast;
        _initialized=true;
        Changed?.Invoke();
    }
    public static Brush Brush(string key)=>(Brush)Application.Current.FindResource(key);
    public static string SubjectColor(string id)
    {var colors=Dark?new[]{"#9DBBA2","#9DBED0","#BBAACB","#D3B389","#CFABB0","#A8BEC8","#BBC799","#B9B4D0"}:new[]{"#6A8875","#7B9EB4","#A79ABB","#BFA078","#B17F89","#779EAA","#83985E","#9189AC"};if(id=="math")return colors[0];if(id=="english")return colors[1];if(id=="408")return colors[2];if(id=="politics")return colors[3];uint hash=2166136261;foreach(var c in id)hash=(hash^c)*16777619;return colors[hash%(uint)colors.Length];}
}
