using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Manganese.Text;
using Newtonsoft.Json;
using OpenPetsWorld.Data;
using OpenPetsWorld.Item;
using SkiaSharp;
using Sora;
using Sora.Entities;
using Sora.Entities.Base;
using Sora.Entities.Segment.DataModel;
using Sora.Enumeration;
using Sora.EventArgs.SoraEvent;
using Sora.Interfaces;
using Sora.Net.Config;
using Sora.Util;
using YukariToolBox.LightLog;
using static OpenPetsWorld.Game;
using Timer = System.Timers.Timer;

namespace OpenPetsWorld;

internal static class Program
{
    public static SKFontStyleSet FontStyleSet;
    private static Config _config = new();
    public static readonly Cache Cache = new();

    public static readonly HttpClient HttpClient = new();
    public static readonly Random Random = new();
    private static SoraApi? _soraApi;

    private static async Task Main()
    {
        Console.Title = "OpenPetWorld控制台";

        //设置log等级
        Log.LogConfiguration.EnableConsoleOutput().SetLogLevel(LogLevel.Info);

        const string configPath = "./config.json";
        if (File.Exists(configPath))
        {
            #region ReadConfig

            try
            {
                ReadConfig();
            }
            catch
            {
                File.Delete(configPath);
                Console.WriteLine("读取配置文件错误！已删除配置文件");
                KeysExit();
            }

            #endregion ReadConfig
        }
        else
        {
            #region Initialize

            Log.Info("Main", "未检测到配置文件，开始初始化，请保持开启Mirai");
            Initialize();

            #endregion Initialize
        }

        Console.WriteLine(
            "===================================[ OpenPetsWorld Pre.3 ]===================================");
        Console.WriteLine(
            "\r\n  ___                       _____        _         _    _               _      _ \r\n / _ \\                     | ___ \\      | |       | |  | |             | |    | |\r\n/ / \\ \\ _ __    ___  _ __  | |_/ |  ___ | |_  ___ | |  | |  ___   _ __ | |  __| |\r\n| | | || '_ \\  / _ \\| '_ \\ |  ___/ / _ \\| __|/ __|| |/\\| | / _ \\ |  __|| | / _  |\r\n\\ \\_/ /| |_) | | __/| | | || |     | __/| |_ \\__ \\\\  /\\  /| (_) || |   | | |(_| |\r\n \\___/ | .__/  \\___||_| |_||_|     \\___| \\__||___/ \\/  \\/  \\___/ |_|   |_| \\____|\r\n       | |                                                                       \r\n       |_|                                                                       ");

        #region Start

        var host = "127.0.0.1";
        if (_config.LanPublic)
        {
            var localHost = GetLocalIpAddress();
            if (localHost == null)
            {
                Log.Error("Main", "无法公开至局域网，你的网卡是否清醒？");
            }
            else
            {
                host = localHost;
            }
        }

        //实例化Sora服务
        var service = SoraServiceFactory.CreateService(GetSoraConfig(host));

        #endregion Start

        //写入配置文件
        if (!File.Exists(configPath)) WriteConfig();

        // 获取字体集合
        var families = SKFontManager.Default.FontFamilies.ToList();
        // 获取下标
        var index = families.FindIndex(font => font == _config.Font);
        if (index != -1)
        {
            // 创建字形
            FontStyleSet = SKFontManager.Default.GetFontStyles(index);
        }
        else
        {
            Log.Error("Main", $"字体 {_config.Font} 不存在！您可尝试更改 {configPath} 里的Font项为已安装字体，或安装 {_config.Font}");
            Console.Write($"是否打开 {configPath} ？(y/N)");
            var result = Console.ReadLine(); 
            if (!result.IsNullOrEmpty() && result.Equals("y", StringComparison.CurrentCultureIgnoreCase))
            {
                Process.Start(configPath);
            }
        }

        ReadData();

        //TODO:支持自定义
        //精力恢复事件
        Timer timer = new(600000)
        {
            Enabled = true,
            AutoReset = true
        };
        timer.Elapsed += EnergyRecovery;

        //获取SoraApi
        service.Event.OnClientConnect += (_, eventArgs) =>
        {
            _soraApi = eventArgs.SoraApi;
            return ValueTask.CompletedTask;
        };

        service.Event.OnGroupMessage += (_, eventArgs) => OnMessage(eventArgs);
        await service.StartService().RunCatch(e => Log.Error("Sora Service", Log.ErrorLogBuilder(e)));

        for (;;)
        {
            ConsoleCommand();
        }
    }

    private static bool Filter(GroupMessageEventArgs x)
    {
        if (_config.UserBlackList.Contains(x.Sender.Id)) return false;

        var whiteList = _config.GroupList.Contains(x.SourceGroup.Id.ToString());
        return _config.BlackListMode ? !whiteList : whiteList;
    }

    private static async ValueTask OnMessage(GroupMessageEventArgs eventArgs)
    {
        if (!Filter(eventArgs)) return;

        var groupId = eventArgs.SourceGroup.Id;
        var senderId = eventArgs.Sender.Id;
        var senderName = (await eventArgs.Sender.GetUserInfo()).userInfo.Nick;
        var context = eventArgs.Message;
        var textMessage = context.GetText();

        var notRunning = _config.NotRunningGroup.Contains(groupId);
        if (textMessage == "开OPW" && HavePermissions(senderId) && notRunning)
        {
            _config.NotRunningGroup.Remove(groupId);
            await eventArgs.SendAtMessage($"本群已开启文字游戏OpenPetsWorld[本游戏完全开源]\n{_config.BootText}");
        }

        if (notRunning) return;

        switch (textMessage)
        {
            case "OpenPetsWorld":
                await eventArgs.Reply("由 OpenPetsWorld 核能驱动！");
                break;

            case "关OPW":
                if (HavePermissions(senderId))
                {
                    _config.NotRunningGroup.Add(groupId);
                    await eventArgs.SendAtMessage("已关闭OpenPetsWorld");
                }

                break;

            case "宠物世界":
            {
                const string menuPath = "./datapack/menu.png";

                if (!File.Exists(menuPath))
                {
                    Log.Warning("OnMessage", "菜单图片不存在");
                    break;
                }

                using var image = SKBitmap.Decode(menuPath);

                await eventArgs.Reply(new MessageBodyBuilder().Image(image).Build());
                break;
            }
            case "我的宠物":
            {
                if (HavePet(eventArgs, out var pet))
                {
                    var image = pet.Render();
                    await eventArgs.SendBmpMessage(image);
                }

                break;
            }
            case "砸蛋":
            {
                var message = Commands.Gacha(groupId, senderId);
                if (message != null) await eventArgs.Reply(message);

                break;
            }
            case "砸蛋十连":
            {
                var message = Commands.GachaTen(groupId, senderId, senderName);
                if (message != null) await eventArgs.Reply(message);

                break;
            }
            case "修炼":
            {
                var player = Player.Register(eventArgs);
                if (HavePet(eventArgs, out var pet))
                {
                    if (!player.Activity(eventArgs, 10)) break;
                    var addExp = Random.Next(PlayConfig.MinExpAdd, PlayConfig.MaxExpAdd);
                    pet.Experience += addExp;

                    var place = PlayConfig.Places[Random.Next(PlayConfig.Places.Length)];

                    await eventArgs.SendAtMessage(
                        $"您的【{pet.Name}】正在{place}刻苦的修炼！\r\n" +
                        $"------------------\r\n" +
                        $"·修炼时间：+{PlayConfig.BreaksTime}秒\r\n" +
                        $"·耗费精力：-10点\r\n" +
                        $"·增加经验：+{addExp}\n" +
                        $"------------------");
                }

                break;
            }
            case "学习":
            {
                var player = Player.Register(eventArgs);
                if (HavePet(eventArgs, out var pet))
                {
                    if (!player.Activity(eventArgs, 10)) break;
                    var intellectAdd = Random.Next(PlayConfig.MinIqAdd, PlayConfig.MaxIqAdd);
                    pet.BaseIntellect += intellectAdd;
                    await eventArgs.SendAtMessage(
                        $"您的【{pet.Name}】出门上学啦！\n------------------\n●学习耗时：+{PlayConfig.BreaksTime}秒\n●减少精力：-10点\n●获得智力：+{intellectAdd}\n------------------");
                }

                break;
            }
            case "洗髓":
            {
                var player = Player.Register(eventArgs);
                if (HavePet(eventArgs, out var pet))
                {
                    if (!player.Activity(eventArgs, 10)) break;
                    pet.BaseIntellect--;
                    var addAttr = RandomBool();
                    var addAttrNumber = Random.Next(PlayConfig.MinAttrAdd, PlayConfig.MaxAttrAdd);
                    var addAttText = addAttr ? "攻击" : "防御";
                    if (addAttr)
                        pet.BaseAttack += addAttrNumber;
                    else
                        pet.BaseDefense += addAttrNumber;

                    await eventArgs.SendAtMessage(
                        $"您的【{pet.Name}】正在洗髓伐毛！\n------------------\n●洗髓耗时：+{PlayConfig.BreaksTime}秒\n●减少精力：-10点\n●减少智力：-1\n●增加{addAttText} ：+{addAttrNumber}\n------------------");
                }

                break;
            }
            case "宠物进化":
            {
                var message = Commands.Evolve(groupId, senderId);
                if (message != null) await eventArgs.Reply(message);

                break;
            }
            case "卸下神器":
                if (HavePet(eventArgs))
                {
                    var player = Player.Register(eventArgs);
                    var pet = player.Pet;
                    await eventArgs.SendAtMessage(pet.RemoveArtifact(eventArgs)
                        ? $"卸下神器成功！没有神器的[{pet.Name}]显得很落寞呢！"
                        : "你的宠物还未佩戴神器！");
                }

                break;
            case "宠物战榜":
            {
                var list = Players[groupId]
                    .Where(x => x.Value.Pet != null)
                    .OrderByDescending(p => p.Value.Pet.Power)
                    .ToList()
                    .SafeGetRange(0, 10);


                var message = string.Join("\n", list.ConvertAll(x =>
                {
                    var pet = x.Value.Pet;
                    return $"{x.Key} : {pet.Name} {pet.Power}";
                }));

                await eventArgs.Reply("临时测试界面：\n" + message);
                break;
            }
            case "宠物放生":
                if (HavePet(eventArgs))
                {
                    var player = Player.Register(eventArgs);
                    var pet = player.Pet;
                    await eventArgs.SendAtMessage(
                        $"危险操作\n（LV·{pet.Level}-{pet.Rank}-{pet.Name}）\n将被放生，请在1分钟内回复：\n【确定放生】");
                    player.SentFreeUnixTime = GetNowUnixTime();
                }

                break;
            case "确定放生":
            {
                var player = Player.Register(eventArgs);
                if (GetNowUnixTime() - player.SentFreeUnixTime <= 60)
                {
                    player.SentFreeUnixTime = 0;
                    player.Pet = null;
                    await eventArgs.SendAtMessage("成功放生宠物,您的宠物屁颠屁颠的走了!");
                }

                break;
            }
            case "签到":
            {
                var player = Player.Register(eventArgs);
                var todayUnixTime = DateTime.Now.Date.ToUnixTime();
                //if (todayUnixTime - player.LastSignedUnixTime <= 86400)
                {
                    //await eventArgs.SendAtMessage("今天已签到过了,明天再来吧!");
                    //break;
                }

                if (todayUnixTime - player.LastSignedUnixTime >= 172800 && player.LastSignedUnixTime != 0)
                    player.ContinuousSignedDays = 0;
                else
                    player.ContinuousSignedDays++;

                player.LastSignedUnixTime = todayUnixTime;
                player.SignedDays++;
                var points = 2200 + player.SignedDays * 50;
                player.Points += points;

                using var image = await Renders.SignRender(points, player.SignedDays, player.ContinuousSignedDays,
                    senderName, senderId);
                await eventArgs.SendBmpMessage(image);
                break;
            }
            case "我的资产":
            {
                var player = Player.Register(eventArgs);

                using var image = Renders.AssetRender(player.Points, player.Bonds, senderName);
                await eventArgs.SendBmpMessage(image);
                break;
            }
            case "我的背包":
            {
                var player = Player.Register(eventArgs);
                List<string> items = [];
                foreach (var (name, count) in player.Bag)
                {
                    var item = Items[name];
                    var type = item.ItemType.ToStr();

                    if (count != 0) items.Add($"●[{type}]:{name}×{count}");
                }

                if (items.Count == 0)
                {
                    await eventArgs.SendAtMessage("您的背包里面空空如也哦！");
                    break;
                }

                using var image = Renders.BagRender(items, senderName);
                await eventArgs.SendBmpMessage(image);

                break;
            }
#if DEBUG
            case "AllItemList":
            {
                if (senderId != _config.MasterId) break;

                var message = Items.Values.Select(item => $"{item.Name}");
                await eventArgs.Reply(string.Join("\n", message));
                break;
            }
            case "崩溃测试":
            {
                if (senderId != _config.MasterId) break;
                throw new Exception("崩溃测试");
            }
#endif

            default:
                if (textMessage.StartsWith("使用"))
                {
                    Commands.UseItem(eventArgs);
                }
                else if (textMessage.StartsWith("转让"))
                {
                    Commands.Trade(eventArgs);
                }
                else if (textMessage.StartsWith("宠物升级"))
                {
                    var levelsToUpgrade = 1;
                    if (textMessage.Length > 4) _ = int.TryParse(textMessage[4..], out levelsToUpgrade);

                    var message = Commands.LevelUp(groupId, senderId, levelsToUpgrade);
                    if (message != null) await eventArgs.Reply(message);
                }
                else if (textMessage.StartsWith("购买"))
                {
                    Commands.Buy(eventArgs);
                }
                else if (textMessage.StartsWith("奖励") && HavePermissions(senderId))
                {
                    Tools.ParseString(context, 2, out var itemName, out var count, out var target);

                    if (count != -1 && count == 0)
                    {
                        await eventArgs.SendAtMessage("格式错误！");
                        break;
                    }

                    if (!Items.TryGetValue(itemName, out var item))
                    {
                        await eventArgs.SendAtMessage("该道具并不存在，请检查是否输错！");
                        break;
                    }

                    if (target == null)
                    {
                        foreach (var player in Players[groupId].Values) player.Bag.MergeValue(item.Name, count);
                    }
                    else
                    {
                        var group = Players[groupId];
                        if (group.TryGetValue(target.Value, out var player))
                        {
                            player.Bag.MergeValue(item.Name, count);
                        }
                        else
                        {
                            await eventArgs.SendAtMessage("对方未注册，无法奖励！");
                            break;
                        }
                    }

                    await eventArgs.Reply($"已奖励{itemName}*{count}");
                }
                else if (textMessage.StartsWith("扣除") && HavePermissions(senderId))
                {
                    Tools.ParseString(context, 2, out var itemName, out var count, out var target);

                    if (count != -1 && count == 0)
                    {
                        await eventArgs.SendAtMessage("格式错误！");
                        break;
                    }

                    if (!Items.TryGetValue(itemName, out var item))
                    {
                        await eventArgs.SendAtMessage("该道具并不存在，请检查是否输错！");
                        break;
                    }

                    var players = Players[groupId];
                    var succeedCount = 0;
                    var failCount = 0;

                    if (target == null)
                    {
                        foreach (var player in players.Values)
                        {
                            if (player.Bag[item.Name] < count)
                            {
                                failCount++;
                                continue;
                            }

                            player.Bag.MergeValue(item.Name, -count);
                            succeedCount++;
                        }

                        await eventArgs.SendAtMessage($"扣除完毕\n成功数：{succeedCount}\n失败数：{failCount}");
                        break;
                    }

                    {
                        if (players.TryGetValue(target.Value, out var player))
                        {
                            if (player.Bag[item.Name] >= count) player.Bag.MergeValue(item.Name, -count);
                        }
                        else
                        {
                            await eventArgs.SendAtMessage("对方未注册，无法扣除！");
                            break;
                        }
                    }

                    await eventArgs.Reply($"已扣除{itemName}*{count}");
                }
                else if (textMessage.StartsWith("拉黑") && HavePermissions(senderId))
                {
                    var targetId = GetAtNumber(context.MessageBody);
                    if (targetId == null) return;

                    _config.UserBlackList.Add(targetId.Value);
                    await eventArgs.SendAtMessage($"已拉黑 {targetId}");
                }
                else if (textMessage.StartsWith("解除拉黑") && HavePermissions(senderId))
                {
                    var targetId = GetAtNumber(context.MessageBody);
                    if (targetId == null) return;

                    if (!_config.UserBlackList.Remove(targetId.Value))
                    {
                        await eventArgs.SendAtMessage($"{targetId} 并未被拉黑");
                        return;
                    }

                    await eventArgs.SendAtMessage($"已解除拉黑 {targetId}");
                }
                else if (textMessage.StartsWith("移除管理") && senderId == _config.MasterId)
                {
                    var targetId = GetAtNumber(context.MessageBody);
                    if (targetId == null) return;

                    if (!_config.Admins.Remove(targetId.Value))
                    {
                        await eventArgs.SendAtMessage($"{targetId} 并不是管理");
                        return;
                    }

                    await eventArgs.SendAtMessage($"已移除管理 {targetId}");
                }
                else if (textMessage.StartsWith("添加管理") && senderId == _config.MasterId)
                {
                    var targetId = GetAtNumber(context.MessageBody);
                    if (targetId == null) return;

                    _config.Admins.Add(targetId.Value);
                    await eventArgs.SendAtMessage($"已添加管理 {targetId}");
                }
                else if (textMessage.StartsWith("选择"))
                {
                    if (!(textMessage.Length > 2 && int.TryParse(textMessage[2..], out var decidedCount) &&
                          decidedCount > 0)) return;

                    var player = Player.Register(eventArgs);

                    if (player.GachaPets == null) return;

                    if (player.Pet != null)
                    {
                        await eventArgs.SendAtMessage("您已经有宠物了,贪多嚼不烂哦!\n◇指令:宠物放生");
                        break;
                    }


                    var pet = player.GachaPets[decidedCount - 1];

                    player.Pet = pet;
                    player.GachaPets = null;

                    await eventArgs.Reply(new MessageBodyBuilder()
                        .At(senderId)
                        .Plain($" 恭喜您砸到了一颗{pet.Attribute}属性的宠物蛋")
                        .Image(pet.Render())
                        .Build());
                }
                else if (textMessage.StartsWith("宠物商店"))
                {
                    var count = 1;
                    if (textMessage.Length > 4) _ = int.TryParse(textMessage[4..], out count);

                    if (count > 99999)
                    {
                        await eventArgs.SendAtMessage("数量超出范围！");
                        break;
                    }

                    if (count < 0) count = 1;

                    using var bitmap = PointShop.Render(count);
                    await eventArgs.SendBmpMessage(bitmap);
                }
                else if (textMessage.StartsWith("查看"))
                {
                    var itemName = textMessage[2..];
                    if (!Items.TryGetValue(itemName, out var item))
                    {
                        await eventArgs.SendAtMessage("此物品不存在，或者输入错误！");
                        break;
                    }

                    MessageBodyBuilder builder = new();

                    if (item.DescriptionImageName != null)
                    {
                        var image = SKBitmap.Decode($"./datapack/itemicon/{item.DescriptionImageName}.png");
                        builder.Image(image);
                    }

                    builder.Plain($"{item.Name}：{item.Description}");

#if DEBUG
                    builder.Plain("\n" + item.GetType());
#endif

                    await eventArgs.Reply(builder.Build());
                }
                else if (textMessage.StartsWith("查看"))
                {
                    Commands.Sell(eventArgs);
                }
                else if (textMessage.StartsWith("合成"))
                {
                    Commands.Make(eventArgs);
                }
                else if (textMessage.StartsWith("领取"))
                {
                    Tools.ParseString(context, 2, out var giftName, out _, out _);
                    if (textMessage.Length < 3)
                    {
                        await eventArgs.SendAtMessage("◇指令:领取+礼包名");
                        break;
                    }

                    var gift = FindGift(giftName);
                    if (gift == null)
                    {
                        await eventArgs.SendAtMessage("此礼包不存在，或者输入错误！");
                        break;
                    }

                    var player = Player.Register(eventArgs);

                    if (player.ClaimedGifts.Contains(gift.Id))
                    {
                        await eventArgs.SendAtMessage("你已领取过该礼包，不可重复领取");
                        break;
                    }

                    if (gift.Level != 0 && !(HavePet(eventArgs, out var pet) && pet.Level >= 0)) break;

                    List<string> message = new();
                    foreach (var item in gift.Items)
                    {
                        message.Add($"{Items[item.Name].Name}*{item.Count}");
                        player.Bag.MergeValue(item.Name, item.Count);
                    }

                    player.ClaimedGifts.Add(gift.Id);

                    await eventArgs.SendAtMessage("领取成功\n" + string.Join("\n", message));
                }
                else if (textMessage.StartsWith("宠物副本"))
                {
                    var index = textMessage[4..].GetCount("");
                    if (index == -1) index = 0;

                    var instances = Instances
                        .ConvertAll(instance => $"● {instance.Name} LV > {instance.Level}")
                        .SafeGetRange(index, 10);

                    using var image = Renders.InstanceListRender(instances, index + 1, Cache.MaxInstanceIndex);
                    await eventArgs.SendBmpMessage(image);
                }
                else if (textMessage.StartsWith("进入副本"))
                {
                    Tools.ParseString(context, 4, out var instanceName, out var count, out _);
                    var instance = FindReplica(instanceName);
                    if (!HavePet(eventArgs, out var pet)) break;

                    if (instance == null)
                    {
                        await eventArgs.SendAtMessage("此副本不存在,或副本名称错误!\n◇指令:进入副本+副本名*次数");
                        break;
                    }

                    var player = Player.Register(eventArgs);
                    var result = instance.Challenge(player, count);

                    using var image = Renders.InstanceRender(pet.Name, instance.EnemyName, result);

                    MessageBodyBuilder builder = new();
                    builder.Image(image);
                    if (instance.IconName != null)
                    {
                        var path = $"./datapack/replicaicon/{instance.IconName}.png";
                        var icon = SKBitmap.Decode(path);
                        builder.Image(icon);
                    }

                    await eventArgs.Reply(builder.Build());
                }
                else if (textMessage.StartsWith("宠物攻击"))
                {
                    Commands.Attack(eventArgs);
                }
                else if (textMessage.StartsWith("宠物侦察"))
                {
                    if (!HavePet(eventArgs, out var pet)) return;

                    const int energy = 5;
                    var target = GetAtNumber(context.MessageBody);
                    if (target == null) return;


                    if (pet.Energy < energy)
                    {
                        //TODO:支持自定义
                        await eventArgs.SendAtMessage($"你的宠物已经精疲力竭了，侦察宠物需要消化{energy}点精力！当前精力剩余{pet.Energy}");
                        return;
                    }

                    var targetPlayer = Player.Register(groupId, target.Value);
                    if (targetPlayer.Pet == null)
                    {
                        await eventArgs.SendAtMessage("对方还未拥有宠物，无法进行侦查宠物！");
                        return;
                    }

                    pet.Energy -= energy;
                    await eventArgs.Reply(new MessageBodyBuilder()
                        .At(senderId)
                        .Plain($" 侦察成功，精力-{energy}，您侦察的宠物信息如下：")
                        .Image(targetPlayer.Pet.Render())
                        .Build());
                }

                return;
        }
    }

    private static void ConsoleCommand()
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write("> ");
        var input = Console.ReadLine() ?? string.Empty;
        var commands = input.Split(' ');
        switch (commands[0])
        {
            case "config save":
                WriteConfig();
                Console.WriteLine("已保存配置");
                break;

            case "stop":
                WriteConfig();
                SaveData();
                KeysExit();
                break;

            case "stop!":
                Environment.Exit(0);
                return;

            case "help":
                Console.WriteLine("——————帮助菜单——————\n" +
                                  "/clearconfig 清除配置文件\n" +
                                  "/stop 退出程序\n" +
                                  "/stop! 不保存数据退出\n" +
                                  "/save 保存数据\n" +
                                  "/reload 热重载" +
                                  "/group add {群号} 添加群\n" +
                                  "/group del {群号} 删除群\n" +
                                  "/group list 列出所有已添加群");
                break;

            case "group":
                if (commands.Length <= 1)
                {
                    CoverLine("参数不足");
                    break;
                }

                switch (commands[1])
                {
                    case "list":
                        Console.WriteLine(string.Join("\n", _config.GroupList));
                        break;

                    case "add":
                    {
                        if (commands.Length < 3)
                        {
                            CoverLine("参数不足");
                            break;
                        }

                        var groupId = commands[2];
                        if (!long.TryParse(groupId, out _))
                        {
                            Console.WriteLine("请输入正确的群号！");
                            break;
                        }

                        _config.GroupList.Add(groupId);
                        Log.Info("Main", $"已添加群{groupId}");
                        break;
                    }
                    case "del":
                    {
                        if (commands.Length < 3)
                        {
                            CoverLine("参数不足");
                            break;
                        }

                        var groupId = input[10..];
                        if (!long.TryParse(groupId, out _) && !_config.GroupList.Remove(groupId))
                        {
                            Console.WriteLine("请输入正确的群号！");
                            break;
                        }

                        Log.Info("Main", $"已删除群{groupId}");

                        break;
                    }
                }

                break;

            case "reload":
                Reload();
                Console.WriteLine("已重载数据");
                break;

            case "save":
                WriteConfig();
                SaveData();
                break;

            default:
                Console.WriteLine($"未知命令\"{input}\"，请输入/help查看命令");
                break;
        }
    }

    public static long GetLevelUpExp(int level)
    {
        return 5 * Pow(level, 3) + 15 * Pow(level, 2) + 40 * level + 100;
    }

    public static void CoverLine(string text = "")
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.WriteLine(text);
    }

    private static bool RandomBool()
    {
        return Random.Next(0, 2) == 1;
    }


    private static void WriteConfig()
    {
        const string path = "./config.json";
        var json = _config.ToJsonString();
        File.WriteAllText(path, json);
    }

    private static void ReadConfig()
    {
        const string path = "./config.json";
        var json = File.ReadAllText(path);
        var config = JsonConvert.DeserializeObject<Config>(json);

        if (config == null) return;

        _config = config;
    }

    private static bool HavePermissions(long id)
    {
        return _config.Admins.Contains(id) || _config.MasterId == id;
    }

    public static long? GetAtNumber(MessageBody messages)
    {
        foreach (var message in messages.ToList().Where(message => message.MessageType == SegmentType.At))
        {
            return ((AtSegment)message.Data).Target.ToInt64();
        }

        return null;
    }

    private static void Reload()
    {
        SaveData();
        ReadData();
    }

    public static long GetNowUnixTime()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public static void KeysExit()
    {
        Console.Write("按任意键退出…");
        Console.ReadKey(true);
        Environment.Exit(0);
    }

    public static long Pow(long x, long y)
    {
        return (long)Math.Pow(x, y);
    }

    private static ISoraConfig GetSoraConfig(string host)
    {
        if (_config.ReverseWebsocket)
        {
            // 反向Websocket
            return new ServerConfig
            {
                EnableSocketMessage = false,
                ThrowCommandException = false,
                SendCommandErrMsg = false,
                CommandExceptionHandle = CommandExceptionHandle,
                HeartBeatTimeOut = _config.HeartBeatTimeOut,
                Host = host,
                Port = _config.Port
            };
        }

        //正向Websocket
        return new ClientConfig
        {
            EnableSocketMessage = false,
            ThrowCommandException = false,
            SendCommandErrMsg = false,
            CommandExceptionHandle = CommandExceptionHandle,
            Host = _config.Host,
            Port = _config.Port,
            HeartBeatTimeOut = _config.HeartBeatTimeOut
        };
    }

    private static void Initialize()
    {
        #region 设置连接类型

        Console.Write("使用反向Websocket(Y/n):");
        var reverseReslut = Console.ReadLine();
        var reverse = true;
        if (!reverseReslut.IsNullOrEmpty() && reverseReslut.ToLower() == "n")
        {
            reverse = false;
            _config.ReverseWebsocket = true;
            Console.Write("连接地址(不含端口):");
            var host = Console.ReadLine();
            if (!string.IsNullOrEmpty(host))
            {
                _config.Host = host;
            }
        }

        #endregion

        #region 设置端口

        while (true)
        {
            Console.Write("连接端口(留空则8080):");
            var portText = Console.ReadLine();
            if (string.IsNullOrEmpty(portText)) break;
            // 尝试解析端口号
            if (ushort.TryParse(portText, out var port))
            {
                _config.Port = port;
                break;
            }

            Console.WriteLine("请输入有效的端口号！");
        }

        #endregion

        #region 设置是否公开至局域网

        if (!reverse) return;
        Console.Write("是否公开至局域网(y/N):");
        var lanPublic = Console.ReadLine();
        if (!lanPublic.IsNullOrEmpty() && lanPublic.Equals("y", StringComparison.CurrentCultureIgnoreCase))
        {
            _config.LanPublic = true;
        }

        #endregion
    }

    private static string? GetLocalIpAddress()
    {
        var ipEntry = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in ipEntry.AddressList)
        {
            if (ip.AddressFamily != AddressFamily.InterNetwork) continue;

            var ipText = ip.ToString();
            if (ipText.StartsWith("192.168.")) return ipText;
        }

        return null;
    }

    //指令错误处理
    private static async void CommandExceptionHandle(Exception exception, BaseMessageEventArgs eventArgs, string log)
    {
        await eventArgs.Reply($"死了啦都你害的啦\r\n{log}\r\n{exception.Message}");
    }

    #region 发送消息

    public static async void SendAtMessage(long groupId, long senderId, string message)
    {
        await SendMessage(groupId, new MessageBodyBuilder().At(senderId).Plain(" " + message).Build());
    }

    /*public static async void SendBmpMessage(string groupId, Image imageData)
    {
        await SendMessage(groupId, new MessageBodyBuilder().Image(imageData).Build());
    }*/

    public static async Task SendMessage(long groupId, MessageBody message)
    {
        await _soraApi.SendGroupMessage(groupId, message);
    }

    #endregion 发送消息
}