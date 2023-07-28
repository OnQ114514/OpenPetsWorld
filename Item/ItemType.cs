namespace OpenPetsWorld.Item;

public enum ItemType
{
    Material,
    Artifact,
    Resurrection,
    Recovery,
    Gain
}

public static class Converter
{
    public static string ToStr(this ItemType type)
    {
        return type switch
        {
            ItemType.Material => "材料",
            ItemType.Artifact => "神器",
            ItemType.Resurrection => "复活",
            ItemType.Recovery => "恢复",
            ItemType.Gain => "增益",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}