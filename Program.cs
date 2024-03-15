using Manganese.Text;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Sessions;
using Mirai.Net.Sessions.Http.Managers;
using Mirai.Net.Utils.Scaffolds;
using Newtonsoft.Json;
using OpenPetsWorld.Item;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reactive.Linq;
using static OpenPetsWorld.Game;
using Timer = System.Timers.Timer;

namespace OpenPetsWorld;

internal static class Program
{
    public static readonly Font YaHei = new("微软雅黑", 20, FontStyle.Regular);
    public static readonly Brush Black = Brushes.Black;
    private static string? _address;
    private static string? _verifyKey;
    private static string? _qqNumber;
    private static List<string> _groupList = new();
    private static HashSet<string> _notRunningGroup = new();
    private static bool _blackListMode;
    private static string _masterId = "";
    private static List<string> _admins = new();
    private static readonly HttpClient HttpClient = new();
    public static readonly Random Random = new();
    public static Logger Log;
    private static string _bootText = "";

    private static async Task Main()
    {
        Console.Title = "OpenPetWorld控制台";

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

            Log.Info("未检测到配置文件，开始初始化，请保持开启Mirai");
            Initialize();

            #endregion Initialize
        }

        Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
        //Log = new($"./{GetNowUnixTime()}.txt");
        Log = new();

        #region Start

        Console.WriteLine(
            "===================================[ OpenPetsWorld Pre.3 ]===================================");
        Console.WriteLine(
            "\r\n  ___                       _____        _         _    _               _      _ \r\n / _ \\                     | ___ \\      | |       | |  | |             | |    | |\r\n/ / \\ \\ _ __    ___  _ __  | |_/ |  ___ | |_  ___ | |  | |  ___   _ __ | |  __| |\r\n| | | || '_ \\  / _ \\| '_ \\ |  ___/ / _ \\| __|/ __|| |/\\| | / _ \\ |  __|| | / _  |\r\n\\ \\_/ /| |_) | | __/| | | || |     | __/| |_ \\__ \\\\  /\\  /| (_) || |   | | |(_| |\r\n \\___/ | .__/  \\___||_| |_||_|     \\___| \\__||___/ \\/  \\/  \\___/ |_|   |_| \\____|\r\n       | |                                                                       \r\n       |_|                                                                       ");

        MiraiBot bot;
        Beginning:
        try
        {
            bot = new()
            {
                Address = _address,
                QQ = _qqNumber,
                VerifyKey = _verifyKey
            };
            Log.Info("正在连接Mirai");
            await bot.LaunchAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.Write("是否重新输入连接地址、QQ号、密钥配置？(留空则否，R为重试)(Y/N/R):");
            var selection = Console.ReadLine();
            if (selection == "Y")
            {
                Initialize();
                WriteConfig();
                goto Beginning;
            }
            else if (selection == "R")
            {
                goto Beginning;
            }

            return;
        }

        #endregion Start
        
        Log.Info("已连接至Mirai");
        //写入配置文件
        if (!File.Exists(configPath)) WriteConfig();

        ReadData();

        Timer timer = new(60000)
        {
            Enabled = true,
            AutoReset = true
        };
        timer.Elapsed += EnergyRecovery;

        bot.MessageReceived
            .OfType<GroupMessageReceiver>()
            .Where(Filter)
            .Subscribe(x =>
            {
                try
                {
                    OnMessage(x);
                }
                catch (Exception e)
                {
                    Log.Error(e.Message);
                    Trace.Flush();
                }
            });

        for (;;)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write("> ");
            var input = Console.ReadLine() ?? string.Empty;
            var commands = input.Split(' ');
            switch (commands[0])
            {
                case "delconfig":
                    File.Delete("./config.json");
                    Console.WriteLine("已清除配置");
                    break;

                case "stop":
                    WriteConfig();
                    SaveData();
                    KeysExit();
                    break;

                case "stop!":
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
                            Console.WriteLine(string.Join("\n", _groupList));
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

                            _groupList.Add(groupId);
                            Log.Info($"已添加群{groupId}");
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
                            if (!long.TryParse(groupId, out _) && !_groupList.Remove(groupId))
                            {
                                Console.WriteLine("请输入正确的群号！");
                                break;
                            }

                            Log.Info($"已删除群{groupId}");

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
    }

    private static bool Filter(GroupMessageReceiver x)
    {
        var whiteList = _groupList.Contains(x.GroupId);
        return _blackListMode ? !whiteList : whiteList;
    }

    private static async void OnMessage(GroupMessageReceiver receiver)
    {
        var groupId = receiver.GroupId;
        var memberId = receiver.Sender.Id;
        var chain = receiver.MessageChain;
        var strMess = chain.GetPlainMessage();

        var notRunning = _notRunningGroup.Contains(groupId);
        if (strMess == "开OPW" && HavePermissions(memberId) && notRunning)
        {
            _notRunningGroup.Remove(groupId);
            receiver.SendAtMessage($"本群已开启文字游戏OpenPetsWorld[本游戏完全开源]\n{_bootText}");
        }

        if (notRunning) return;

        switch (strMess)
        {
            case "OpenPetsWorld":
                await receiver.SendMessageAsync("由OpenPetsWorld Pre.3强力驱动");
                break;

            case "关OPW":
                if (HavePermissions(memberId))
                {
                    _notRunningGroup.Add(groupId);
                    receiver.SendAtMessage("已关闭OpenPetsWorld");
                }

                break;

            case "宠物世界":
            {
                const string menuPath = "./datapack/menu.png";

                if (!File.Exists(menuPath))
                {
                    Log.Warn("菜单图片不存在");
                    break;
                }

                var fullPath = Path.GetFullPath(menuPath);

                await receiver.SendMessageAsync(new MessageChainBuilder().ImageFromPath(fullPath).Build());
                break;
            }
            case "我的宠物":
            {
                if (HavePet(receiver, out var pet))
                {
                    var image = pet.Render();
                    receiver.SendBmpMessage(image);
                }

                break;
            }
            case "砸蛋":
            {
                var message = Core.OpenEgg(groupId, memberId);
                if ((object?)message != null) await receiver.SendMessageAsync(message);

                break;
            }
            case "砸蛋十连":
            {
                var message = Core.OpenTenEggs(groupId, memberId);
                if ((object?)message != null) await receiver.SendMessageAsync(message);
                
                break;
            }
            case "修炼":
            {
                var player = Player.Register(receiver);
                if (HavePet(receiver, out var pet))
                {
                    if (player.Activity(receiver, 10)) break;
                    var addExp = Random.Next(MinExpAdd, MaxExpAdd);
                    pet.Experience += addExp;
                    receiver.SendAtMessage(
                        $"您的【{pet.Name}】正在{UnitingPlace[Random.Next(0, UnitingPlace.Length)]}刻苦的修炼！\r\n------------------\r\n·修炼时间：+{BreaksTime}秒\r\n·耗费精力：-10点\r\n·增加经验：+{addExp}\n------------------");
                }

                break;
            }
            case "学习":
            {
                var player = Player.Register(receiver);
                if (HavePet(receiver, out var pet))
                {
                    if (player.Activity(receiver, 10)) break;
                    var intellectAdd = Random.Next(MinIqAdd, MaxIqAdd);
                    pet.BaseIntellect += intellectAdd;
                    receiver.SendAtMessage(
                        $"您的【{pet.Name}】出门上学啦！\n------------------\n●学习耗时：+{BreaksTime}秒\n●减少精力：-10点\n●获得智力：+{intellectAdd}\n------------------");
                }

                break;
            }
            case "洗髓":
            {
                var player = Player.Register(receiver);
                if (HavePet(receiver, out var pet))
                {
                    if (player.Activity(receiver, 10)) break;
                    pet.BaseIntellect--;
                    var addAttr = RandomBool();
                    var addAttrNumber = Random.Next(MinAttrAdd, MaxAttrAdd);
                    var addAttText = addAttr ? "攻击" : "防御";
                    if (addAttr)
                        pet.BaseAttack += addAttrNumber;
                    else
                        pet.BaseDefense += addAttrNumber;

                    receiver.SendAtMessage(
                        $"您的【{pet.Name}】正在洗髓伐毛！\n------------------\n●洗髓耗时：+{BreaksTime}秒\n●减少精力：-10点\n●减少智力：-1\n●增加{addAttText} ：+{addAttrNumber}\n------------------");
                }

                break;
            }
            case "宠物进化":
            {
                var message = Core.Evolve(groupId, memberId);
                if ((object?)message != null) await receiver.SendMessageAsync(message);

                break;
            }
            case "卸下神器":
                if (HavePet(receiver))
                {
                    var player = Player.Register(receiver);
                    var pet = player.Pet;
                    receiver.SendAtMessage(pet.RemoveArtifact(receiver)
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

                await receiver.SendMessageAsync("临时测试界面：\n" + message);
                break;
            }
            case "宠物放生":
                if (HavePet(receiver))
                {
                    var player = Player.Register(receiver);
                    var pet = player.Pet;
                    receiver.SendAtMessage(
                        $"危险操作\n（LV·{pet.Level}-{pet.Rank}-{pet.Name}）\n将被放生，请在1分钟内回复：\n【确定放生】");
                    player.SentFreeUnixTime = GetNowUnixTime();
                }

                break;

            case "确定放生":
            {
                var player = Player.Register(receiver);
                if (GetNowUnixTime() - player.SentFreeUnixTime <= 60)
                {
                    player.SentFreeUnixTime = 0;
                    player.Pet = null;
                    receiver.SendAtMessage("成功放生宠物,您的宠物屁颠屁颠的走了!");
                }

                break;
            }
            case "签到":
            {
                var player = Player.Register(receiver);
                var todayUnixTime = DateTime.Now.Date.ToUnixTime();
                if (todayUnixTime - player.LastSignedUnixTime <= 86400)
                {
                    receiver.SendAtMessage("今天已签到过了,明天再来吧!");
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

                Stopwatch watch = new();
                watch.Start();
                #region 绘图

                var stream =
                    await HttpClient.GetStreamAsync($"https://q2.qlogo.cn/headimg_dl?dst_uin={memberId}&spec=100");
                using Bitmap imageData = new(230, 90);
                using var graphics = Graphics.FromImage(imageData); //存入画布
                graphics.Clear(Color.White);
                graphics.DrawImage(Image.FromStream(stream), new Rectangle(0, 0, 90, 90));
                stream.Close();
                graphics.ClearText();
                graphics.DrawString(receiver.Sender.Name, new("微软雅黑", 15, FontStyle.Bold),
                    Brushes.Black, new Point(95, 5));
                using Font font = new("微软雅黑", 13, FontStyle.Regular);
                string[] signTexts2 =
                {
                    points.ToString(),
                    player.SignedDays.ToString(),
                    $"{player.ContinuousSignedDays}/31"
                };
                var n = 30;
                for (var i = 0; i < 3; i++)
                {
                    var signText = SignTexts[i];
                    var signText2 = signTexts2[i];
                    graphics.DrawString($"{signText}：{signText2}", font, Brushes.Black, new Point(95, n));
                    n += 18;
                }

                #endregion 绘图

                watch.Stop();
                //Debug.Write(watch.ElapsedMilliseconds);
                
                receiver.SendBmpMessage(imageData);
                break;
            }
            case "我的资产":
            {
                var player = Player.Register(receiver);
                using Bitmap bitmap = new(480, 235);
                using var graphics = Graphics.FromImage(bitmap);
                using Font font = new("Microsoft YaHei", 23, FontStyle.Bold);
                graphics.Clear(Color.White);
                graphics.DrawString($"[@{memberId}]您的财富信息如下：", font, Black, 2, 2);
                graphics.DrawLine(new Pen(Color.Black, 3), new(0, 55), new(480, 55));
                graphics.DrawString($"●积分：{player.Points}", font, Black, 0, 65);
                graphics.DrawString($"●点券：{player.Bonds}", font, Black, 0, 125);
                graphics.DrawLine(new Pen(Color.Black, 3), new(0, 180), new(480, 180));
                receiver.SendBmpMessage(bitmap);
                break;
            }
            case "我的背包":
            {
                var player = Player.Register(receiver);
                List<string> bagItemList = new();
                foreach (var (id, count) in player.Bag)
                {
                    var item = Items[id];
                    var type = item.ItemType.ToStr();

                    if (count != 0) bagItemList.Add($"●[{type}]:{item.Name}⨉{count}");
                }

                if (bagItemList.Count == 0)
                {
                    receiver.SendAtMessage("您的背包里面空空如也哦！");
                    break;
                }

                #region 绘图

                var height = bagItemList.Count * 38 + 110;
                using Bitmap imageData = new(480, height);
                using var graphics = Graphics.FromImage(imageData);
                using Font font = new("Microsoft YaHei", 23, FontStyle.Regular);

                graphics.Clear(Color.White);
                graphics.DrawString($"[@{memberId}]您的背包：", font, Black, 2, 2);
                graphics.DrawLine(new Pen(Color.Black, 3), new Point(0, 55), new Point(480, 55));

                var i = 65;
                foreach (var itemStr in bagItemList)
                {
                    graphics.DrawString(itemStr, font, Black, 0, i);
                    i += 38;
                }

                graphics.DrawLine(new Pen(Color.Black, 3), new Point(0, height - 30), new Point(480, height - 30));
                receiver.SendBmpMessage(imageData);

                #endregion 绘图

                break;
            }
            case "AllItemList":
                if (memberId == _masterId)
                {
                    var message = Items.Values.Select(item => $"{item.Id} {item.Name}").ToList();

                    await receiver.SendMessageAsync(string.Join("\n", message));
                }

                break;

            default:
                if (strMess.StartsWith("使用"))
                {
                    Tools.ParseString(chain, 2, out var itemName, out var count, out var target);
                    if (!IsCompliant(receiver, count)) break;

                    var item = FindItem(itemName);

                    if (item == null)
                    {
                        receiver.SendAtMessage("该道具并不存在，请检查是否输错！");
                        break;
                    }

                    var player = Player.Register(receiver);
                    player.Bag.TryAdd(item.Id, 0);

                    item.Use(receiver, count);
                }
                else if (strMess.StartsWith("宠物升级"))
                {
                    var levelsToUpgrade = 1;
                    if (strMess.Length > 4) _ = int.TryParse(strMess[4..], out levelsToUpgrade);

                    var message = Core.LevelUp(groupId, memberId, levelsToUpgrade);
                    if ((object?)message != null) await receiver.SendMessageAsync(message);
                }
                else if (strMess.StartsWith("购买"))
                {
                    Tools.ParseString(chain, 2, out var itemName, out var count, out _);

                    if (!IsCompliant(receiver, count)) break;
                    var item = FindItem(itemName);

                    if (item == null)
                    {
                        receiver.SendAtMessage("该道具并不存在，请检查是否输错！");
                        break;
                    }

                    if (PointShop.Commodities.TryGetValue(item.Id, out var unitPrice))
                    {
                        var price = unitPrice * count;

                        var player = Player.Register(receiver);
                        var succeeded = player.Buy(item.Id, count);
                        receiver.SendAtMessage(succeeded
                            ? $"购买成功！获得{count}个{item.Name},本次消费{price}积分！可发送[我的背包]查询物品！\n物品说明：{item.Description}"
                            : $"你的积分不足[{price}]，无法购买！");
                    }
                    else
                    {
                        receiver.SendAtMessage("宠物商店内未有此物品的身影！");
                    }
                }
                else if (strMess.StartsWith("奖励") && HavePermissions(memberId))
                {
                    Tools.ParseString(chain, 2, out var itemName, out var count, out var target);

                    if (count != -1 && count == 0)
                    {
                        receiver.SendAtMessage("格式错误！");
                        break;
                    }

                    var item = FindItem(itemName);

                    if (item == null)
                    {
                        receiver.SendAtMessage("该道具并不存在，请检查是否输错！");
                        break;
                    }

                    if (target == null)
                    {
                        foreach (var player in Players[receiver.GroupId].Values) player.Bag.MergeValue(item.Id, count);
                    }
                    else
                    {
                        var group = Players[receiver.GroupId];
                        if (group.TryGetValue(target, out var player))
                        {
                            player.Bag.MergeValue(item.Id, count);
                        }
                        else
                        {
                            receiver.SendAtMessage("对方未注册，无法奖励！");
                            break;
                        }
                    }

                    await receiver.SendMessageAsync($"已奖励{itemName}*{count}");
                }
                else if (strMess.StartsWith("选择"))
                {
                    if (!(strMess.Length > 2 && int.TryParse(strMess[2..], out var decidedCount) &&
                          decidedCount > 0)) return;

                    var player = Player.Register(receiver);

                    if (player.GachaPets == null) return;

                    if (player.Pet != null)
                    {
                        receiver.SendAtMessage("您已经有宠物了,贪多嚼不烂哦!\n◇指令:宠物放生");
                        break;
                    }


                    var pet = player.GachaPets[decidedCount - 1];

                    player.Pet = pet;
                    player.GachaPets = null;

                    await receiver.SendMessageAsync(new MessageChainBuilder()
                        .At(memberId)
                        .Plain($" 恭喜您砸到了一颗{pet.Attribute}属性的宠物蛋")
                        .ImageFromBase64(ToBase64(pet.Render()))
                        .Build());
                }
                else if (strMess.StartsWith("扣除") && HavePermissions(memberId))
                {
                    Tools.ParseString(chain, 2, out var itemName, out var count, out var target);

                    if (count != -1 && count == 0)
                    {
                        receiver.SendAtMessage("格式错误！");
                        break;
                    }

                    var item = FindItem(itemName);

                    if (item == null)
                    {
                        receiver.SendAtMessage("该道具并不存在，请检查是否输错！");
                        break;
                    }

                    var players = Players[receiver.GroupId];
                    var succeedCount = 0;
                    var failCount = 0;

                    if (target == null)
                    {
                        foreach (var player in players.Values)
                        {
                            if (player.Bag[item.Id] < count)
                            {
                                failCount++;
                                continue;
                            }

                            player.Bag.MergeValue(item.Id, -count);
                            succeedCount++;
                        }

                        receiver.SendAtMessage($"扣除完毕\n成功数：{succeedCount}\n失败数：{failCount}");
                        break;
                    }

                    {
                        if (players.TryGetValue(target, out var player))
                        {
                            if (player.Bag[item.Id] >= count) player.Bag.MergeValue(item.Id, -count);
                        }
                        else
                        {
                            receiver.SendAtMessage("对方未注册，无法扣除！");
                            break;
                        }
                    }

                    await receiver.SendMessageAsync($"已扣除{itemName}*{count}");
                }
                else if (strMess.StartsWith("宠物商店"))
                {
                    var count = 1;
                    if (strMess.Length > 4) _ = int.TryParse(strMess[4..], out count);

                    if (count > 99999)
                    {
                        receiver.SendAtMessage("数量超出范围！");
                        break;
                    }

                    if (count < 0) count = 1;

                    using var bitmap = PointShop.Render(count);
                    receiver.SendBmpMessage(bitmap);
                }
                else if (strMess.StartsWith("查看"))
                {
                    var itemName = strMess[2..];
                    var item = FindItem(itemName);
                    if (item == null)
                    {
                        receiver.SendAtMessage("此物品不存在，或者输入错误！");
                        break;
                    }

                    MessageChainBuilder builder = new();

                    if (item.DescriptionImageName != null)
                        builder.ImageFromPath(
                            Path.GetFullPath($"./datapack/itemicon/{item.DescriptionImageName}.png"));

                    builder.Plain($"{item.Name}：{item.Description}");

#if DEBUG
                    builder.Plain("\n" + item.GetType());
#endif

                    await receiver.SendMessageAsync(builder.Build());
                }
                else if (strMess.StartsWith("合成"))
                {
                    if (strMess.Length < 3)
                    {
                        receiver.SendAtMessage("◇指令:合成+道具*数量");
                        break;
                    }

                    Tools.ParseString(chain, 2, out var itemName, out var count, out _);
                    if (!IsCompliant(receiver, count)) break;

                    var item = FindItem(itemName);
                    if (item == null)
                    {
                        receiver.SendAtMessage("此物品不存在，或者输入错误！");
                        break;
                    }

                    if (item.Make(receiver, count))
                        await receiver.SendMessageAsync(new MessageChainBuilder()
                            .ImageFromPath(Path.GetFullPath($"./itemicon/{item.DescriptionImageName}.png"))
                            .At(memberId)
                            .Plain($" 合成成功了！恭喜你获得了道具·{itemName}*{count}")
                            .Build());
                }
                else if (strMess.StartsWith("领取"))
                {
                    Tools.ParseString(chain, 2, out var giftName, out _, out _);
                    if (strMess.Length < 3)
                    {
                        receiver.SendAtMessage("◇指令:领取+礼包名");
                        break;
                    }

                    var gift = FindGift(giftName);
                    if (gift == null)
                    {
                        receiver.SendAtMessage("此礼包不存在，或者输入错误！");
                        break;
                    }

                    var player = Player.Register(receiver);

                    if (player.ClaimedGifts.Contains(gift.Id))
                    {
                        receiver.SendAtMessage("你已领取过该礼包，不可重复领取");
                        break;
                    }

                    if (gift.Level != 0 && !(HavePet(receiver, out var pet) && pet.Level >= 0)) break;

                    List<string> message = new();
                    foreach (var item in gift.Items)
                    {
                        message.Add($"{Items[item.Id].Name}*{item.Count}"); 
                        player.Bag.MergeValue(item.Id, item.Count);
                    }

                    player.ClaimedGifts.Add(gift.Id);

                    receiver.SendAtMessage("领取成功\n" + string.Join("\n", message));
                }
                else if (strMess.StartsWith("宠物副本"))
                {
                    var index = strMess[4..].GetCount("");
                    if (index == -1) index = 0;

                    var replicasString =
                        Replicas.ConvertAll(replica => $"● {replica.Name} LV > {replica.Level}");
                    using Font font = new("Microsoft YaHei", 23, FontStyle.Regular);
                    using Bitmap bitmap = new(480, 640);
                    using var graphics = Graphics.FromImage(bitmap);
                    using Pen pen = new(Color.Black, 3);
                    graphics.Clear(Color.White);
                    graphics.DrawString("当前开放副本如下：", font, Brushes.Black, 5, 5);
                    graphics.DrawLine(pen, 0, 60, 480, 60);
                    graphics.DrawLine(pen, 0, 498, 235, 498);
                    var n = 60;
                    foreach (var text in replicasString.SafeGetRange(index, 10))
                        graphics.DrawString(text, font, Brushes.Black, 0, n);

                    receiver.SendBmpMessage(bitmap);
                }
                else if (strMess.StartsWith("进入副本"))
                {
                    Tools.ParseString(chain, 4, out var replicaName, out var count, out _);
                    var replica = FindReplica(replicaName);
                    if (replica == null || !HavePet(receiver)) break;

                    var player = Player.Register(receiver);
                    if (!HavePet(receiver)) break;

                    _ = replica.Challenge(player, count, out var expAdd, out var pointAdd);
                    
                    using Bitmap bitmap = new(600, 205);
                    using var graphics = Graphics.FromImage(bitmap);
                    using Font font = new("Microsoft YaHei", 23, FontStyle.Regular);
                    graphics.Clear(Color.White);
                    graphics.DrawString($"【{player.Pet.Name} VS {replica.enemyName}】", font, Brushes.Black, 180,
                        15);
                    graphics.DrawString($"◆战斗结果：胜利\n◆获得经验：{expAdd}\n◆获得积分：{pointAdd}", font, Brushes.Black, 15, 55);
                    graphics.DrawString(
                        $"◆消耗精力：{replica.Energy * count}\n◆血量减少：{replica.Attack * count}\n◆获得积分：{(player.Pet.Health == 0 ? "死亡" : "正常")}",
                        font, Brushes.Black, 305, 55);

                    MessageChainBuilder builder = new();
                    builder.ImageFromBase64(ToBase64(bitmap));
                    if (replica.IconName != null)
                        builder.ImageFromPath(Path.GetFullPath($"./datapack/replicaicon/{replica.IconName}.png"));

                    await receiver.SendMessageAsync(builder.Build());
                }
                else if (strMess.StartsWith("宠物攻击"))
                {
                    var target = GetAtNumber(chain);
                    if (target == null) break;

                    var player = Player.Register(receiver);
                    var tPlayer = Player.Register(groupId, target);
                    if (!HavePet(groupId, memberId)) break;

                    if (tPlayer.Pet == null)
                    {
                        receiver.SendAtMessage("对方并没有宠物，无法对目标发起攻击！");
                        break;
                    }

                    var attack = tPlayer.Pet.Damage(player.Pet.Attack, player.Pet.Intellect);
                    tPlayer.Pet.Health -= attack;
                    await receiver.SendMessageAsync(
                        $"【{player.Pet.Name} VS {tPlayer.Pet.Name}】\n" +
                        $"属性:[{player.Pet.Attribute}] -- [{tPlayer.Pet.Attribute}]\n" +
                        //TODO:完善结果
                        $"你的宠物直接KO对方宠物\n" +
                        $"● 经验：+0\n" +
                        $"---------------\n" +
                        $"对方血量扣除：-{attack}\n" +
                        $"我方血量扣除：-0\n" +
                        $"对方剩余血量：{tPlayer.Pet.Health}\n" +
                        $"我方剩余血量：{player.Pet.Health}");
                }

                break;
        }
    }

    public static int GetLevelUpExp(int level)
    {
        return 5 * Pow(level, 3) + 15 * Pow(level, 2) + 40 * level + 100;
    }

    public static void CoverLine(string text = "")
    {
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.WriteLine(text);
    }

    public static bool RandomBool()
    {
        return Random.Next(0, 2) == 1;
    }

    

    private static void WriteConfig()
    {
        const string path = "./config.json";
        Config config = new()
        {
            Address = _address,
            QNumber = _qqNumber,
            VerifyKey = _verifyKey,
            MasterId = _masterId,
            Admins = _admins,
            GroupList = _groupList,
            BlackListMode = _blackListMode,
            NotRunningGroup = _notRunningGroup
        };
        var json = config.ToJsonString();
        File.WriteAllText(path, json);
    }

    private static void ReadConfig()
    {
        const string path = "./config.json";
        var json = File.ReadAllText(path);
        var config = JsonConvert.DeserializeObject<Config>(json);

        if (config == null) return;

        _address = config.Address;
        _qqNumber = config.QNumber;
        _verifyKey = config.VerifyKey;
        _masterId = config.MasterId;
        _admins = config.Admins;
        _groupList = config.GroupList;
        _blackListMode = config.BlackListMode;
        _notRunningGroup = config.NotRunningGroup;
        _bootText = config.BootText;
    }

    private static bool HavePermissions(string id)
    {
        return _admins.Contains(id) || _masterId == id;
    }

    public static string? GetAtNumber(MessageChain messageChain)
    {
        var atMessages = messageChain.OfType<AtMessage>().ToList();
        return atMessages.Count != 0 ? atMessages[0].Target : null;
    }

    private static string GetQNumber()
    {
        for (;;)
        {
            Console.Write("QQ号：");
            var qqNumber = Console.ReadLine();
            if (long.TryParse(qqNumber, out _)) return qqNumber;
            Console.WriteLine("请输入正确的QQ号！");
        }
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

    private static bool IsCompliant(GroupMessageReceiver receiver, int count)
    {
        switch (count)
        {
            case -1:
                return true;
            case 0:
                receiver.SendAtMessage("格式错误！");
                return false;
            case > 99999:
                receiver.SendAtMessage("数量超出范围！");
                return false;
            default:
                return true;
        }
    }

    public static void KeysExit()
    {
        Console.Write("按任意键退出…");
        Console.ReadKey(true);
        Environment.Exit(0);
    }

    public static string ToBase64(Image bmp)
    {
        using MemoryStream stream = new();
        bmp.Save(stream, ImageFormat.Png);
        var array = stream.ToArray();

        return Convert.ToBase64String(array);
    }

    public static int Pow(int x, int y)
    {
        return (int)Math.Pow(x, y);
    }

    private static void Initialize()
    {
        Console.Write("连接地址（默认为localhost:8080）：");
        _address = Console.ReadLine();
        if (_address == string.Empty) _address = "localhost:8080";

        _qqNumber = GetQNumber();
        Console.Write("验证密钥：");
        _verifyKey = Console.ReadLine();
    }

    #region 发送消息

    public static async void SendAtMessage(string groupId, string memberId, string message)
    {
        await SendMessage(groupId, new MessageChainBuilder().At(memberId).Plain(" " + message).Build());
    }

    public static async void SendBmpMessage(string groupId, Image imageData)
    {
        await SendMessage(groupId, new MessageChainBuilder().ImageFromBase64(ToBase64(imageData)).Build());
    }

    public static async Task SendMessage(string groupId, MessageChain message)
    {
        await MessageManager.SendGroupMessageAsync(groupId, message);
    }

    #endregion 发送消息
}