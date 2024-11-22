using System;
using UnityEngine;
namespace Kurisu.Framework.Level
{
    public enum LoadLevelMode
    {
        Single,
        Additive,
        Dynamic
    }
    
    [Flags]
    public enum LoadLevelPolicy
    {
        Never = 0,
        PC = 2,
        Mobile = 4,
        Console = 8,
        AllPlatform = PC | Mobile | Console
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class LevelNameAttribute : PopupSelector
    {
        public LevelNameAttribute(): base(typeof(LevelNameCollection))
        {
            
        }
    }

    [CreateAssetMenu(fileName = "LevelNameCollection", menuName = "AkiFramework/Level/LevelNameCollection")]
    public class LevelNameCollection : PopupSet
    {

    }
}