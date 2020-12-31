using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace OrleansDashboard
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
    public sealed class NoProfilingAttribute : Attribute
    {
    }

    
}
