namespace OpenPetsWorld;

public class GameConfig
{
    /// <summary>
    /// 修炼地点名称
    /// </summary>
    public string[] Places = [];

    /// <summary>
    /// 活动冷却时间
    /// </summary>
    public int BreaksTime;

    /// <summary>
    /// 单次最多使用物品数量
    /// </summary>
    public int MaxUsedItem = 99999;

    /// <summary>
    /// 精力恢复间隔时间（tick）
    /// </summary>
    public int EnergyAddTime = 600000;

    /// <summary>
    /// 精力恢复数量
    /// </summary>
    public long EnergyAddCount = 10;

    /// <summary>
    /// 宠物侦察所需精力
    /// </summary>
    public long ViewNeededEnergy = 5;

    public int MaxIqAdd;
    public int MinIqAdd;
    public int MaxAttrAdd;
    public int MinAttrAdd;
    public int MaxExpAdd;
    public int MinExpAdd;

    public int MaxMood = 50;
    public int MaxLevel = 300;

    /// <summary>
    /// 怪物入侵（如果为false则全服都关闭）
    /// </summary>
    public static bool BossIntruding = false;

    /// <summary>
    /// 单次砸蛋所需积分
    /// </summary>
    public int GachaPoint;

    /// <summary>
    /// 宠物神榜刷新时间
    /// </summary>
    public static string UpdateTime = "";
}