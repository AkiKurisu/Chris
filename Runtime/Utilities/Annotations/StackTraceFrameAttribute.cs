using System;
namespace Chris
{
    /// <summary>
    /// Notify framework stack trace to trace frame use this method or constructor
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
    public sealed class StackTraceFrameAttribute : Attribute
    {

    }
}