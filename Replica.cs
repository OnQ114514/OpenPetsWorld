namespace OpenPetsWorld;

public class Replica
{
    public int Level = 0;
    public string Name;
    public Dictionary<int, int> RewardingItems = new();
    public int RewardingPoint = 0;
    public int ExpAdd;
    public string enemyName;
    public int Attack;
    public int Energy = 10;
    public string? IconName = null;

    public bool Challenge(Player player, int count)
    {
        if (player.Pet == null || player.Pet.Energy < count * Energy)
        {
            return false;
        }

        player.Pet.Health -= Attack * count;
        player.Pet.Energy -= Energy * count;
        player.Pet.Experience += ExpAdd * count;
        player.Points += RewardingPoint * count;
        foreach (var item in RewardingItems)
        {
            if (!player.Bag.ContainsKey(item.Key))
            {
                player.Bag[item.Key] = item.Value;
            }
            else
            {
                player.Bag[item.Key] += item.Value;
            }
        }

        return true;
    }
}