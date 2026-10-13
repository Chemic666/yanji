using System;
using System.Windows;
using System.Windows.Media;
namespace YanJi.App;
public static class ThemeService
{
    public static bool Dark {get;private set;}
    private static bool _initialized;
    public static event Action? Changed;
    public static void Apply(bool dark)
    {
        if(_initialized&&Dark==dark)return;
        Dark=dark;
#pragma warning disable WPF0001
        Application.Current.ThemeMode=dark?ThemeMode.Dark:ThemeMode.Light;
#pragma warning restore WPF0001
        var colors=dark?new[]{"#101920","#19262F","#223440","#2E4652","#EEF5F7","#A8BDC7","#70DED0","#163E40","#FF9C9C","#432B31","#F4CA82","#3B3323"}:
            new[]{"#F3F6F8","#FFFFFF","#EDF3F5","#DFE8ED","#18313E","#5C7380","#087F86","#DDF4F0","#B63249","#FDEDF0","#8A5A0A","#FFF3D8"};
        var keys=new[]{"Canvas","CardBackground","Soft","Line","Ink","Muted","Accent","AccentSoft","Danger","DangerSoft","Warning","WarningSoft"};
        for(var i=0;i<keys.Length;i++)Application.Current.Resources[keys[i]]=new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[i]));
        _initialized=true;
        Changed?.Invoke();
    }
    public static Brush Brush(string key)=>(Brush)Application.Current.FindResource(key);
    public static string SubjectColor(string id)
    {if(id=="math")return "#23A99D";if(id=="english")return "#538CDB";if(id=="408")return "#BC86DC";if(id=="politics")return "#EAAD59";var colors=new[]{"#23A99D","#538CDB","#BC86DC","#EAAD59","#E47E91","#7098AE","#83B55E","#9A8FD2"};uint hash=2166136261;foreach(var c in id)hash=(hash^c)*16777619;return colors[hash%(uint)colors.Length];}
}
