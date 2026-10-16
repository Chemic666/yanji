using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
namespace YanJi.App;
/// <summary>One composition-aware routed save command per editor scope; never a timer shortcut.</summary>
internal sealed class SaveShortcuts
{
    private static readonly DependencyProperty ExcludedProperty=DependencyProperty.RegisterAttached("Excluded",typeof(bool),typeof(SaveShortcuts),new PropertyMetadata(false));
    internal static void SetExcluded(DependencyObject o,bool value)=>o.SetValue(ExcludedProperty,value);
    internal static readonly RoutedUICommand Save=new("保存","SaveEditor",typeof(SaveShortcuts));
    private readonly FrameworkElement _scope;private readonly Func<Task> _save;private readonly Func<bool> _canSave;
    private bool _composing,_finishing,_executing;
    internal SaveShortcuts(FrameworkElement scope,Func<Task> save,Func<bool>? canSave=null)
    {
        _scope=scope;_save=save;_canSave=canSave??(()=>true);
        TextCompositionManager.AddPreviewTextInputStartHandler(scope,(_,_)=>_composing=true);
        TextCompositionManager.AddPreviewTextInputUpdateHandler(scope,(_,_)=>_composing=true);
        TextCompositionManager.AddPreviewTextInputHandler(scope,(_,_)=>{_composing=false;_finishing=true;scope.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,new Action(()=>_finishing=false));});
        scope.LostKeyboardFocus+=(_,_)=>{_composing=false;};
        scope.CommandBindings.Add(new CommandBinding(Save,async(_,e)=>{e.Handled=true;if(!CanSave)return;_executing=true;try{await _save();}finally{_executing=false;}},(_,e)=>{e.CanExecute=CanSave;e.Handled=true;}));
        scope.PreviewKeyDown+=(_,e)=>{if(TrySave(e.Key,Keyboard.Modifiers,e.OriginalSource as DependencyObject))e.Handled=true;};
    }
    private bool CanSave=>!_composing&&!_finishing&&!_executing&&_canSave();
    internal bool TrySave(Key key,ModifierKeys modifiers,DependencyObject? source)
    {
        // ImeProcessed/DeadCharProcessed stay with WPF/TSF. No default button bypasses this gate.
        var text=source as TextBox??Keyboard.FocusedElement as TextBox;
        if(key!=Key.Return||!CanSave||text==null||text.IsReadOnly||(bool)text.GetValue(ExcludedProperty)||!_scope.IsAncestorOf(text))return false;
        for(DependencyObject? p=text;p!=null&&p!=_scope;p=System.Windows.Media.VisualTreeHelper.GetParent(p))if(p is ComboBox)return false;
        var save=modifiers==ModifierKeys.Control||modifiers==ModifierKeys.None&&!text.AcceptsReturn;
        if(!save)return false;text.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();Save.Execute(null,_scope);return true;
    }
}
