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
    public readonly Dictionary<int, int> Bag = new();

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
        return Register(x.SourceGroup.Id.ToString(), x.Sender.Id.ToString());
    }

    public static Player Register(string groupId, string memberId)
    {
        if (!Players.ContainsKey(groupId))
        {
            Players[groupId] = new();
        }

        if (!Players[groupId].ContainsKey(memberId))
        {
            Players[groupId][memberId] = new();
        }

        return Players[groupId][memberId];
    }

    public bool Buy(int id, int count)
    {
        int price = PointShop[id] * count;
        if (Points < price)
        {
            return false;
        }
        Points -= price;
        Bag.MergeValue(id, count);
        return true;
    }

    public bool Activity(GroupMessageEventArgs eventArgs, int energy)
    {
        if (!CanActivity(eventArgs)) return false;
        _lastActivityUnixTime = GetNowUnixTime();
        if (Pet.Energy < energy)
        {
            //receiver.SendAtMessage("");
            return false;
        }

        Pet.Energy -= energy;
        return true;
    }

    public bool CanActivity(GroupMessageEventArgs receiver)
    {
        return CanActivity(receiver.SourceGroup.Id.ToString(), receiver.Sender.Id.ToString());
    }

    private bool CanActivity(string groupId, string memberId)
    {
        if (GetNowUnixTime() - _lastActivityUnixTime > BreaksTime || (_lastActivityUnixTime == 0))
        {
            return HavePet(groupId, memberId);
        }

        SendAtMessage(groupId, memberId,
            $"时间还没到，距您下一次活动还差[{120 - GetNowUnixTime() + _lastActivityUnixTime}]秒!");
        return false;
    }

    public void EnergyAdd(int energy = 10)
    {
        if (Pet != null && Pet.Energy < Pet.MaxEnergy)
        {
            Pet.Energy += energy;
        }
    }
}