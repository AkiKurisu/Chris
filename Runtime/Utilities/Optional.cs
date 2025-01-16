using System;
using UnityEngine;
namespace Chris
{
    /// <summary>
    /// A replacement of <see cref="Nullable{T}"/> that can be edited in the inspector
    /// </summary>
    [Serializable]
    public struct Optional<T>
    {
        [SerializeField] private bool enabled;
        [SerializeField] private T value;

        public readonly bool Enabled => enabled;
        public readonly T Value => value;

        public Optional(T initialValue)
        {
            enabled = true;
            value = initialValue;
        }
    }
}
