namespace OpenPetsWorld;

public class GameConfig
{
    /// <summary>
    /// 修炼地点名称
    /// </summary>
    public string[] Places;
    /// <summary>
    /// 活动冷却时间
    /// </summary>
    public int BreaksTime;

    public int MaxIqAdd;
    public int MinIqAdd;
    public int MaxAttrAdd;
    public int MinAttrAdd;
    public int MaxExpAdd;
    public int MinExpAdd;

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