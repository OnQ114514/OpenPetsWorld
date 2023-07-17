using Newtonsoft.Json;
using OpenPetsWorld.Item;

namespace OpenPetsWorld;

public static class ItemReader
{
    public static Dictionary<int, BaseItem>? Read(string path)
    {
        if (!File.Exists(path)) return null;
        string json = File.ReadAllText(path);
        var originDict = JsonConvert.DeserializeObject<Dictionary<int, BaseItem>>(json);
        if (originDict == null) return null;
        return originDict.Convert();
    }

    private static Dictionary<int, BaseItem> Convert(this Dictionary<int, BaseItem> dictionary)
    {
        return dictionary.ToDictionary(k => k.Key, k =>
        {
            BaseItem input = k.Value;
            return (BaseItem)(input.ItemType switch
            {
                ItemType.Material => (Material)input,
                ItemType.Artifact => (Artifact)input,
                ItemType.Resurrection => (Resurrection)input,
                ItemType.Recovery => (Recovery)input,
                ItemType.Gain => (Gain)input,
                _ => throw new ArgumentOutOfRangeException()
            });
        });
    }
}