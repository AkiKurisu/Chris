using System;

namespace Chris.Configs
{
    /// <summary>
    /// Define config variable can be edited through console.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ConsoleVariableAttribute : Attribute
    {
        public string Name { get; private set; }

        public ConsoleVariableAttribute(string name)
        {
            Name = name;
        }
    }
}
