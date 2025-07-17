using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
namespace R3.Chris
{
    /// <summary>
    /// Serialization helper for <see cref="ReactiveProperty{T}"/> when use <see cref="JsonConverter"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ReactivePropertyConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ReactiveProperty<T>);
        }
        
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            try
            {
                var token = JToken.Load(reader);
                var value = token.ToObject<T>();
                return new ReactiveProperty<T>(value);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return new ReactiveProperty<T>();
            }
        }
        
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, (value as ReactiveProperty<T>)!.Value);
        }
    }
}