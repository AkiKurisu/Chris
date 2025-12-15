#if !CERES_INSTALL
using System;

namespace Ceres.Graph.Flow.Annotations
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class ExecutableFunctionAttribute : Attribute
    {
        
    }
    
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class ImplementableEventAttribute : Attribute
    {

    }
    
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class ResolveReturnAttribute : Attribute
    {
        
    }
}
#endif