using OpenPetsWorld.Extra;

namespace OpenPetsWorld;

public class Replica
{
    /// <summary>
    /// 所需等级
    /// </summary>
    public readonly int Level = 0;
    /// <summary>
    /// 副本名
    /// </summary>
    public string Name;
    /// <summary>
    /// 奖励物品
    /// </summary>
    private readonly Dictionary<int, int> _rewardingItems = new();
    /// <summary>
    /// 最大奖励积分
    /// </summary>
    private readonly int _maxPoint = 0;
    /// <summary>
    /// 最小奖励积分
    /// </summary>
    private readonly int _minPoint = 0;
    /// <summary>
    /// 最大经验
    /// </summary>
    private readonly int _maxExp = 0;
    /// <summary>
    /// 最小经验
    /// </summary>
    private readonly int _minExp = 2;
    /// <summary>
    /// 敌人名
    /// </summary>
    public string enemyName;
    /// <summary>
    /// 敌人攻击
    /// </summary>
    public int Attack;
    /// <summary>
    /// 消耗精力
    /// </summary>
    public readonly int Energy = 10;
    /// <summary>
    /// 敌人图片
    /// </summary>
    public readonly string? IconName = null;
    /// <summary>
    /// 消耗物品
    /// </summary>
    private readonly FItem? _neededItem = null;

    public int Challenge(Player player, int count, out int expAdd, out int pointAdd)
    {
        expAdd = 0;
        pointAdd = 0;

        if (_neededItem != null)
        {
            if (!player.Bag.TryGetValue(_neededItem.Id, out var value))
            {
                return 1;
            }

            if (value < _neededItem.Count * count)
            {
                return 1;
            }
        }

        var pet = player.Pet;
        if (pet == null || pet.Energy < count * Energy)
        {
            return 2;
        }

        pet.Health -= pet.Damage(Attack) * count;
        pet.Energy -= Energy * count;
        pet.Experience += Program.Random.Next(_minExp, _maxExp) * count;
        player.Points += Program.Random.Next(_minPoint, _maxPoint) * count;
        foreach (var item in _rewardingItems)
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

        return 0;
    }
}