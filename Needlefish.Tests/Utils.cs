using System;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Needlefish.Tests
{
    public class Utils
    {
        public static void Benchmark(ITestOutputHelper output, Action action)
        {            
            long memory = GC.GetTotalMemory(true);
            Stopwatch sw = Stopwatch.StartNew();
            action.Invoke();
            sw.Stop();
            memory = GC.GetTotalMemory(true) - memory;

            output.WriteLine($"Elapsed: {(double) sw.ElapsedTicks / TimeSpan.TicksPerMillisecond}ms, Memory: {memory}b ({memory / 1000d}kb)");
        }
    }
}
