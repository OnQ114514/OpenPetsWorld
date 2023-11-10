using OpenPetsWorld.Item;

namespace OpenPetsWorld.Extra;

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
    public int Id;
    public int Count = 1;

    public static implicit operator FItem(BaseItem item)
    {
        return new()
        {
            Id = item.Id
        };
    }
}