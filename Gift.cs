using OpenPetsWorld.Extra;

namespace OpenPetsWorld;

public class Gift
{
    public int Id;
    public string Name;
    public int Level;
    public List<FItem> Items = new();
}