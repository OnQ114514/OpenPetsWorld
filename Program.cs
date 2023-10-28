using Manganese.Text;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Sessions;
using Mirai.Net.Sessions.Http.Managers;
using Mirai.Net.Utils.Scaffolds;
using Newtonsoft.Json;
using OpenPetsWorld.Item;
using OpenPetsWorld.PetTool;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Timers;
using static OpenPetsWorld.OpenPetsWorld;
using Timer = System.Timers.Timer;

namespace OpenPetsWorld
{
    internal static class Program
    {
        #region 关闭保存

        private delegate bool ControlCtrlDelegate(int CtrlType);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ControlCtrlDelegate HandlerRoutine, bool Add);

        private static readonly ControlCtrlDelegate cancelHandler = HandlerRoutine;

        private static bool HandlerRoutine(int ctrlType)
        {
            WriteConfig();
            SaveData();
            Debug.Flush();
            Environment.Exit(0);
            return false;
        }

        #endregion 关闭保存

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

        private static async Task Main()
        {
            Console.Title = "OpenPetWorld控制台";

            string configPath = "./config.json";
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
                Console.Write("连接地址（默认为localhost:8080）：");
                _address = Console.ReadLine();
                if (_address == string.Empty)
                {
                    _address = "localhost:8080";
                }

                _qqNumber = GetQNumber();
                Console.Write("验证密钥：");
                _verifyKey = Console.ReadLine();

                #endregion Initialize
            }

            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            //Log = new($"./{GetNowUnixTime()}.txt");
            Log = new();

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

            #endregion Start

            Log.Info("已连接至Mirai");

            #region WriteConfig

            if (!File.Exists("./config.txt"))
            {
                WriteConfig();
            }

            #endregion WriteConfig

            ReadData();

            #region 生成示例

#if DEBUG
            if (!File.Exists("./datapack/petpool.json"))
            {
                PetPool = new()
                {
                    new(),
                    new(),
                    new()
                };
                await File.WriteAllTextAsync("./datapack/PetPool.json", PetPool.ToJsonString());
                Log.Info("测试宠物池已生成");
            }

            if (!File.Exists("./datapack/replicas.json"))
            {
                Replicas = new()
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
                await File.WriteAllTextAsync("./datapack/replicas.json", Replicas.ToJsonString());
                Log.Info("测试副本已生成");
            }

            if (!File.Exists("./datapack/Items.json"))
            {
                Items = new()
                {
                    {
                        1, new()
                        {
                            Name = "测试材料",
                            Id = 1,
                            DescriptionImageName = "test",
                            Description = "I'm a item"
                        }
                    }
                };
                await File.WriteAllTextAsync("./datapack/Items.json", Items.ToJsonString());
                Log.Info("测试物品已生成");
            }
#endif

            #endregion 生成示例

            Timer timer = new(60000)
            {
                Enabled = true,
                AutoReset = true
            };
            timer.Elapsed += NewEnergyRecovery;

            try
            {
                bot.MessageReceived
                    .OfType<GroupMessageReceiver>()
                    .Where(Filter)
                    .Subscribe(OnMessage);
            }
            catch (Exception e)
            {
                Trace.WriteLine(e);
                Trace.Flush();
                KeysExit();
                throw;
            }

            SetConsoleCtrlHandler(cancelHandler, true);

            for (; ; )
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write("> ");
                string input = Console.ReadLine() ?? string.Empty;
                string[] commands = input.Split(' ');
                switch (commands[0])
                {
                    case "clearconfig":
                        File.Delete("./config.txt");
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

                                    string groupId = commands[2];
                                    if (!long.TryParse(groupId, out _))
                                    {
                                        Console.WriteLine("请输入正确的群号！");
                                        break;
                                    }

                                    _groupList.Add(groupId);
                                    break;
                                }
                            case "del":
                                {
                                    if (commands.Length < 3)
                                    {
                                        CoverLine("参数不足");
                                        break;
                                    }

                                    string groupId = input[10..];
                                    if (!long.TryParse(groupId, out _) && !_groupList.Remove(groupId))
                                    {
                                        Console.WriteLine("请输入正确的群号！");
                                    }

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

                    case "":
                        break;

                    default:
                        Console.WriteLine($"未知命令\"{input}\"，请输入/help查看命令");
                        break;
                }
            }
        }

        private static bool Filter(GroupMessageReceiver x)
        {
            bool whiteList = _groupList.Contains(x.GroupId);
            return _blackListMode ? !whiteList : whiteList;
        }

        private static async void OnMessage(GroupMessageReceiver x)
        {
            string groupId = x.GroupId;
            string memberId = x.Sender.Id;
            MessageChain chain = x.MessageChain;
            string strMess = chain.GetPlainMessage();

            var notRunning = _notRunningGroup.Contains(groupId);
            if (strMess == "开OPW" && HavePermissions(memberId) && notRunning)
            {
                _notRunningGroup.Remove(groupId);
                x.SendAtMessage("本群已开启文字游戏OpenPetsWorld[本游戏完全开源]\nTAKE OFF TOWARD THE DREAM");
            }

            if (notRunning)
            {
                return;
            }

            switch (strMess)
            {
                case "OpenPetsWorld":
                    await x.SendMessageAsync("由OpenPetsWorld Pre.2强力驱动");
                    break;

                case "关OPW":
                    if (HavePermissions(memberId))
                    {
                        _notRunningGroup.Add(groupId);
                        x.SendAtMessage("已关闭OpenPetsWorld");
                    }

                    break;

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
                        if (HavePet(x, out Pet? pet))
                        {
                            var image = pet.Render();
                            x.SendBmpMessage(image);
                        }

                        break;
                    }
                case "砸蛋":
                    {
                        Player player = Player.Register(x);
                        if (player.Pet == null)
                        {
                            if (player.Points < ExtractNeededPoint)
                            {
                                x.SendAtMessage($"您的积分不足,无法进行砸蛋!\n【所需[{ExtractNeededPoint}]积分】\n请发送【签到】获得积分");
                                break;
                            }

                            player.Points -= ExtractNeededPoint;
                            Pet pet;
                            try
                            {
                                pet = Pet.Extract();
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                break;
                            }

                            player.Pet = pet;

                            await x.SendMessageAsync(new MessageChainBuilder()
                                .At(memberId)
                                .Plain($" 恭喜您砸到了一颗{pet.Attribute}属性的宠物蛋")
                                .ImageFromBase64(ToBase64(pet.Render()))
                                .Build());
                        }
                        else
                        {
                            x.SendAtMessage("您已经有宠物了,贪多嚼不烂哦!\n◇指令:宠物放生");
                        }

                        break;
                    }
                case "砸蛋十连":
                    {
                        var player = Player.Register(x);
                        if (player.Pet == null)
                        {
                            int neededPoint = ExtractNeededPoint * 10;
                            if (player.Points < neededPoint)
                            {
                                x.SendAtMessage($"您的积分不足,无法进行砸蛋!\n【所需[{neededPoint}]积分】\n请发送【签到】获得积分");
                                break;
                            }

                            player.Points -= neededPoint;

                            List<string> texts = new();
                            List<Pet> pets = new();
                            for (int i = 0; i < 10; i++)
                            {
                                var pet = Pet.Extract();
                                pets.Add(pet);

                                texts.Add($"[{i + 1}]{pet.Rank}-{pet.Name}");
                            }

                            #region 绘图

                            using Bitmap bitmap = new(480, 480);
                            using var graphics = Graphics.FromImage(bitmap);
                            using Font font = new("Microsoft YaHei", 23, FontStyle.Regular);

                            graphics.Clear(Color.White);
                            graphics.DrawString($"[@{memberId}]", font, Black, 3, 3);
                            graphics.DrawString("◇指令：选择+数字", font, Black, 3, 440);

                            int y = 40;
                            foreach (var text in texts)
                            {
                                graphics.DrawString(text, font, Black, 3, y);
                                y += 40;
                            }

                            #endregion

                            x.SendBmpMessage(bitmap);
                        }
                        break;
                    }
                case "修炼":
                    {
                        var player = Player.Register(x);
                        if (HavePet(x, out Pet? pet))
                        {
                            if (player.Activity(x, 10)) break;
                            int addExp = Random.Next(MinExpAdd, MaxExpAdd);
                            pet.Experience += addExp;
                            x.SendAtMessage(
                                $"您的【{pet.Name}】正在{UnitingPlace[Random.Next(0, UnitingPlace.Length)]}刻苦的修炼！\r\n------------------\r\n·修炼时间：+120秒\r\n·耗费精力：-10点\r\n·增加经验：+{addExp}\n------------------");
                        }

                        break;
                    }
                case "学习":
                    {
                        Player player = Player.Register(x);
                        if (HavePet(x, out Pet? pet))
                        {
                            if (player.Activity(x, 10)) break;
                            pet.Intellect += Random.Next(MinIQAdd, MaxIQAdd);
                            x.SendAtMessage(
                                $"您的【{pet.Name}】出门上学啦！\n------------------\n●学习耗时：+120秒\n●减少精力：-10点\n●获得智力：+2\n------------------");
                        }

                        break;
                    }
                case "洗髓":
                    {
                        Player player = Player.Register(x);
                        if (HavePet(x, out Pet? pet))
                        {
                            if (player.Activity(x, 10)) break;
                            pet.Intellect--;
                            bool addAttr = RandomBool();
                            int addAttrNumber = Random.Next(MinAttrAdd, MaxAttrAdd);
                            string addAttText = addAttr ? "攻击" : "防御";
                            if (addAttr)
                            {
                                pet.Attack += addAttrNumber;
                            }
                            else
                            {
                                pet.Defense += addAttrNumber;
                            }

                            x.SendAtMessage(
                                $"您的【{pet.Name}】正在洗髓伐毛！\n------------------\n●洗髓耗时：+120秒\n●减少精力：-10点\n●减少智力：-1\n●增加{addAttText} ：+{addAttrNumber}\n------------------");
                        }

                        break;
                    }
                case "宠物进化":
                    {
                        if (HavePet(x, out var pet))
                        {
                            int statusCode = pet.Evolved(out int level);
                            switch (statusCode)
                            {
                                case 0:
                                    MessageChainBuilder builder = new();
                                    string path = Path.GetFullPath($"./datapack/peticon/{pet.IconName}");
                                    if (pet.IconName != null) builder.ImageFromPath(path);
                                    builder.At(memberId)
                                        .Plain($" 你的{pet.Name}成功进化至[LV·{level}级]{pet.Stage.ToStr()}·{pet.Name}]！");
                                    await x.SendMessageAsync(builder.Build());
                                    break;

                                case -1:
                                    x.SendAtMessage("你的宠物暂时无法进化哦！");
                                    break;

                                case -2:
                                    x.SendAtMessage($"你的[{pet.Name}]已达到最高进化形态！！！");
                                    break;

                                case -3:
                                    x.SendAtMessage($"你的[{pet.Name}]等级不足" /*,以下为进化流程："*/);
                                    break;
                            }
                        }

                        break;
                    }
                case "卸下神器":
                    if (HavePet(x))
                    {
                        Player player = Player.Register(x);
                        var pet = player.Pet;
                        x.SendAtMessage(pet.RemoveArtifact(x) ? $"卸下神器成功！没有神器的[{pet.Name}]显得很落寞呢！" : "你的宠物还未佩戴神器！");
                    }

                    break;

                case "宠物放生":
                    if (HavePet(x))
                    {
                        Pet? pet = Player.Register(x).Pet;
                        x.SendAtMessage(
                            $"危险操作\n（LV·{pet.Level}-{pet.Rank}-{pet.Name}）\n将被放生，请在1分钟内回复：\n【确定放生】");
                        SentTime[memberId] = GetNowUnixTime();
                    }

                    break;

                case "确定放生":
                    if (SentTime.ContainsKey(memberId))
                    {
                        if (GetNowUnixTime() - SentTime[memberId] <= 60)
                        {
                            Player.Register(x).Pet = null;
                            SentTime.Remove(memberId);
                            x.SendAtMessage("成功放生宠物,您的宠物屁颠屁颠的走了!");
                        }
                    }

                    break;

                case "签到":
                    {
                        Player player = Player.Register(x);
                        long todayUnixTime = DateTime.Now.Date.ToUnixTime();
                        if (todayUnixTime - player.LastSignedUnixTime <= 86400)
                        {
                            x.SendAtMessage("今天已签到过了,明天再来吧!");
                            break;
                        }

                        if (todayUnixTime - player.LastSignedUnixTime >= 172800 && player.LastSignedUnixTime != 0)
                        {
                            player.ContinuousSignedDays = 0;
                        }
                        else
                        {
                            player.ContinuousSignedDays++;
                        }

                        player.LastSignedUnixTime = todayUnixTime;
                        player.SignedDays++;
                        int points = 2200 + player.SignedDays * 50;
                        player.Points += points;

                        #region 绘图

                        Stream stream =
                            await HttpClient.GetStreamAsync($"https://q2.qlogo.cn/headimg_dl?dst_uin={memberId}&spec=100");
                        using Bitmap imageData = new(230, 90);
                        using var graphics = Graphics.FromImage(imageData); //存入画布
                        graphics.Clear(Color.White);
                        graphics.DrawImage(Image.FromStream(stream), new Rectangle(0, 0, 90, 90));
                        stream.Close();
                        graphics.ClearText();
                        graphics.DrawString(x.Sender.MemberProfile.NickName, new("微软雅黑", 15, FontStyle.Bold),
                            Brushes.Black, new Point(95, 5));
                        using Font font = new("微软雅黑", 13, FontStyle.Regular);
                        string[] signTexts2 =
                        {
                        points.ToString(),
                        player.SignedDays.ToString(),
                        $"{player.ContinuousSignedDays}/31"
                    };
                        int n = 30;
                        for (int i = 0; i < 3; i++)
                        {
                            string signText = SignTexts[i];
                            string signText2 = signTexts2[i];
                            graphics.DrawString($"{signText}：{signText2}", font, Brushes.Black, new Point(95, n));
                            n += 18;
                        }

                        #endregion 绘图

                        x.SendBmpMessage(imageData);
                        break;
                    }
                case "我的资产":
                    {
                        Player player = Player.Register(x);
                        using Bitmap bitmap = new(480, 235);
                        using var graphics = Graphics.FromImage(bitmap);
                        using Font font = new("Microsoft YaHei", 23, FontStyle.Bold);
                        graphics.Clear(Color.White);
                        graphics.DrawString($"[@{memberId}]您的财富信息如下：", font, Black, 2, 2);
                        graphics.DrawLine(new(Color.Black, 3), new(0, 55), new(480, 55));
                        graphics.DrawString($"●积分：{player.Points}", font, Black, 0, 65);
                        graphics.DrawString($"●点券：{player.Bonds}", font, Black, 0, 125);
                        graphics.DrawLine(new(Color.Black, 3), new(0, 180), new(480, 180));
                        x.SendBmpMessage(bitmap);
                        break;
                    }
                case "我的背包":
                    {
                        var player = Player.Register(x);
                        List<string> bagItemList = new();
                        foreach (var bagItem in player.Bag)
                        {
                            BaseItem item = Items[bagItem.Key];
                            string type = item.ItemType.ToStr();

                            int count = bagItem.Value;
                            if (count != 0)
                            {
                                bagItemList.Add($"●[{type}]:{item.Name}⨉{count}");
                            }
                        }

                        if (bagItemList.Count == 0)
                        {
                            x.SendAtMessage("您的背包里面空空如也哦！");
                            break;
                        }

                        #region 绘图

                        int height = bagItemList.Count * 38 + 110;
                        using Bitmap imageData = new(480, height);
                        using var graphics = Graphics.FromImage(imageData);
                        using Font font = new("Microsoft YaHei", 23, FontStyle.Regular);

                        graphics.Clear(Color.White);
                        graphics.DrawString($"[@{memberId}]您的背包：", font, Black, 2, 2);
                        graphics.DrawLine(new(Color.Black, 3), new(0, 55), new(480, 55));

                        int i = 65;
                        foreach (string itemStr in bagItemList)
                        {
                            graphics.DrawString(itemStr, font, Black, 0, i);
                            i += 38;
                        }

                        graphics.DrawLine(new(Color.Black, 3), new(0, height - 30), new(480, height - 30));
                        x.SendBmpMessage(imageData);

                        #endregion 绘图

                        break;
                    }
                case "AllItemList":
                    if (memberId == _masterId)
                    {
                        List<string> message = new();
                        foreach (BaseItem item in Items.Values)
                        {
                            message.Add($"{item.Id} {item.Name}");
                        }

                        await x.SendMessageAsync(string.Join("\n", message));
                    }

                    break;

                default:
                    if (strMess.StartsWith("使用"))
                    {
                        Tools.ParseString(chain, 2, out string itemName, out int count, out string? target);
                        if (!IsCompliant(x, count)) break;

                        var item = FindItem(itemName);

                        if (item == null)
                        {
                            x.SendAtMessage("该道具并不存在，请检查是否输错！");
                            break;
                        }

                        var player = Player.Register(x);
                        player.Bag.TryAdd(item.Id, 0);

                        item.Use(x, count);
                    }
                    else if (strMess.StartsWith("宠物升级"))
                    {
                        int levelsToUpgrade = 1;
                        if (strMess.Length > 4)
                        {
                            _ = int.TryParse(strMess[4..], out levelsToUpgrade);
                        }

                        if (!HavePet(x, out var pet))
                        {
                            break;
                        }

                        int originalPower = pet.Power;
                        int currentLevel = pet.Level;
                        int tempExp = 0;
                        int allExp = 0;
                        int allHealth = 0;
                        int allAttribute = 0;
                        int addedLevel = 0;
                        int nextExpNeeded = 0;

                        for (int i = 0; i < levelsToUpgrade; i++)
                        {
                            int expNeeded = nextExpNeeded = GetLevelUpExp(currentLevel);
                            tempExp = allExp + expNeeded;

                            if (allExp > pet.Experience)
                            {
                                break;
                            }

                            allHealth += 2 * Pow(currentLevel, 2) + 4 * currentLevel + 10;
                            allAttribute += 3 * currentLevel + 1;

                            currentLevel++;
                            addedLevel++;

                            allExp = tempExp;

                        }

                        if (addedLevel == 0)
                        {
                            x.SendAtMessage(
                                $"您的宠物经验不足,无法升级,升级到[Lv·{pet.Level + 1}]级还需要[{pet.MaxExperience - pet.Experience}]经验值!");
                            return;
                        }

                        pet.Level = currentLevel;
                        pet.MaxExperience = nextExpNeeded;
                        pet.Experience -= allExp;
                        pet.MaxHealth += allHealth;
                        pet.Attack += allAttribute;
                        pet.Defense += allAttribute;

                        x.SendAtMessage($"您的[{pet.Name}]成功升级啦!\n"
                            + "------------------\n"
                            + $"● 等级提升：+{addedLevel}\n"
                            + $"● 经验减少：-{allExp}\n"
                            + $"● 生命提升：+{allHealth}\n"
                            + $"● 攻击提升：+{allAttribute}\n"
                            + $"● 防御提升：+{allAttribute}\n"
                            + $"● 战力提升：+{pet.Power - originalPower}\n"
                            + "------------------");

                        /*int originalMaxExp = pet.MaxExperience;
                        if (pet.Experience >= originalMaxExp)
                        {
                            int originalPower = pet.Power;
                            pet.Experience -= originalMaxExp;
                            pet.Level++;
                            int n = pet.Level;// + count;
                            int maxExp = 5 * Pow(n, 3) + 15 * Pow(n, 2) + 40 * n + 100;
                            pet.MaxExperience = maxExp;
                            int maxHealthAdd = 2 * Pow(n, 2) + 4 * n + 10;
                            pet.MaxHealth += maxHealthAdd;
                            int attAndDefAdd = 3 * n + 1;
                            pet.Attack += attAndDefAdd;
                            pet.Defense += attAndDefAdd;
                            x.SendAtMessage($"您的[{pet.Name}]成功升级啦!\n-" + "-----------------\n" + "● 等级提升：+1\n" +
                                            $"● 经验减少：-{originalMaxExp}\n" + $"● 生命提升：+{maxHealthAdd}\n" +
                                            $"● 攻击提升：+{attAndDefAdd}\n" + $"● 防御提升：+{attAndDefAdd}\n" +
                                            $"● 战力提升：+{pet.Power - originalPower}\n" + "------------------");
                        }
                        else
                        {
                            x.SendAtMessage(
                                $"您的宠物经验不足,无法升级,升级到[Lv·{pet.Level + 1}]级还需要[{pet.MaxExperience - pet.Experience}]经验值!");
                        }*/

                        break;
                    }
                    else if (strMess.StartsWith("购买"))
                    {
                        Tools.ParseString(chain, 2, out string itemName, out int count, out _);

                        if (!IsCompliant(x, count)) break;
                        var item = FindItem(itemName);

                        if (item == null)
                        {
                            x.SendAtMessage("该道具并不存在，请检查是否输错！");
                            break;
                        }

                        if (PointShop.Commodities.TryGetValue(item.Id, out var unitPrice))
                        {
                            int price = unitPrice * count;

                            var player = Player.Register(x);
                            bool succeeded = player.Buy(item.Id, count);
                            x.SendAtMessage(succeeded
                                ? $"购买成功！获得{count}个{item.Name},本次消费{price}积分！可发送[我的背包]查询物品！\n物品说明：{item.Description}"
                                : $"你的积分不足[{price}]，无法购买！");
                        }
                        else
                        {
                            x.SendAtMessage("宠物商店内未有此物品的身影！");
                        }
                    }
                    else if (strMess.StartsWith("奖励") && HavePermissions(memberId))
                    {
                        Tools.ParseString(chain, 2, out string itemName, out int count, out string? target);

                        if (count != -1 && count == 0)
                        {
                            x.SendAtMessage("格式错误！");
                            break;
                        }

                        var item = FindItem(itemName);

                        if (item == null)
                        {
                            x.SendAtMessage("该道具并不存在，请检查是否输错！");
                            break;
                        }

                        if (target == null)
                        {
                            foreach (var player in Players[x.GroupId].Values)
                            {
                                player.Bag.MergeValue(item.Id, count);
                            }
                        }
                        else
                        {
                            var group = Players[x.GroupId];
                            if (group.TryGetValue(target, out var player))
                            {
                                player.Bag.MergeValue(item.Id, count);
                            }
                            else
                            {
                                x.SendAtMessage("对方未注册，无法奖励！");
                                break;
                            }
                        }

                        await x.SendMessageAsync($"已奖励{itemName}*{count}");
                    }
                    else if (strMess.StartsWith("扣除") && HavePermissions(memberId))
                    {
                        Tools.ParseString(chain, 2, out string itemName, out int count, out string? target);

                        if (count != -1 && count == 0)
                        {
                            x.SendAtMessage("格式错误！");
                            break;
                        }

                        var item = FindItem(itemName);

                        if (item == null)
                        {
                            x.SendAtMessage("该道具并不存在，请检查是否输错！");
                            break;
                        }

                        var players = Players[x.GroupId];
                        int succeedCount = 0;
                        int failCount = 0;

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

                            x.SendAtMessage($"扣除完毕\n成功数：{succeedCount}\n失败数：{failCount}");
                            break;
                        }
                        else
                        {
                            if (players.TryGetValue(target, out var player))
                            {
                                if (player.Bag[item.Id] >= count)
                                {
                                    player.Bag.MergeValue(item.Id, -count);
                                }
                            }
                            else
                            {
                                x.SendAtMessage("对方未注册，无法扣除！");
                                break;
                            }
                        }

                        await x.SendMessageAsync($"已扣除{itemName}*{count}");
                    }
                    else if (strMess.StartsWith("宠物商店"))
                    {
                        int count = 1;
                        if (strMess.Length > 4)
                        {
                            _ = int.TryParse(strMess[4..], out count);
                        }

                        if (count > 99999)
                        {
                            x.SendAtMessage("数量超出范围！");
                            break;
                        }

                        if (count < 0)
                        {
                            count = 1;
                        }

                        using var bitmap = PointShop.Render(count);
                        x.SendBmpMessage(bitmap);
                    }
                    else if (strMess.StartsWith("查看"))
                    {
                        string itemName = strMess[2..];
                        BaseItem? item = FindItem(itemName);
                        if (item == null)
                        {
                            x.SendAtMessage("此物品不存在，或者输入错误！");
                            break;
                        }

                        MessageChainBuilder builder = new();

                        if (item.DescriptionImageName != null)
                        {
                            builder.ImageFromPath(
                                Path.GetFullPath($"./datapack/itemicon/{item.DescriptionImageName}.png"));
                        }

                        builder.Plain($"{item.Name}：{item.Description}");

#if DEBUG
                        builder.Plain("\n" + item.GetType());
#endif

                        await x.SendMessageAsync(builder.Build());
                    }
                    else if (strMess.StartsWith("合成"))
                    {
                        if (strMess.Length < 3)
                        {
                            x.SendAtMessage("◇指令:合成+道具*数量");
                            break;
                        }

                        Tools.ParseString(chain, 2, out string itemName, out int count, out _);
                        if (!IsCompliant(x, count)) break;

                        BaseItem? item = FindItem(itemName);
                        if (item == null)
                        {
                            x.SendAtMessage("此物品不存在，或者输入错误！");
                            break;
                        }

                        if (item.Make(x, count))
                        {
                            await x.SendMessageAsync(new MessageChainBuilder()
                                .ImageFromPath(Path.GetFullPath($"./itemicon/{item.DescriptionImageName}.png"))
                                .At(memberId)
                                .Plain($" 合成成功了！恭喜你获得了道具·{itemName}*{count}")
                                .Build());
                        }
                    }
                    else if (strMess.StartsWith("领取"))
                    {
                        Tools.ParseString(chain, 2, out string giftName, out _, out _);
                        if (strMess.Length < 3)
                        {
                            x.SendAtMessage("◇指令:领取+礼包名");
                            break;
                        }

                        var gift = FindGift(giftName);
                        if (gift == null)
                        {
                            x.SendAtMessage("此礼包不存在，或者输入错误！");
                            break;
                        }

                        var player = Player.Register(x);

                        if (player.ClaimedGifts.Contains(gift.Id))
                        {
                            x.SendAtMessage("你已领取过该礼包，不可重复领取");
                            break;
                        }

                        if (gift.Level != 0 && !(HavePet(x, out var pet) && pet.Level >= 0))
                        {
                            break;
                        }

                        List<string> message = new();
                        foreach (var item in gift.Items)
                        {
                            message.Add($"{Items[item.Id].Name}*{item.Count}");
                            player.Bag.MergeValue(item.Id, item.Count);
                        }

                        player.ClaimedGifts.Add(gift.Id);

                        x.SendAtMessage("领取成功\n" + string.Join("\n", message));
                    }
                    else if (strMess.StartsWith("宠物副本"))
                    {
                        int index = strMess[4..].GetCount("");
                        if (index == -1)
                        {
                            index = 0;
                        }

                        List<string> replicasString =
                            Replicas.ConvertAll(replica => $"● {replica.Name} LV > {replica.Level}");
                        using Font font = new("Microsoft YaHei", 23, FontStyle.Regular);
                        using Bitmap bitmap = new(480, 640);
                        using var graphics = Graphics.FromImage(bitmap);
                        using Pen pen = new(Color.Black, 3);
                        graphics.Clear(Color.White);
                        graphics.DrawString("当前开放副本如下：", font, Brushes.Black, 5, 5);
                        graphics.DrawLine(pen, 0, 60, 480, 60);
                        graphics.DrawLine(pen, 0, 498, 235, 498);
                        int n = 55;
                        foreach (var text in replicasString.SafeGetRange(index, 10))
                        {
                            graphics.DrawString(text, font, Brushes.Black, 0, n);
                        }

                        x.SendBmpMessage(bitmap);
                    }
                    else if (strMess.StartsWith("进入副本"))
                    {
                        Tools.ParseString(chain, 4, out var replicaName, out int count, out _);
                        Replica? replica = FindReplica(replicaName);
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
                        int expAdd = replica.ExpAdd * count;
                        int points = replica.RewardingPoint * count;
                        using Bitmap bitmap = new(600, 205);
                        using var graphics = Graphics.FromImage(bitmap);
                        using Font font = new("Microsoft YaHei", 23, FontStyle.Regular);
                        graphics.Clear(Color.White);
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
                        string? target = GetAtNumber(chain);
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
                    }

                    break;
            }
        }

        private static int GetLevelUpExp(int level)
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

        private static void NewEnergyRecovery(object? sender, ElapsedEventArgs e)
        {
            foreach (var group in Players.Values)
            {
                foreach (var player in group.Values)
                {
                    player.EnergyAdd();
                }
            }
        }

        private static void WriteConfig()
        {
            string path = "./config.json";
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
            var json = JsonConvert.SerializeObject(config);
            File.WriteAllText(path, json);
        }

        private static void ReadConfig()
        {
            string path = "./config.json";
            var json = File.ReadAllText(path);
            var config = JsonConvert.DeserializeObject<Config>(json);

            _address = config.Address;
            _qqNumber = config.QNumber;
            _verifyKey = config.VerifyKey;
            _masterId = config.MasterId;
            _admins = config.Admins;
            _groupList = config.GroupList;
            _blackListMode = config.BlackListMode;
            _notRunningGroup = config.NotRunningGroup;
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

        private static bool HavePermissions(string id) => _admins.Contains(id) || _masterId == id;

        public static string? GetAtNumber(MessageChain messageChain)
        {
            var atMessages = messageChain.OfType<AtMessage>().ToList();
            if (atMessages.Count != 0)
            {
                return atMessages[0].Target;
            }

            return null;
        }

        private static string GetQNumber()
        {
            for (; ; )
            {
                Console.Write("QQ号：");
                string? qqNumber = Console.ReadLine();
                if (long.TryParse(qqNumber, out _)) return qqNumber;
                Console.WriteLine("请输入正确的QQ号！");
            }
        }

        private static void Reload()
        {
            SaveData();
            ReadData();
        }

        public static long GetNowUnixTime() => DateTime.UtcNow.ToUnixTime();

        private static bool IsCompliant(GroupMessageReceiver receiver, int count)
        {
            if (count != -1)
            {
                if (count == 0)
                {
                    receiver.SendAtMessage("格式错误！");
                    return false;
                }

                if (count > 99999)
                {
                    receiver.SendAtMessage("数量超出范围！");
                    return false;
                }
            }

            return true;
        }

        private static void KeysExit()
        {
            Console.Write("按任意键退出…");
            Console.ReadKey(true);
            Environment.Exit(0);
        }

        public static string ToBase64(Image bmp)
        {
            using MemoryStream stream = new();
            bmp.Save(stream, ImageFormat.Png);
            byte[] arr = stream.ToArray();

            return Convert.ToBase64String(arr);
        }

        private static int Pow(int x, int y) => (int)Math.Pow(x, y);
    }
}