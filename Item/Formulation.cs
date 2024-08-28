namespace OpenPetsWorld.Item;

/// <summary>
/// 物品配方类
/// </summary>
public class Formulation
{
    public int Level = 0;
    public int Experience = 0;
    public int Points = 0;
    public int Bonds = 0;

    public List<FItem> Items = new();
}

public class FItem
{
    public string Name;
    public int Count = 1;

    public static implicit operator FItem(BaseItem item) => new()
    {
        Name = item.Name
    };
}