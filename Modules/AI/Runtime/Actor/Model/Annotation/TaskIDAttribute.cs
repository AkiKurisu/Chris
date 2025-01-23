using System;
using UnityEngine;
namespace Chris.AI
{
    public class TaskIDAttribute : PropertyAttribute { }
    
    /// <summary>
    /// Create bridge between popUpSet and custom task id
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TaskIDHostAttribute : Attribute { }
}
