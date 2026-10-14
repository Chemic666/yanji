using System;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using YanJi.Core;
namespace YanJi.App;

/// <summary>Presentation from original milliseconds; never reads formatted text back into data.</summary>
public sealed class NaturalDuration : TextBlock
{
    public static readonly DependencyProperty MillisecondsProperty=DependencyProperty.Register(nameof(Milliseconds),typeof(long),typeof(NaturalDuration),new PropertyMetadata(0L,(d,_)=>((NaturalDuration)d).RenderDuration()));
    public long Milliseconds {get=>(long)GetValue(MillisecondsProperty);set=>SetValue(MillisecondsProperty,value);}
    public NaturalDuration(){Loaded+=(_,_)=>RenderDuration();}
    private void RenderDuration()
    {
        var ms=Math.Max(0,Milliseconds);Inlines.Clear();
        void Part(string number,string unit){Inlines.Add(new Run(number){FontWeight=FontWeights.SemiBold});Inlines.Add(new Run(" "+unit+" "){FontSize=FontSize*.55,FontWeight=FontWeights.Normal});}
        if(ms is >0 and <1000){Inlines.Add(new Run("不足 "){FontSize=FontSize*.65});Part("1","秒");}
        else if(ms<60000){Part((ms/1000).ToString(CultureInfo.InvariantCulture),"秒");}
        else
        {
            var hours=ms/3600000;var minutes=ms/60000%60;var seconds=ms%60000/1000;
            if(hours>0)Part(hours.ToString(CultureInfo.InvariantCulture),"小时");
            if(minutes>0)Part(minutes.ToString(CultureInfo.InvariantCulture),"分");
            if(seconds>0)Part(seconds.ToString(CultureInfo.InvariantCulture),"秒");
        }
        AutomationProperties.SetName(this,DurationText.Format(ms));
    }
}
