using Newtonsoft.Json;
using OpenPetsWorld.PetTool;
using Sora.EventArgs.SoraEvent;
using static OpenPetsWorld.Game;
using static OpenPetsWorld.Program;

namespace OpenPetsWorld;

public class Player
{
    /// <summary>
    /// 积分
    /// </summary>
    public long Points;

    /// <summary>
    /// 点券
    /// </summary>
    public long Bonds = 0;

    /// <summary>
    /// 累签
    /// </summary>
    public int SignedDays = 0;

    /// <summary>
    /// 连签
    /// </summary>
    public int ContinuousSignedDays = 0;

    /// <summary>
    /// 上次签到时间
    /// </summary>
    public long LastSignedUnixTime = 0;

    /// <summary>
    /// 背包
    /// </summary>
    public readonly Dictionary<string, long> Bag = new();

    /// <summary>
    /// 宠物
    /// </summary>
    public Pet? Pet;

    /// <summary>
    /// 已领取的礼包ID
    /// </summary>
    public readonly List<int> ClaimedGifts = new();

    /// <summary>
    /// 上次活动时间
    /// </summary>
    [JsonIgnore] private long _lastActivityUnixTime;

    /// <summary>
    /// 砸蛋十连抽到的宠物
    /// </summary>
    [JsonIgnore]
    public List<Pet>? GachaPets = null;

    /// <summary>
    /// 发送放生的时间
    /// </summary>
    [JsonIgnore]
    public long SentFreeUnixTime;

    public static Player Register(GroupMessageEventArgs x)
    {
        return Register(x.SourceGroup.Id, x.Sender.Id);
    }

    public static Player Register(long groupId, long senderId)
    {
        if (!Players.TryGetValue(groupId, out var group))
        {
            group = new Dictionary<long, Player>();
            Players[groupId] = group;
        }

        if (group.TryGetValue(senderId, out var player)) return player;
        
        player = new Player();
        group[senderId] = player;

        return player;
    }

    public bool Buy(string name, long count)
    {
        var price = PointShop[name] * count;
        if (Points < price)
        {
            return false;
        }
        Points -= price;
        Bag.MergeValue(name, count);
        return true;
    }

    public bool Activity(GroupMessageEventArgs eventArgs, int energy)
    {
        if (!CanActivity(eventArgs)) return false;
        _lastActivityUnixTime = GetNowUnixTime();
        if (Pet.Energy < energy) return false;

        Pet.Energy -= energy;
        return true;
    }

    private bool CanActivity(GroupMessageEventArgs eventArgs)
    {
        return CanActivity(eventArgs.SourceGroup.Id, eventArgs.Sender.Id);
    }

    private bool CanActivity(long groupId, long senderId)
    {
        if (GetNowUnixTime() - _lastActivityUnixTime > PlayConfig.BreaksTime || (_lastActivityUnixTime == 0))
        {
            return HavePet(groupId, senderId);
        }

        SendAtMessage(groupId, senderId,
            $"时间还没到，距您下一次活动还差[{120 - GetNowUnixTime() + _lastActivityUnixTime}]秒!");
        return false;
    }

    public void EnergyAdd(long energy)
    {
        if (Pet != null && Pet.Energy < Pet.MaxEnergy)
        {
            Pet.Energy += energy;
        }
    }
}