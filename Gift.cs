using OpenPetsWorld.Item;

namespace OpenPetsWorld;

public class Gift
{
    public int Id;
    public string Name;
    public int Level;
    public readonly List<FItem> Items = new();
}