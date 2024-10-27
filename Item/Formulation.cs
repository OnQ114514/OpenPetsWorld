namespace OpenPetsWorld.Item;

/// <summary>
/// 物品配方类
/// </summary>
public class Formulation
{
    public int Level = 0;
    public long Experience = 0;
    public long Points = 0;
    public long Bonds = 0;

    public double Probability;
    public List<FItem> Items = [];
}

public class FItem
{
    public string Name;
    public long Count = 1;

    public static implicit operator FItem(BaseItem item) => new()
    {
        Name = item.Name
    };
}