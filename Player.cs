using Mirai.Net.Data.Messages.Receivers;
using OpenPetsWorld.PetTool;
using static OpenPetsWorld.Program;
using static OpenPetsWorld.OpenPetsWorld;

namespace OpenPetsWorld;

public class Player
{
    /// <summary>
    /// 积分
    /// </summary>
    public int Points;

    /// <summary>
    /// 点券
    /// </summary>
    public int Bonds = 0;

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

    public List<int> ClaimedGifts = new();

    public long LastActivityUnixTime = 0;

    public static Player Register(GroupMessageReceiver x)
    {
        return Register(x.GroupId, x.Sender.Id);
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

    public bool Activity(GroupMessageReceiver receiver, int energy)
    {
        if (!CanActivity(receiver)) return false;
        LastActivityUnixTime = GetNowUnixTime();
        if (Pet.Energy < energy)
        {
            //receiver.SendAtMessage("");
            return false;
        }
        
        Pet.Energy -= energy;
        return true;
    }
    
    public bool CanActivity(GroupMessageReceiver receiver)
    {
        return CanActivity(receiver.GroupId, receiver.Sender.Id);
    }

    private bool CanActivity(string groupId, string memberId)
    {
        if (GetNowUnixTime() - LastActivityUnixTime > BreaksTime || (LastActivityUnixTime == 0))
        {
            return HavePet(groupId, memberId);
        }

        SendAtMessage(groupId, memberId,
            $"时间还没到，距您下一次活动还差[{120 - GetNowUnixTime() + LastActivityUnixTime}]秒!");
        return false;
    }

    public void EnergyAdd()
    {
        if (Pet != null && Pet.Energy < Pet.MaxEnergy)
        {
            Pet.Energy++;
        }
    }
}