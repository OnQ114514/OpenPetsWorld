using Mirai.Net.Data.Messages.Receivers;
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
    public Dictionary<int, int> BagItems = new();

    /// <summary>
    /// 宠物
    /// </summary>
    public Pet? pet;

    public long LastActivityUnixTime = 0;

    public static Player Register(GroupMessageReceiver x)
    {
        return Register(x.GroupId, x.Sender.Id);
    }

    public static Player Register(string GroupId, string MemberId)
    {
        if (!Players.ContainsKey(GroupId))
        {
            Players[GroupId] = new();
        }

        if (!Players[GroupId].ContainsKey(MemberId))
        {
            Players[GroupId][MemberId] = new();
        }

        return Players[GroupId][MemberId];
    }

    public bool CanActivity(GroupMessageReceiver receiver)
    {
        return CanActivity(receiver.GroupId, receiver.Sender.Id);
    }

    public bool CanActivity(string GroupId, string MemberId)
    {
        if (GetNowUnixTime() - LastActivityUnixTime > 120 || (LastActivityUnixTime == 0))
        {
            return HavePet(GroupId, MemberId);
        }

        SendAtMessage(GroupId, MemberId,
            $"时间还没到，距您下一次活动还差[{120 - GetNowUnixTime() + LastActivityUnixTime}]秒!");
        return false;
    }

    public void EnergyAdd()
    {
        if (pet != null && pet.Energy < pet.MaxEnergy)
        {
            pet.Energy++;
        }
    }
}