using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace YanJi.App;
public partial class MainWindow
{
    private double _atmosphereStrength=1;
    internal double AtmosphereStrength=>_atmosphereStrength;
    internal void SetAtmosphere(double strength)
    {_atmosphereStrength=Math.Clamp(strength,0,1);RefreshAtelier();}
    private void Atmosphere_Click(object sender,RoutedEventArgs e)
    {if(sender is MenuItem item&&double.TryParse(item.Tag?.ToString(),NumberStyles.Float,CultureInfo.InvariantCulture,out var strength))SetAtmosphere(strength);}
    private void RefreshAtelier()
    {
        var dark=ThemeService.Dark;var contrast=SystemParameters.HighContrast;
        void Brush(string key,string hex){var brush=new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));brush.Freeze();Resources[key]=brush;}
        void Glass(string key,string top,string bottom)
        {var brush=new LinearGradientBrush((Color)ColorConverter.ConvertFromString(top),(Color)ColorConverter.ConvertFromString(bottom),90);brush.Freeze();Resources[key]=brush;}
        Glass("AtelierGlass",dark?"#F02E4539":"#F0F6F4E9",dark?"#EE23382E":"#E2E8EDE0");
        Glass("AtelierRow",dark?"#F032493B":"#F2F9F8F0",dark?"#F02A4034":"#EBF0F3E8");
        Glass("AtelierSummary",dark?"#ED304839":"#E8F5F5EA",dark?"#E5273A2E":"#DDE9EEDF");
        Brush("AtelierSecondary",dark?"#CC3A5140":"#A6DFE6D6");
        Brush("AtelierAuxiliary",dark?"#50374D3E":"#4DDDE5D6");
        Brush("AtelierMuted",dark?"#C4D4C3":"#56664F");
        Brush("AtelierSuccess",dark?"#F02D4836":"#EDF0F5E7");
        Brush("AtelierEdge",dark?"#40688970":"#70FFFFF5");
        Brush("AtelierField",dark?"#E621362B":"#C9F9FAF1");
        Brush("AtelierSegment",dark?"#B51D3125":"#CCDFE6D6");
        Brush("AtelierNavigation",dark?"#EA1C2922":"#D5F1F3EA");
        Brush("AtelierButton",dark?"#ADC7AA":"#3C6752");
        Brush("AtelierButtonInk",dark?"#172A1E":"#FFFFFF");
        if(contrast)
        {
            foreach(var key in new[]{"AtelierGlass","AtelierRow","AtelierField","AtelierSegment","AtelierNavigation","AtelierSummary","AtelierSecondary","AtelierAuxiliary","AtelierSuccess"})Resources[key]=SystemColors.WindowBrush;
            Resources["AtelierEdge"]=SystemColors.WindowTextBrush;Resources["AtelierMuted"]=SystemColors.WindowTextBrush;
            Resources["AtelierButton"]=SystemColors.HighlightBrush;Resources["AtelierButtonInk"]=SystemColors.HighlightTextBrush;
        }
        if(AtelierScenery!=null)AtelierScenery.Opacity=contrast?0:_atmosphereStrength*(dark?0.15:0.9);
    }
}
