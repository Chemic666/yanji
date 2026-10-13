using System;
using System.Runtime.InteropServices;
using YanJi.Core;

namespace YanJi.App;

public static class WindowsClock
{
    private static readonly string Token = Guid.NewGuid().ToString("N");
    [DllImport("kernel32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryUnbiasedInterruptTime(out ulong ticks);
    [DllImport("kernel32.dll")] private static extern ulong GetTickCount64();
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void PreciseQuery(out ulong ticks);
    private static readonly PreciseQuery? AwakeQuery=FindPrecise("QueryUnbiasedInterruptTimePrecise");
    private static readonly PreciseQuery? BootQuery=FindPrecise("QueryInterruptTimePrecise");
    public static string Sources=>$"awake={(AwakeQuery!=null?"precise unbiased":"unbiased interrupt")}; boot={(BootQuery!=null?"precise interrupt":"GetTickCount64")}";
    private static PreciseQuery? FindPrecise(string name)
    {
        foreach(var library in new[]{"kernel32.dll","kernelbase.dll"})
        {
            if(NativeLibrary.TryLoad(library,typeof(WindowsClock).Assembly,DllImportSearchPath.System32,out var handle)&&NativeLibrary.TryGetExport(handle,name,out var address))
                return Marshal.GetDelegateForFunctionPointer<PreciseQuery>(address);
        }
        return null;
    }
    public static ClockSample Sample()
    {
        ulong awake,boot;
        if(AwakeQuery!=null)AwakeQuery(out awake);
        else if(!QueryUnbiasedInterruptTime(out awake))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        if(BootQuery!=null)BootQuery(out boot);else boot=checked(GetTickCount64()*10000);
        return new(DateTimeOffset.UtcNow, checked((long)(awake/10000)), checked((long)(boot/10000)), Token);
    }
}
