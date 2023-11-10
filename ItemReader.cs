using Newtonsoft.Json;
using OpenPetsWorld.Item;

namespace OpenPetsWorld;

public static class ItemReader
{
    public static Dictionary<int, BaseItem>? Read(string path)
    {
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new ItemConverter());

        var dictionary = JsonConvert.DeserializeObject<Dictionary<int, BaseItem>>(json, settings);
        return dictionary;
    }

    /*private static Dictionary<int, BaseItem?> Convert(this JObject jsonObject, Dictionary<int, BaseItem> dictionary)
    {
        return dictionary.ToDictionary(k => k.Key, k =>
        {
            BaseItem input = k.Value;
            BaseItem? output = input.ItemType switch
            {
                ItemType.Material => jsonObject.ToObject<Material>(),
                ItemType.Artifact => jsonObject.ToObject<Artifact>(),
                ItemType.Resurrection => jsonObject.ToObject<Resurrection>(),
                ItemType.Recovery => jsonObject.ToObject<Recovery>(),
                ItemType.Gain => jsonObject.ToObject<Material>(),
                _ => throw new JsonSerializationException("Unknown object type")
            };
            if (output != null)
            {
                Debug.WriteLine(1);
            }
            return output;
        });
    }*/
}