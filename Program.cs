using System.Drawing;
using System.Net;
using System.Net.Sockets;
using Manganese.Text;
using Newtonsoft.Json;
using OpenPetsWorld.Data;
using OpenPetsWorld.Item;
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
    public static readonly Font YaHei = new("微软雅黑", 20, FontStyle.Regular);
    public static readonly Brush Black = Brushes.Black;
    private static Config _config = new();

    private static readonly HttpClient HttpClient = new();
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
            eventArgs.SendAtMessage($"本群已开启文字游戏OpenPetsWorld[本游戏完全开源]\n{_config.BootText}");
        }

        if (notRunning) return;

        switch (textMessage)
        {
            case "OpenPetsWorld":
                await eventArgs.Reply("由 OpenPetsWorld Pre.3 核能驱动！");
                break;

            case "关OPW":
                if (HavePermissions(senderId))
                {
                    _config.NotRunningGroup.Add(groupId);
                    eventArgs.SendAtMessage("已关闭OpenPetsWorld");
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

                var image = Image.FromFile(menuPath);

                await eventArgs.Reply(new MessageBodyBuilder().Image(image).Build());
                break;
            }
            case "我的宠物":
            {
                if (HavePet(eventArgs, out var pet))
                {
                    var image = pet.Render();
                    eventArgs.SendBmpMessage(image);
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
                var message = Commands.GachaTen(groupId, senderId);
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

                    eventArgs.SendAtMessage(
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
                    eventArgs.SendAtMessage(
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

                    eventArgs.SendAtMessage(
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
                    eventArgs.SendAtMessage(pet.RemoveArtifact(eventArgs)
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
                    eventArgs.SendAtMessage(
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
                    eventArgs.SendAtMessage("成功放生宠物,您的宠物屁颠屁颠的走了!");
                }

                break;
            }
            case "签到":
            {
                var player = Player.Register(eventArgs);
                var todayUnixTime = DateTime.Now.Date.ToUnixTime();
                if (todayUnixTime - player.LastSignedUnixTime <= 86400)
                {
                    eventArgs.SendAtMessage("今天已签到过了,明天再来吧!");
                    break;
                }

                if (todayUnixTime - player.LastSignedUnixTime >= 172800 && player.LastSignedUnixTime != 0)
                    player.ContinuousSignedDays = 0;
                else
                    player.ContinuousSignedDays++;

                player.LastSignedUnixTime = todayUnixTime;
                player.SignedDays++;
                var points = 2200 + player.SignedDays * 50;
                player.Points += points;

                #region 绘图

                var stream =
                    await HttpClient.GetStreamAsync($"https://q2.qlogo.cn/headimg_dl?dst_uin={senderId}&spec=100");
                using Bitmap image = new(230, 90);
                using var graphics = Graphics.FromImage(image);
                graphics.Clear(Color.White);
                graphics.DrawImage(Image.FromStream(stream), new Rectangle(0, 0, 90, 90));
                stream.Close();
                graphics.ClearText();
                graphics.DrawString(senderName, new Font("微软雅黑", 15, FontStyle.Bold),
                    Brushes.Black, new Point(95, 5));
                using Font font = new("微软雅黑", 13, FontStyle.Regular);
                string[] signTexts =
                [
                    $"奖励积分：{points}",
                    $"累签天数：{player.SignedDays}",
                    $"连签天数：{player.ContinuousSignedDays}/31"
                ];
                var n = 30;
                foreach (var text in signTexts)
                {
                    graphics.DrawString(text, font, Brushes.Black, new Point(92, n));
                    n += 18;
                }

                #endregion 绘图

                eventArgs.SendBmpMessage(image);
                break;
            }
            case "我的资产":
            {
                var player = Player.Register(eventArgs);
                using Bitmap bitmap = new(480, 235);
                using var graphics = Graphics.FromImage(bitmap);
                using Font font = new("Microsoft YaHei", 23, FontStyle.Bold);
                graphics.Clear(Color.White);
                graphics.DrawString($"@{senderName} 财富信息如下：", font, Black, 2, 2);
                graphics.DrawLine(new Pen(Color.Black, 3), new Point(0, 55), new Point(480, 55));
                graphics.DrawString($"●积分：{player.Points}", font, Black, 0, 65);
                graphics.DrawString($"●点券：{player.Bonds}", font, Black, 0, 125);
                graphics.DrawLine(new Pen(Color.Black, 3), new Point(0, 180), new Point(480, 180));
                eventArgs.SendBmpMessage(bitmap);
                break;
            }
            case "我的背包":
            {
                var player = Player.Register(eventArgs);
                List<string> bagItemList = [];
                foreach (var (name, count) in player.Bag)
                {
                    var item = Items[name];
                    var type = item.ItemType.ToStr();

                    if (count != 0) bagItemList.Add($"●[{type}]:{name}⨉{count}");
                }

                if (bagItemList.Count == 0)
                {
                    eventArgs.SendAtMessage("您的背包里面空空如也哦！");
                    break;
                }

                #region 绘图

                var height = bagItemList.Count * 38 + 110;
                using Bitmap imageData = new(480, height);
                using var graphics = Graphics.FromImage(imageData);
                using Font font = new("Microsoft YaHei", 23, FontStyle.Regular);

                graphics.Clear(Color.White);
                graphics.DrawString($"[@{senderId}]您的背包：", font, Black, 2, 2);
                graphics.DrawLine(new Pen(Color.Black, 3), new Point(0, 55), new Point(480, 55));

                var i = 65;
                foreach (var itemStr in bagItemList)
                {
                    graphics.DrawString(itemStr, font, Black, 0, i);
                    i += 38;
                }

                graphics.DrawLine(new Pen(Color.Black, 3), new Point(0, height - 30), new Point(480, height - 30));
                eventArgs.SendBmpMessage(imageData);

                #endregion 绘图

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
                        eventArgs.SendAtMessage("格式错误！");
                        break;
                    }

                    if (!Items.TryGetValue(itemName, out var item))
                    {
                        eventArgs.SendAtMessage("该道具并不存在，请检查是否输错！");
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
                            eventArgs.SendAtMessage("对方未注册，无法奖励！");
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
                        eventArgs.SendAtMessage("格式错误！");
                        break;
                    }

                    if (!Items.TryGetValue(itemName, out var item))
                    {
                        eventArgs.SendAtMessage("该道具并不存在，请检查是否输错！");
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

                        eventArgs.SendAtMessage($"扣除完毕\n成功数：{succeedCount}\n失败数：{failCount}");
                        break;
                    }

                    {
                        if (players.TryGetValue(target.Value, out var player))
                        {
                            if (player.Bag[item.Name] >= count) player.Bag.MergeValue(item.Name, -count);
                        }
                        else
                        {
                            eventArgs.SendAtMessage("对方未注册，无法扣除！");
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
                    eventArgs.SendAtMessage($"已拉黑 {targetId}");
                }
                else if (textMessage.StartsWith("解除拉黑") && HavePermissions(senderId))
                {
                    var targetId = GetAtNumber(context.MessageBody);
                    if (targetId == null) return;

                    if (!_config.UserBlackList.Remove(targetId.Value))
                    {
                        eventArgs.SendAtMessage($"{targetId} 并未被拉黑");
                        return;
                    }

                    eventArgs.SendAtMessage($"已解除拉黑 {targetId}");
                }
                else if (textMessage.StartsWith("移除管理") && senderId == _config.MasterId)
                {
                    var targetId = GetAtNumber(context.MessageBody);
                    if (targetId == null) return;

                    if (!_config.Admins.Remove(targetId.Value))
                    {
                        eventArgs.SendAtMessage($"{targetId} 并不是管理");
                        return;
                    }

                    eventArgs.SendAtMessage($"已移除管理 {targetId}");
                }
                else if (textMessage.StartsWith("添加管理") && senderId == _config.MasterId)
                {
                    var targetId = GetAtNumber(context.MessageBody);
                    if (targetId == null) return;

                    _config.Admins.Add(targetId.Value);
                    eventArgs.SendAtMessage($"已添加管理 {targetId}");
                }
                else if (textMessage.StartsWith("选择"))
                {
                    if (!(textMessage.Length > 2 && int.TryParse(textMessage[2..], out var decidedCount) &&
                          decidedCount > 0)) return;

                    var player = Player.Register(eventArgs);

                    if (player.GachaPets == null) return;

                    if (player.Pet != null)
                    {
                        eventArgs.SendAtMessage("您已经有宠物了,贪多嚼不烂哦!\n◇指令:宠物放生");
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
                        eventArgs.SendAtMessage("数量超出范围！");
                        break;
                    }

                    if (count < 0) count = 1;

                    using var bitmap = PointShop.Render(count);
                    eventArgs.SendBmpMessage(bitmap);
                }
                else if (textMessage.StartsWith("查看"))
                {
                    var itemName = textMessage[2..];
                    if (!Items.TryGetValue(itemName, out var item))
                    {
                        eventArgs.SendAtMessage("此物品不存在，或者输入错误！");
                        break;
                    }

                    MessageBodyBuilder builder = new();

                    if (item.DescriptionImageName != null)
                    {
                        var image = Image.FromFile($"./datapack/itemicon/{item.DescriptionImageName}.png");
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
                        eventArgs.SendAtMessage("◇指令:领取+礼包名");
                        break;
                    }

                    var gift = FindGift(giftName);
                    if (gift == null)
                    {
                        eventArgs.SendAtMessage("此礼包不存在，或者输入错误！");
                        break;
                    }

                    var player = Player.Register(eventArgs);

                    if (player.ClaimedGifts.Contains(gift.Id))
                    {
                        eventArgs.SendAtMessage("你已领取过该礼包，不可重复领取");
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

                    eventArgs.SendAtMessage("领取成功\n" + string.Join("\n", message));
                }
                else if (textMessage.StartsWith("宠物副本"))
                {
                    var index = textMessage[4..].GetCount("");
                    if (index == -1) index = 0;

                    var replicasString =
                        Instances.ConvertAll(replica => $"● {replica.Name} LV > {replica.Level}");

                    #region 绘图

                    using Font font = new("Microsoft YaHei", 23, FontStyle.Regular);
                    using Bitmap bitmap = new(480, 640);
                    using var graphics = Graphics.FromImage(bitmap);
                    using Pen pen = new(Color.Black, 3);
                    graphics.Clear(Color.White);
                    graphics.DrawString("当前开放副本如下：", font, Brushes.Black, 5, 5);
                    graphics.DrawLine(pen, 0, 60, 480, 60);
                    graphics.DrawLine(pen, 0, 498, 235, 498);

                    #endregion

                    var n = 60;
                    foreach (var text in replicasString.SafeGetRange(index, 10))
                        graphics.DrawString(text, font, Brushes.Black, 0, n);

                    eventArgs.SendBmpMessage(bitmap);
                }
                else if (textMessage.StartsWith("进入副本"))
                {
                    Tools.ParseString(context, 4, out var replicaName, out var count, out _);
                    var replica = FindReplica(replicaName);
                    if (replica == null || !HavePet(eventArgs)) break;

                    var player = Player.Register(eventArgs);
                    if (!HavePet(eventArgs, out var pet)) break;

                    _ = replica.Challenge(player, count, out var expAdd, out var pointAdd);

                    #region 绘图

                    using Bitmap bitmap = new(600, 205);
                    using var graphics = Graphics.FromImage(bitmap);
                    using Font font = new("Microsoft YaHei", 23, FontStyle.Regular);
                    graphics.Clear(Color.White);
                    graphics.DrawString($"【{pet.Name} VS {replica.enemyName}】", font, Brushes.Black, 180,
                        15);
                    graphics.DrawString($"◆战斗结果：胜利\n◆获得经验：{expAdd}\n◆获得积分：{pointAdd}", font, Brushes.Black, 15, 55);
                    graphics.DrawString(
                        $"◆消耗精力：{replica.Energy * count}\n◆血量减少：{replica.Attack * count}\n◆获得积分：{(player.Pet.Health == 0 ? "死亡" : "正常")}",
                        font, Brushes.Black, 305, 55);

                    #endregion

                    MessageBodyBuilder builder = new();
                    builder.Image(bitmap);
                    if (replica.IconName != null)
                    {
                        var path = $"./datapack/replicaicon/{replica.IconName}.png";
                        var icon = Image.FromFile(path);
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
                        eventArgs.SendAtMessage($"你的宠物已经精疲力竭了，侦察宠物需要消化{energy}点精力！当前精力剩余{pet.Energy}");
                        return;
                    }

                    var targetPlayer = Player.Register(groupId, target.Value);
                    if (targetPlayer.Pet == null)
                    {
                        eventArgs.SendAtMessage("对方还未拥有宠物，无法进行侦查宠物！");
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
                HeartBeatTimeOut = TimeSpan.FromSeconds(32.0),
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
            Port = _config.Port
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
        if (!lanPublic.IsNullOrEmpty() && lanPublic.ToLower() == "y")
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