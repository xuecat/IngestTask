using System;
using System.Runtime.CompilerServices;

namespace OrleansDashboard.Metrics
{
    public interface IGrainProfiler
    {
        void Track(double elapsedMs, Type grainType, string identity, [CallerMemberName] string methodName = null, bool failed = false);
    }
}
