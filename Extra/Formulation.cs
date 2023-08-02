using OpenPetsWorld.Item;

namespace OpenPetsWorld.Extra;

/// <summary>
/// 物品配方类，具有非原本功能
/// </summary>
public class Formulation : List<FItem>
{
    public int Level = 0;
    public int Experience = 0;
    public int Points = 0;
    public int Bonds = 0;
}

public class FItem
{
    public int Id;
    public int Count = 1;

    public static implicit operator FItem(BaseItem item)
    {
        return new FItem()
        {
            Id = item.Id
        };
    }
}