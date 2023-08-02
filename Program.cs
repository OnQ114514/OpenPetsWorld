using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Sessions;
using Mirai.Net.Sessions.Http.Managers;
using Mirai.Net.Utils.Scaffolds;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Timers;
using Manganese.Text;
using static OpenPetsWorld.OpenPetsWorld;
using Timer = System.Timers.Timer;
using OpenPetsWorld.Item;

namespace OpenPetsWorld
{
    internal static class Program
    {
        #region 关闭保存

        private delegate bool ControlCtrlDelegate(int CtrlType);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ControlCtrlDelegate HandlerRoutine, bool Add);

        private static readonly ControlCtrlDelegate cancelHandler = HandlerRoutine;

        private static bool HandlerRoutine(int CtrlType)
        {
            WriteConfig();
            SaveData();
            Environment.Exit(0);
            return false;
        }

        #endregion

        private static readonly Font Font = new("微软雅黑", 20, FontStyle.Regular);
        private static readonly SolidBrush brush = new(Color.Black);
        private static string? _address;
        private static string? _verifyKey;
        private static string? _qqNumber;
        private static List<string> _groupList = new();
        private static bool _blackListMode = false;
        private const string MasterId = "58554566";
        private static readonly HttpClient HttpClient = new();
        public static readonly Random Random = new();
        public static readonly Logger Log = new();

        private static async Task Main()
        {
            Console.Title = "OpenPetWorld控制台";

            if (File.Exists("./config.txt"))
            {
                #region ReadConfig

                try
                {
                    string[] configs = await File.ReadAllLinesAsync("./config.txt");
                    _address = configs[0][8..];
                    _qqNumber = configs[1][9..];
                    _verifyKey = configs[2][10..];
                    _groupList = configs[3][11..].Split(',').ToList();
                }
                catch
                {
                    File.Delete("./config.txt");
                    Console.WriteLine("读取配置文件错误！已删除配置文件");
                    KeysExit();
                }

                #endregion
            }
            else
            {
                #region Initialize

                Console.Write("连接地址（默认为localhost:8080）：");
                _address = Console.ReadLine();
                if (_address == string.Empty)
                {
                    _address = "localhost:8080";
                }

                _qqNumber = GetQNumber();
                Console.Write("验证密钥：");
                _verifyKey = Console.ReadLine();

                #endregion
            }

            #region Start

            MiraiBot bot;
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
                KeysExit();
                return;
            }

            #endregion

            Log.Info("已连接至Mirai");

            #region WriteConfig

            if (!File.Exists("./config.txt"))
            {
                WriteConfig();
            }

            #endregion

#if DEBUG
            if (!File.Exists("./datapack/PetPool.json"))
            {
                List<Pet> pool = new()
                {
                    new(),
                    new(),
                    new()
                };
                await File.WriteAllTextAsync("./datapack/PetPool.json", pool.ToJsonString());
                Log.Info("测试宠物池已生成");
            }

            if (!File.Exists("./datapack/replicas.json"))
            {
                List<Replica> replicas = new()
                {
                    new()
                    {
                        Name = "测试副本",
                        enemyName = "测试敌人",
                        RewardingPoint = 1145,
                        Attack = 1,
                        RewardingItems = new()
                        {
                            { 1, 1 }
                        }
                    }
                };
                await File.WriteAllTextAsync("./datapack/replicas.json", replicas.ToJsonString());
                Log.Info("测试副本已生成");
            }

            if (!File.Exists("./datapack/Items.json"))
            {
                Dictionary<int, BaseItem> lItems = new()
                {
                    {
                        1, new()
                        {
                            Name = "测试材料",
                            Id = 1,
                            descriptionImageName = "test",
                            description = "I'm a item"
                        }
                    }
                };
                await File.WriteAllTextAsync("./datapack/Items.json", lItems.ToJsonString());
                Log.Info("测试物品已生成");
            }
#endif

            ReadData();
            Timer timer = new(60000)
            {
                Enabled = true,
                AutoReset = true
            };
            timer.Elapsed += EnergyRecovery;

            bot.MessageReceived
                .OfType<GroupMessageReceiver>()
                .Where(x =>
                {
                    bool whiteList = _groupList.Contains(x.GroupId);
                    return !_blackListMode ? whiteList : !whiteList;
                })
                .Subscribe(OnMessage);

            SetConsoleCtrlHandler(cancelHandler, true);

            for (;;)
            {
                Console.Write("> ");
                string? userInput = Console.ReadLine();
                switch (userInput)
                {
                    case "/clearconfig":
                        File.Delete("./config.txt");
                        Console.WriteLine("已清除配置");
                        break;
                    case "/stop":
                        WriteConfig();
                        SaveData();
                        KeysExit();
                        break;
                    case "/stop!":
                        return;
                    case "/help":
                        Console.WriteLine("——————帮助菜单——————\n" +
                                          "/clearconfig 清除配置文件\n" +
                                          "/stop 退出程序\n" +
                                          "/stop! 不保存数据退出\n" +
                                          "/AddGroup {群号} 添加群\n" +
                                          "/DelGroup {群号} 删除群\n" +
                                          "/GroupList 列出所有已添加群");
                        break;
                    case "/GroupList":
                        Console.WriteLine(string.Join("\n", _groupList));
                        break;
                    case "/reload":
                        Reload();
                        Console.WriteLine("已重载数据");
                        break;
                    case "":
                        break;
                    default:
                        if (userInput == null)
                        {
                            break;
                        }

                        if (userInput.StartsWith("/AddGroup "))
                        {
                            string LGroupId = userInput[10..];
                            if (!long.TryParse(LGroupId, out _))
                            {
                                Console.WriteLine("请输入正确的群号！");
                                break;
                            }

                            _groupList.Add(LGroupId);
                            break;
                        }
                        else if (userInput.StartsWith("/DelGroup "))
                        {
                            string LGroupId = userInput[10..];
                            if (!long.TryParse(LGroupId, out _) && !_groupList.Remove(LGroupId))
                            {
                                Console.WriteLine("请输入正确的群号！");
                            }

                            break;
                        }

                        Console.WriteLine($"未知命令\"{userInput}\"，请输入/help查看命令");
                        break;
                }
            }
        }

        private static async void OnMessage(GroupMessageReceiver x)
        {
            string groupId = x.GroupId;
            string memberId = x.Sender.Id;
            MessageChain OriginalMess = x.MessageChain;
            string strMess = OriginalMess.GetPlainMessage();

            switch (strMess)
            {
                case "宠物世界":
                {
                    string menuPath = "./datapack/menu.png";

                    if (!File.Exists(menuPath))
                    {
                        Log.Warn("菜单图片不存在");
                        break;
                    }

                    string fullPath = Path.GetFullPath(menuPath);

                    await x.SendMessageAsync(new MessageChainBuilder().ImageFromPath(fullPath).Build());
                    break;
                }
                case "我的宠物":
                {
                    if (HavePet(groupId, memberId, out Pet? p))
                    {
                        Image imageData;
                        Graphics graphics;
                        try
                        {
                            imageData = Image.FromFile("./datapack/wallpaper.jpg");
                            graphics = Graphics.FromImage(imageData);
                            graphics.DrawImage(Image.FromFile($"./datapack/peticon/{p.IconName}"), 5, 5, 380, 380);
                        }
                        catch
                        {
                            Log.Error("绘制宠物图片时未找到图片或绘制错误");
                            break;
                        }

                        string[] abTexts =
                        {
                            $"心情:{p.GetMoodSymbol()}",
                            $"精力:{p.Energy}/{p.MaxEnergy}",
                            $"血量:{p.Health}/{p.MaxHealth}",
                            $"经验:{p.Experience}/{p.MaxExperience}"
                        };
                        int n = 390;
                        foreach (string abText in abTexts)
                        {
                            graphics.DrawString(abText, Font, brush, 15, n);
                            n += 25;
                        }

                        string[] abTexts2 =
                        {
                            $"等级:{p.Level}",
                            $"昵称:{p.Name}",
                            $"性别:{p.Gender}",
                            $"阶段:{p.Stage}",
                            $"属性:{p.Attribute}",
                            $"级别:{p.Rank}",
                            $"状态:{p.State}",
                            "神器:",
                            $"天赋:{p.PettAlent}",
                            $"战力:{p.Power}",
                            $"智力:{p.Intellect}",
                            $"攻击:{p.Attack}",
                            $"防御:{p.Defense}"
                        };
                        abTexts2[7] += p.Artifact?.Name ?? "无";

                        int n2 = 20;
                        foreach (string abText in abTexts2)
                        {
                            graphics.DrawString("◆" + abText, Font, brush, 395, n2);
                            n2 += 35;
                        }

                        SendBmpMessage(groupId, imageData);
                    }

                    break;
                }
                case "砸蛋":
                {
                    Player player = Player.Register(x);
                    if (player.Pet == null)
                    {
                        if (player.Points < 500)
                        {
                            x.SendAtMessage("您的积分不足,无法进行砸蛋!\n【所需[500]积分】\n请发送【签到】获得积分");
                            break;
                        }

                        player.Points -= 500;
                        Pet petData;
                        try
                        {
                            petData = Pet.Extract();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            break;
                        }

                        player.Pet = petData;
                        Players[groupId][memberId] = player;
                        x.SendAtMessage($"恭喜您砸到了一颗{petData.Attribute}属性的宠物蛋");
                    }
                    else
                    {
                        x.SendAtMessage("您已经有宠物了,贪多嚼不烂哦!\n◇指令:宠物放生");
                    }

                    break;
                }
                case "修炼":
                {
                    Player player = Player.Register(x);
                    if (player.CanActivity(x) && HavePet(x))
                    {
                        player.LastActivityUnixTime = GetNowUnixTime();
                        Pet? petData = Players[groupId][memberId].Pet;
                        petData.Energy -= 10;
                        int addExp = Random.Next(250, 550);
                        petData.Experience += addExp;
                        Players[groupId][memberId].Pet = petData;
                        Players[groupId][memberId] = player;
                        SendAtMessage(groupId, memberId,
                            $"您的【{petData.Name}】正在{UnitingPlace[Random.Next(0, UnitingPlace.Length)]}刻苦的修炼！\r\n------------------\r\n·修炼时间：+120秒\r\n·耗费精力：-10点\r\n·增加经验：+{addExp}\n------------------");
                    }

                    break;
                }
                case "学习":
                {
                    Player player = Player.Register(x);
                    if (player.CanActivity(x) && HavePet(x))
                    {
                        Pet? petData = Players[groupId][memberId].Pet;
                        petData.Energy -= 10;
                        petData.Intellect += Random.Next(2, 6);
                        Players[groupId][memberId].Pet = petData;
                        Players[groupId][memberId].LastActivityUnixTime = GetNowUnixTime();
                        SendAtMessage(groupId, memberId,
                            $"您的【{petData.Name}】出门上学啦！\n------------------\n●学习耗时：+120秒\n●减少精力：-10点\n●获得智力：+2\n------------------");
                    }

                    break;
                }
                case "洗髓":
                {
                    Player player = Player.Register(x);
                    if (player.CanActivity(x))
                    {
                        Pet? petData = Players[groupId][memberId].Pet;
                        petData.Energy -= 10;
                        petData.Intellect--;
                        bool addAtt = RandomBool();
                        int addAttNumber = Random.Next(10, 18);
                        string addAttText = addAtt ? "攻击" : "防御";
                        if (addAtt)
                        {
                            petData.Attack += addAttNumber;
                        }
                        else
                        {
                            petData.Defense += addAttNumber;
                        }

                        Players[groupId][memberId].Pet = petData;
                        Players[groupId][memberId].LastActivityUnixTime = GetNowUnixTime();
                        x.SendAtMessage(
                            $"您的【{petData.Name}】正在洗髓伐毛！\n------------------\n●洗髓耗时：+120秒\n●减少精力：-10点\n●减少智力：-1\n●增加{addAttText} ：+{addAttNumber}\n------------------");
                    }

                    break;
                }
                case "宠物升级":
                    if (HavePet(x))
                    {
                        Pet? lPetData = Players[groupId][memberId].Pet;
                        if (lPetData.Experience >= lPetData.MaxExperience)
                        {
                            int OriginalMaxExp = lPetData.MaxExperience;
                            lPetData.Experience -= OriginalMaxExp;
                            lPetData.Level++;
                            int n = lPetData.Level;
                            int MaxExpAdd = 5 * Pow(n, 3) + 15 * Pow(n, 2) + 40 * n + 100;
                            lPetData.MaxExperience = MaxExpAdd;
                            int MaxHealthAdd = 2 * Pow(n, 2) + 4 * n + 10;
                            lPetData.MaxHealth += MaxHealthAdd;
                            int attAndDefAdd = 3 * n + 1;
                            lPetData.Attack += attAndDefAdd;
                            lPetData.Defense += attAndDefAdd;
                            Players[groupId][memberId].Pet = lPetData;
                            x.SendAtMessage($"您的[{lPetData.Name}]成功升级啦!\n-" + "-----------------\n" + "● 等级提升：+1\n" +
                                            $"● 经验减少：-{OriginalMaxExp}\n" + $"● 生命提升：+{MaxHealthAdd}\n" +
                                            $"● 攻击提升：+{attAndDefAdd}\n" + $"● 防御提升：+{attAndDefAdd}\n" +
                                            "● 战力提升：+null\n" + "------------------");
                        }
                    }

                    break;
                case "卸下神器": 
                    if (HavePet(x))
                    {
                        Player player = Player.Register(x);
                        player.Pet.RemoveArtifact(x);
                    }
                    break;
                case "宠物放生":
                    if (HavePet(x))
                    {
                        Pet? LPetsData = Players[groupId][memberId].Pet;
                        x.SendAtMessage(
                            $"危险操作\n（LV·{LPetsData.Level}-{LPetsData.Rank}-{LPetsData.Name}）\n将被放生，请在1分钟内回复：\n【确定放生】");
                        SentTime[memberId] = GetNowUnixTime();
                    }

                    break;
                case "确定放生":
                    if (SentTime.ContainsKey(memberId))
                    {
                        if (GetNowUnixTime() - SentTime[memberId] <= 60)
                        {
                            Players[groupId][memberId].Pet = null;
                            SentTime.Remove(memberId);
                            x.SendAtMessage("成功放生宠物,您的宠物屁颠屁颠的走了!");
                        }
                    }

                    break;
                case "签到":
                {
                    Player player = Player.Register(x);
                    long TodayUnixTime = DateTime.Now.Date.ToUnixTime();
                    if (TodayUnixTime - player.LastSignedUnixTime <= 86400)
                    {
                        x.SendAtMessage("今天已签到过了,明天再来吧!");
                        break;
                    }

                    if (TodayUnixTime - player.LastSignedUnixTime >= 172800 && player.LastSignedUnixTime != 0)
                    {
                        player.ContinuousSignedDays = 0;
                    }
                    else
                    {
                        player.ContinuousSignedDays++;
                    }

                    player.Points += 5500;
                    player.LastSignedUnixTime = GetNowUnixTime();
                    player.SignedDays++;
                    Players[groupId][memberId] = player;

                    #region 绘制图片

                    Stream stream =
                        await HttpClient.GetStreamAsync($"https://q2.qlogo.cn/headimg_dl?dst_uin={memberId}&spec=100");
                    Bitmap ImageData = new(230, 90);
                    Graphics sourcegra = Graphics.FromImage(ImageData); //存入画布
                    sourcegra.Fill(Color.White, ImageData);
                    sourcegra.DrawImage(Image.FromStream(stream), new Rectangle(0, 0, 90, 90));
                    sourcegra.ClearText();
                    sourcegra.DrawString(x.Sender.MemberProfile.NickName, new Font("微软雅黑", 15, FontStyle.Bold),
                        Brushes.Black, new Point(95, 5));
                    Font sSignFont = new("微软雅黑", 13, FontStyle.Regular);
                    string[] SignTexts2 =
                        { 5500.ToString(), player.SignedDays.ToString(), $"{player.ContinuousSignedDays}/30" };
                    int n = 30;
                    for (int i = 0; i < 3; i++)
                    {
                        string SignText = SignTexts[i];
                        string SignText2 = SignTexts2[i];
                        sourcegra.DrawString($"{SignText}：{SignText2}", sSignFont, Brushes.Black, new Point(95, n));
                        n += 18;
                    }

                    #endregion

                    x.SendBmpMessage(ImageData);
                    break;
                }
                case "我的资产":
                {
                    Player player = Player.Register(x);
                    Bitmap bitmap = new(480, 235);
                    Graphics graphics = Graphics.FromImage(bitmap);
                    graphics.Fill(Brushes.White, bitmap);
                    Font font = new("Microsoft YaHei", 23, FontStyle.Bold);
                    graphics.DrawString($"[{memberId}]您的财富信息如下：", font, brush, 2, 2);
                    graphics.DrawLine(new Pen(Color.Black, 3), new Point(0, 55), new Point(480, 55));
                    graphics.DrawString($"●积分：{player.Points}", font, brush, 0, 65);
                    graphics.DrawString($"●点券：{player.Bonds}", font, brush, 0, 125);
                    graphics.DrawLine(new Pen(Color.Black, 3), new Point(0, 180), new Point(480, 180));
                    x.SendBmpMessage(bitmap);
                    break;
                }
                case "我的背包":
                {
                    Font font = new("Microsoft YaHei", 23, FontStyle.Regular);
                    Player player = Player.Register(x);
                    List<string> bagItemList = new();
                    foreach (var bagItem in player.Bag)
                    {
                        BaseItem item = Items[bagItem.Key];
                        string StrItemType = item.ItemType.ToStr();

                        int count = bagItem.Value;
                        if (count != 0)
                        {
                            bagItemList.Add($"●[{StrItemType}]:{item.Name}⨉{count}");
                        }
                    }

                    if (bagItemList.Count == 0)
                    {
                        x.SendAtMessage("您的背包里面空空如也哦！");
                        break;
                    }

                    int height = bagItemList.Count * 38 + 110;
                    Bitmap ImageData = new(480, height);
                    Graphics graphics = Graphics.FromImage(ImageData);
                    graphics.Fill(Brushes.White, ImageData);
                    graphics.DrawString($"[{memberId}]您的背包：", font, brush, 2, 2);
                    graphics.DrawLine(new Pen(Color.Black, 3), new Point(0, 55), new Point(480, 55));
                    int i = 65;
                    foreach (string itemStr in bagItemList)
                    {
                        graphics.DrawString(itemStr, font, brush, 0, i);
                        i += 38;
                    }

                    graphics.DrawLine(new Pen(Color.Black, 3), new Point(0, height - 30), new Point(480, height - 30));
                    x.SendBmpMessage(ImageData);
                    break;
                }
                default:
                    if (strMess.StartsWith("使用"))
                    {
                        string ItemName = strMess[2..];
                        int count = ItemName.GetCount(ref ItemName);
                        Player player = Player.Register(x);
                        BaseItem? item;

                        if (count != -1)
                        {
                            if (count == 0)
                            {
                                x.SendAtMessage("格式错误！");
                                break;
                            }

                            if (count > 99999)
                            {
                                x.SendAtMessage("数量超出范围！");
                                break;
                            }

                            item = FindItem(ItemName[..count]);
                        }
                        else
                        {
                            item = FindItem(ItemName);
                        }


                        if (item == null)
                        {
                            x.SendAtMessage("该道具并不存在，请检查是否输错！");
                            break;
                        }

                        if (!player.Bag.ContainsKey(item.Id))
                        {
                            player.Bag[item.Id] = 0;
                            Players[groupId][memberId] = player;
                        }

                        item.Use(x, count);
                        //UseItemEvent(GroupId, MemberId, item, count);
                    }
                    else if (strMess.StartsWith("查看"))
                    {
                        string ItemName = strMess[2..];
                        BaseItem? item = FindItem(ItemName);
                        if (item == null)
                        {
                            x.SendAtMessage("此物品不存在，或者输入错误！");
                            break;
                        }

                        MessageChainBuilder builder = new();

                        if (item.descriptionImageName != null)
                        {
                            builder.ImageFromPath(
                                Path.GetFullPath($"./datapack/itemicon/{item.descriptionImageName}.png"));
                        }

                        builder.Plain(item.description ?? "该物品无描述");

                        await x.SendMessageAsync(builder.Build());
                    }
                    else if (strMess.StartsWith("宠物副本"))
                    {
                        int index = strMess[4..].GetCount("");
                        if (index == -1)
                        {
                            index = 0;
                        }

                        List<string> ReplicasString =
                            Replicas.ConvertAll(replica => $"● {replica.Name} LV > {replica.Level}");
                        Font font = new("Microsoft YaHei", 23, FontStyle.Regular);
                        Bitmap bitmap = new(480, 640);
                        Graphics graphics = Graphics.FromImage(bitmap);
                        graphics.Fill(Brushes.White, bitmap);
                        graphics.DrawString("当前开放副本如下：", font, Brushes.Black, 5, 5);
                        Pen pen = new(Color.Black, 3);
                        graphics.DrawLine(pen, 0, 60, 480, 60);
                        graphics.DrawLine(pen, 0, 498, 235, 498);
                        int n = 55;
                        foreach (var text in ReplicasString.SafeGetRange(index, 10))
                        {
                            graphics.DrawString(text, font, Brushes.Black, 0, n);
                        }

                        x.SendBmpMessage(bitmap);
                        Log.Info($"为群({groupId})成员({memberId})完成宠物副本绘制");
                    }
                    else if (strMess.StartsWith("进入副本"))
                    {
                        string ReplicaName = strMess[4..];
                        int count = ReplicaName.GetCount(ref ReplicaName);
                        Replica? replica = FindReplica(ReplicaName);
                        if (replica == null || !HavePet(x))
                        {
                            break;
                        }

                        Player player = Player.Register(x);
                        if (!HavePet(x))
                        {
                            break;
                        }

                        _ = replica.Challenge(player, count);
                        Bitmap bitmap = new(600, 205);
                        int expAdd = replica.ExpAdd * count;
                        int points = replica.RewardingPoint * count;
                        Graphics graphics = Graphics.FromImage(bitmap);
                        Font font = new("Microsoft YaHei", 23, FontStyle.Regular);
                        graphics.Fill(Brushes.White, bitmap);
                        graphics.DrawString($"【{player.Pet.Name} VS {replica.enemyName}】", font, Brushes.Black, 180,
                            15);
                        graphics.DrawString($"◆战斗结果：胜利\n◆获得经验：{expAdd}\n◆获得积分：{points}", font, Brushes.Black, 15, 55);
                        graphics.DrawString(
                            $"◆消耗精力：{replica.Energy * count}\n◆血量减少：{replica.Attack * count}\n◆获得积分：{(player.Pet.Health == 0 ? "死亡" : "正常")}",
                            font, Brushes.Black, 305, 55);

                        MessageChainBuilder builder = new();
                        builder.ImageFromBase64(ToBase64(bitmap));
                        if (replica.IconName != null)
                        {
                            builder.ImageFromPath(Path.GetFullPath($"./datapack/replicaicon/{replica.IconName}.png"));
                        }

                        await x.SendMessageAsync(builder.Build());
                    }
                    else if (strMess.StartsWith("宠物攻击"))
                    {
                        string? target = GetAtNumber(OriginalMess);
                        if (target == null)
                        {
                            break;
                        }

                        Player player = Player.Register(x);
                        Player tPlayer = Player.Register(groupId, target);
                        if (!HavePet(groupId, memberId))
                        {
                            break;
                        }

                        if (!HavePet(tPlayer))
                        {
                            x.SendAtMessage("对方并没有宠物，无法对目标发起攻击！");
                            break;
                        }


                        int attack = tPlayer.Pet.Damage(player.Pet);
                        tPlayer.Pet.Health -= attack;
                        await x.SendMessageAsync(
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
                        Players[groupId][memberId] = player;
                        Players[groupId][target] = tPlayer;
                    }
                    else if (strMess.StartsWith("给予") && memberId == MasterId)
                    {
                        Player player = Player.Register(x);
                        string itemName = strMess[2..];
                        BaseItem? item = FindItem(itemName);
                        if (item == null)
                        {
                            x.SendAtMessage("该道具并不存在，请检查是否输错！");
                            break;
                        }

                        player.Bag.TryAdd(item.Id, 0);

                        Players[groupId][memberId].Bag[item.Id]++;
                        await x.SendMessageAsync($"已给予{itemName}");
                    }

                    break;
            }
        }

        public static void CoverWriteLine(string text = "")
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine(text);
        }

        public static bool RandomBool()
        {
            return Random.Next(0, 2) == 1;
        }

        static void EnergyRecovery(object? sender, ElapsedEventArgs e)
        {
            foreach (var group in Players)
            {
                string groupId = group.Key;
                foreach (var member in Players[groupId])
                {
                    Players[groupId][member.Key].EnergyAdd();
                }
            }
        }


        static void WriteConfig()
        {
            File.WriteAllLines("./config.txt", new[]
            {
                "Address=" + _address,
                "QQNumber=" + _qqNumber,
                "VerifyKey=" + _verifyKey,
                "RunGroupId=" + string.Join(',', _groupList)
            });
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

        #endregion

        private static string? GetAtNumber(MessageChain messageChain)
        {
            var atMessages = messageChain.OfType<AtMessage>().ToList();
            if (atMessages.Count != 0)
            {
                return atMessages[0].Target;
            }

            return null;
        }

        static string GetQNumber()
        {
            ReInput:
            Console.Write("QQ号：");
            string? qqNumber = Console.ReadLine();
            if (!long.TryParse(qqNumber, out _))
            {
                Console.WriteLine("请输入正确的QQ号！");
                goto ReInput;
            }

            return qqNumber;
        }

        private static void Reload()
        {
            SaveData();
            ReadData();
        }

        public static long GetNowUnixTime()
        {
            return DateTime.UtcNow.ToUnixTime();
        }


        public static void KeysExit()
        {
            Console.Write("按任意键退出…");
            Console.ReadKey(true);
            Environment.Exit(0);
        }

        public static string ToBase64(Image bmp)
        {
            MemoryStream stream = new();
            bmp.Save(stream, ImageFormat.Png);
            byte[] arr = stream.ToArray();
            stream.Close();

            return Convert.ToBase64String(arr);
        }

        public static int Pow(int x, int y) => (int)Math.Pow(x, y);
    }
}