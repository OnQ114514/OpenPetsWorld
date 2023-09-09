namespace OpenPetsWorld.Item;

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ItemConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return typeof(Dictionary<int, BaseItem>).IsAssignableFrom(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var dictionary = new Dictionary<int, BaseItem>();
        JObject jsonObject = JObject.Load(reader);
        var origin = jsonObject.ToObject<Dictionary<int, object>>();

        foreach (var key in origin.Keys)
        {
            var token = jsonObject[key.ToString()];
            var type = token["ItemType"].ToObject<ItemType>();
            
            BaseItem? value = type switch
            {
                ItemType.Material => token.ToObject<Material>(),
                ItemType.Artifact => token.ToObject<Artifact>(),
                ItemType.Resurrection => token.ToObject<Resurrection>(),
                ItemType.Recovery => token.ToObject<Recovery>(),
                ItemType.Gain => token.ToObject<Gain>(),
                ItemType.Pet => token.ToObject<PetItem>(),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            dictionary.Add(key, value);
        }

        return dictionary;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}
