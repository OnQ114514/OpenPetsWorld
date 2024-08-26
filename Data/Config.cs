using Newtonsoft.Json;

namespace OpenPetsWorld.Data;

public class Config
{
    /// <summary>
    /// 群列表
    /// </summary>
    public List<string> GroupList = new();
    /// <summary>
    /// 在群列表，但不运行的群（受开/关OPW命令影响）
    /// </summary>
    public HashSet<string> NotRunningGroup = new();
    /// <summary>
    /// 黑名单模式（默认关闭）
    /// </summary>
    public bool BlackListMode = false;
    /// <summary>
    /// 主人QQ号
    /// </summary>
    public string MasterId = "";
    /// <summary>
    /// 管理员QQ号列表
    /// </summary>
    public List<string> Admins = new();
    /// <summary>
    /// 启动文字（开OPW）
    /// </summary>
    public string BootText = "TAKE OFF TOWARD THE DREAM";
    
    /// <summary>
    /// 公开至局域网（默认关闭）
    /// </summary>
    public bool LanPublic = false;
    /// <summary>
    /// Websocket端口
    /// </summary>
    public ushort Port = 8080;
    /// <summary>
    /// 使用反向Websocket
    /// </summary>
    public bool ReverseWebsocket = true;
    /// <summary>
    /// 正向Websocket连接地址
    /// </summary>
    public string Host = "127.0.0.1";
    
    /// <summary>
    /// 配置是否被更改
    /// </summary>
    [JsonIgnore]
    public bool Changed = false;
}