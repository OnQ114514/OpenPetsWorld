using OpenPetsWorld.Item;

namespace OpenPetsWorld;

/// <summary>
/// 副本类
/// </summary>
public class Instance
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
    public readonly List<Reward> Rewards = [];

    /// <summary>
    /// 单次奖励积分
    /// </summary>
    public readonly long Points = 0;

    /// <summary>
    /// 单次奖励经验
    /// </summary>
    public readonly long Experience = 0;

    /// <summary>
    /// 敌人名
    /// </summary>
    public string EnemyName;

    /// <summary>
    /// 敌人攻击
    /// </summary>
    public readonly long EnemyAttack = 0;

    /// <summary>
    /// 消耗精力
    /// </summary>
    public readonly long Energy = 10;

    /// <summary>
    /// 副本图片
    /// </summary>
    public readonly string? IconName = null;

    /// <summary>
    /// 消耗物品
    /// </summary>
    public readonly FItem? NeededItem = null;

    public InstanceResult Challenge(Player player, long count)
    {
        var result = new InstanceResult();
        
        if (NeededItem != null)
        {
            var allItemCount = NeededItem.Count * count; 
            // 进入所需物品不足
            if (!player.Bag.TryGetValue(NeededItem.Name, out var value) || value < allItemCount)
            {
                result.Code = 1;
                return result;
            }
        }

        var pet = player.Pet;
        if (pet == null || pet.Energy < count * Energy)
        {
            result.Code = 2;
            return result;
        }
        
        pet.Health -= result.Damage = pet.Damage(EnemyAttack) * count;
        pet.Energy -= result.Energy = Energy * count;
        pet.Experience += result.Experience = Experience * count;
        player.Points += result.Points = Points * count;
        
        foreach (var reward in from reward in Rewards let get = reward.ProbabilityGet() where get select reward)
        {
            player.Bag.MergeValue(reward.Name, reward.Count);
            result.Items.Add(new FItem
            {
                Name = reward.Name,
                Count = reward.Count
            });
        }

        return result;
    }
}

public class Reward
{
    public string Name;
    public long Count;
    public double Probability = 1;

    public bool ProbabilityGet()
    {
        if (Probability >= 1) return true;

        // 生成 0 到 99 之间的随机数  
        var randomNumber = Program.Random.Next(0, 100);  
        // 判断生成的随机数是否小于指定的概率  
        var result = randomNumber < Probability * 100;
        return result;
    }
}

public class InstanceResult
{
    public int Code;
    public long Energy;
    public long Points;
    public long Experience;
    public long Damage;
    public List<FItem> Items = [];
    //TODO:替换为List<State>
    public string PetState = "正常";
}