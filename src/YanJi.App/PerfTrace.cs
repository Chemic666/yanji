using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
namespace YanJi.App;
// Opt-in local diagnostics. No file IO during measured UI operations.
internal static class PerfTrace
{
    internal sealed record Event(string Name,long Start,double Ms,int StartThread,int EndThread,long AllocatedBytes,int Gen0,int Gen1,int Gen2);
    internal static bool Enabled;
    internal static readonly ConcurrentQueue<Event> Events=new();
    internal static IDisposable? Span(string name)=>Enabled?new Scope(name):null;
    private sealed class Scope(string name):IDisposable
    {
        private readonly long start=Stopwatch.GetTimestamp(),allocated=GC.GetTotalAllocatedBytes();
        private readonly int thread=Environment.CurrentManagedThreadId,g0=GC.CollectionCount(0),g1=GC.CollectionCount(1),g2=GC.CollectionCount(2);
        public void Dispose()=>Events.Enqueue(new(name,start,Stopwatch.GetElapsedTime(start).TotalMilliseconds,thread,Environment.CurrentManagedThreadId,GC.GetTotalAllocatedBytes()-allocated,GC.CollectionCount(0)-g0,GC.CollectionCount(1)-g1,GC.CollectionCount(2)-g2));
    }
}
