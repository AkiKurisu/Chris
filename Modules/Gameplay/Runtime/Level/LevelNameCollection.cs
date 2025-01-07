using System;
using Chris.DataDriven;
using UnityEngine;
namespace Chris.Gameplay.Level
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

    [AttributeUsage(AttributeTargets.Field)]
    public class LevelNameAttribute : PopupSelector
    {
        public LevelNameAttribute(): base(typeof(LevelNameCollection))
        {
            
        }
    }

    [CreateAssetMenu(fileName = "LevelNameCollection", menuName = "Chris/Level/LevelNameCollection")]
    public class LevelNameCollection : PopupSet
    {

    }
}