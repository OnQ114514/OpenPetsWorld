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
using Newtonsoft.Json;
using static OpenPetsWorld.OpenPetsWorld;
using Timer = System.Timers.Timer;

namespace OpenPetsWorld
{
    internal class Program
    {
        #region 关闭保存

        public delegate bool ControlCtrlDelegate(int CtrlType);

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

        static string? Address;
        static string? VerifyKey;
        static string? QQNumber;
        static List<string> RunGroupId = new();
        const string MasterId = "58554566";
        static readonly HttpClient httpClient = new();
        public static readonly Random random = new();
        private static readonly Logger log = new();

        static async Task Main(string[] args)
        {
            Console.Title = "OpenPetWorld控制台";

            if (File.Exists("./config.txt"))
            {
                #region ReadConfig

                try
                {
                    string[] Configs = File.ReadAllLines("./config.txt");
                    Address = Configs[0][8..];
                    QQNumber = Configs[1][9..];
                    VerifyKey = Configs[2][10..];
                    RunGroupId = Configs[3][11..].Split(',').ToList();
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
                Address = Console.ReadLine();
                if (Address == string.Empty)
                {
                    Address = "localhost:8080";
                }

                QQNumber = GetQQNumber();
                Console.Write("验证密钥：");
                VerifyKey = Console.ReadLine();

                #endregion
            }

            #region Start

            MiraiBot bot;
            try
            {
                bot = new()
                {
                    Address = Address,
                    QQ = QQNumber,
                    VerifyKey = VerifyKey
                };
                log.Info("正在连接Mirai");
                await bot.LaunchAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                KeysExit();
                return;
            }

            #endregion

            log.Info("已连接至Mirai");

            #region WriteConfig

            if (!File.Exists("./config.txt"))
            {
                WriteConfig();
            }

            #endregion

#if DEBUG
            if (!File.Exists("./datapack/PetPool.json"))
            {
                List<PetData> Pool = new()
                {
                    new(),
                    new(),
                    new()
                };
                File.WriteAllText("./datapack/PetPool.json", Pool.ToJsonString());
                log.Info("测试宠物池已生成");
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
                        enemyAttack = 1,
                        RewardingItems = new()
                        {
                            { 1, 1 }
                        }
                    }
                };
                File.WriteAllText("./datapack/replicas.json", replicas.ToJsonString());
                log.Info("测试副本已生成");
            }

            if (!File.Exists("./datapack/Items.json"))
            {
                Dictionary<int, Item> LItems = new()
                {
                    {
                        1, new()
                        {
                            Name = "测试材料",
                            Id = 1
                        }
                    }
                };
                File.WriteAllText("./datapack/Items.json", LItems.ToJsonString());
                log.Info("测试物品已生成");
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
                .Where(x => RunGroupId.Contains(x.GroupId))
                .Subscribe(async x =>
                {
                    string GroupId = x.GroupId;
                    string MemberId = x.Sender.Id;
                    MessageChain OriginalMess = x.MessageChain;
                    string StrMess = OriginalMess.GetPlainMessage();

#pragma warning disable CS8602 // 解引用可能出现空引用。
                    switch (StrMess)
                    {
                        case "宠物世界":
                        {
                            string menuPath = "./datapack/menu.png";

                            if (!File.Exists(menuPath))
                            {
                                log.Warn("菜单图片不存在");
                                break;
                            }

                            string fullPath = Path.GetFullPath(menuPath);

                            await x.SendMessageAsync(new MessageChainBuilder().ImageFromPath(fullPath).Build());
                            break;
                        }
                        case "我的宠物":
                        {
                            if (HavePet(GroupId, MemberId, out PetData? p))
                            {
                                Image imagedata;
                                Graphics sourcegra;
                                try
                                {
                                    imagedata = Image.FromFile("./datapack/wallpaper.jpg");
                                    sourcegra = Graphics.FromImage(imagedata);
                                    sourcegra.DrawImage(Image.FromFile($"./datapack/peticon/{p.iconName}"), 5, 5, 380,
                                        380);
                                }
                                catch
                                {
                                    log.Error("绘制宠物图片时未找到图片或绘制错误");
                                    break;
                                }

                                string[] AbTexts =
                                {
                                    $"心情:{p.GetMoodSymbol()}",
                                    $"精力:{p.Energy}/{p.MaxEnergy}",
                                    $"血量:{p.Health}/{p.MaxHealth}",
                                    $"经验:{p.Experience}/{p.MaxExperience}"
                                };
                                int n = 390;
                                foreach (string AbText in AbTexts)
                                {
                                    sourcegra.DrawString(AbText, font, fontColor, 15, n);
                                    n += 25;
                                }

                                string[] AbTexts2 =
                                {
                                    $"等级:{p.Level}",
                                    $"昵称:{p.PetName}",
                                    $"性别:{p.PetGender}",
                                    $"阶段:{p.PetStage}",
                                    $"属性:{p.PetAttribute}",
                                    $"级别:{p.PetRank}",
                                    $"状态:{p.PetState}",
                                    "神器:",
                                    $"天赋:{p.PettAlent}",
                                    $"战力:{p.GetPower()}",
                                    $"智力:{p.Intellect}",
                                    $"攻击:{p.Attack}",
                                    $"防御:{p.Defense}"
                                };
                                AbTexts2[7] += p.artifact != null ? p.artifact.Name : "无";

                                int n2 = 20;
                                foreach (string AbText in AbTexts2)
                                {
                                    sourcegra.DrawString("◆" + AbText, font, fontColor, 395, n2);
                                    n2 += 35;
                                }

                                SendBmpMessage(GroupId, imagedata);
                            }

                            break;
                        }
                        case "砸蛋":
                        {
                            PlayerData playerData = Register(GroupId, MemberId);
                            if (playerData.pet == null)
                            {
                                if (playerData.Points < 500)
                                {
                                    x.SendAtMessage("您的积分不足,无法进行砸蛋!\n【所需[500]积分】\n请发送【签到】获得积分");
                                    break;
                                }

                                playerData.Points -= 500;
                                PetData petData;
                                try
                                {
                                    petData = PetData.Extract();
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(e);
                                    break;
                                }

                                playerData.pet = petData;
                                PlayersData[GroupId][MemberId] = playerData;
                                x.SendAtMessage($"恭喜您砸到了一颗{petData.PetAttribute}属性的宠物蛋");
                            }
                            else
                            {
                                x.SendAtMessage("您已经有宠物了,贪多嚼不烂哦!\n◇指令:宠物放生");
                            }

                            break;
                        }
                        case "修炼":
                        {
                            PlayerData playerData = Register(GroupId, MemberId);
                            if (CanActivity(playerData, GroupId, MemberId))
                            {
                                playerData.ActivityCD = GetNowUnixTime();
                                PetData? petData = PlayersData[GroupId][MemberId].pet;
                                petData.Energy -= 10;
                                int AddExp = random.Next(250, 550);
                                petData.Experience += AddExp;
                                PlayersData[GroupId][MemberId].pet = petData;
                                PlayersData[GroupId][MemberId] = playerData;
                                SendAtMessage(GroupId, MemberId,
                                    $"您的【{petData.PetName}】正在{UnitingPlace[random.Next(0, UnitingPlace.Length)]}刻苦的修炼！\r\n------------------\r\n·修炼时间：+120秒\r\n·耗费精力：-10点\r\n·增加经验：+{AddExp}\n------------------");
                            }

                            break;
                        }
                        case "学习":
                        {
                            PlayerData playerData = Register(GroupId, MemberId);
                            if (CanActivity(playerData, GroupId, MemberId))
                            {
                                PetData? petData = PlayersData[GroupId][MemberId].pet;
                                petData.Energy -= 10;
                                petData.Intellect += random.Next(2, 6);
                                PlayersData[GroupId][MemberId].pet = petData;
                                PlayersData[GroupId][MemberId].ActivityCD = GetNowUnixTime();
                                SendAtMessage(GroupId, MemberId,
                                    $"您的【{petData.PetName}】出门上学啦！\n------------------\n●学习耗时：+120秒\n●减少精力：-10点\n●获得智力：+2\n------------------");
                            }

                            break;
                        }
                        case "洗髓":
                        {
                            PlayerData playerData = Register(GroupId, MemberId);
                            if (CanActivity(playerData, GroupId, MemberId))
                            {
                                PetData? petData = PlayersData[GroupId][MemberId].pet;
                                petData.Energy -= 10;
                                petData.Intellect--;
                                bool AddAtt = RandomBool();
                                int AddAttNumber = random.Next(10, 18);
                                string AddAttText = AddAtt ? "攻击" : "防御";
                                if (AddAtt)
                                {
                                    petData.Attack += AddAttNumber;
                                }
                                else
                                {
                                    petData.Defense += AddAttNumber;
                                }

                                PlayersData[GroupId][MemberId].pet = petData;
                                PlayersData[GroupId][MemberId].ActivityCD = GetNowUnixTime();
                                x.SendAtMessage(
                                    $"您的【{petData.PetName}】正在洗髓伐毛！\n------------------\n●洗髓耗时：+120秒\n●减少精力：-10点\n●减少智力：-1\n●增加{AddAttText} ：+{AddAttNumber}\n------------------");
                            }

                            break;
                        }
                        case "宠物升级":
                            if (HavePet(GroupId, MemberId))
                            {
                                PetData? LPetData = PlayersData[GroupId][MemberId].pet;
                                if (LPetData.Experience >= LPetData.MaxExperience)
                                {
                                    int OriginalMaxExp = LPetData.MaxExperience;
                                    LPetData.Experience -= OriginalMaxExp;
                                    LPetData.Level++;
                                    int n = LPetData.Level;
                                    int MaxExpAdd = 5 * Pow(n, 3) + 15 * Pow(n, 2) + 40 * n + 100;
                                    LPetData.MaxExperience = MaxExpAdd;
                                    int MaxHealthAdd = 2 * Pow(n, 2) + 4 * n + 10;
                                    LPetData.MaxHealth += MaxHealthAdd;
                                    int AttAndDefAdd = 3 * n + 1;
                                    LPetData.Attack += AttAndDefAdd;
                                    LPetData.Defense += AttAndDefAdd;
                                    PlayersData[GroupId][MemberId].pet = LPetData;
                                    x.SendAtMessage(
                                        $"您的[{LPetData.PetName}]成功升级啦!\n-" +
                                        "-----------------\n" +
                                        "● 等级提升：+1\n" +
                                        $"● 经验减少：-{OriginalMaxExp}\n" +
                                        $"● 生命提升：+{MaxHealthAdd}\n" +
                                        $"● 攻击提升：+{AttAndDefAdd}\n" +
                                        $"● 防御提升：+{AttAndDefAdd}\n" +
                                        "● 战力提升：+null\n" +
                                        "------------------");
                                }
                            }

                            break;
                        case "宠物放生":
                            if (HavePet(GroupId, MemberId))
                            {
                                PetData? LPetsData = PlayersData[GroupId][MemberId].pet;
                                x.SendAtMessage(
                                    $"危险操作\n（LV·{LPetsData.Level}-{LPetsData.PetRank}-{LPetsData.PetName}）\n将被放生，请在1分钟内回复：\n【确定放生】");
                                SentTime[MemberId] = GetNowUnixTime();
                            }

                            break;
                        case "确定放生":
                            if (SentTime.ContainsKey(MemberId))
                            {
                                if (GetNowUnixTime() - SentTime[MemberId] <= 60)
                                {
                                    PlayersData[GroupId][MemberId].pet = null;
                                    SentTime.Remove(MemberId);
                                    x.SendAtMessage("成功放生宠物,您的宠物屁颠屁颠的走了!");
                                }
                            }

                            break;
                        case "签到":
                        {
                            PlayerData playerData = Register(GroupId, MemberId);
                            int TodayUnixTime = ToUnixTime(DateTime.Now.Date);
                            if (TodayUnixTime - playerData.LastSignedUnixTime <= 86400)
                            {
                                x.SendAtMessage("今天已签到过了,明天再来吧!");
                                break;
                            }

                            if (TodayUnixTime - playerData.LastSignedUnixTime >= 172800 &&
                                playerData.LastSignedUnixTime != 0)
                            {
                                playerData.ContinuousSignedDays = 0;
                            }
                            else
                            {
                                playerData.ContinuousSignedDays++;
                            }

                            playerData.Points += 5500;
                            playerData.LastSignedUnixTime = GetNowUnixTime();
                            playerData.SignedDays++;
                            PlayersData[GroupId][MemberId] = playerData;

                            #region 绘制图片

                            Stream stream =
                                await httpClient.GetStreamAsync(
                                    $"https://q2.qlogo.cn/headimg_dl?dst_uin={MemberId}&spec=100");
                            Bitmap ImageData = new(230, 90);
                            Graphics sourcegra = Graphics.FromImage(ImageData); //存入画布
                            sourcegra.Fill(Color.White, ImageData);
                            sourcegra.DrawImage(Image.FromStream(stream), new Rectangle(0, 0, 90, 90));
                            sourcegra.ClearText();
                            sourcegra.DrawString(x.Sender.MemberProfile.NickName, new Font("微软雅黑", 15, FontStyle.Bold),
                                Brushes.Black,
                                new Point(95, 5));
                            Font sSignFont = new("微软雅黑", 13, FontStyle.Regular);
                            string[] SignTexts2 =
                            {
                                5500.ToString(), playerData.SignedDays.ToString(),
                                $"{playerData.ContinuousSignedDays}/30"
                            };
                            int n = 30;
                            for (int i = 0; i < 3; i++)
                            {
                                string SignText = SignTexts[i];
                                string SignText2 = SignTexts2[i];
                                sourcegra.DrawString($"{SignText}：{SignText2}", sSignFont, Brushes.Black,
                                    new Point(95, n));
                                n += 18;
                            }

                            #endregion

                            SendBmpMessage(GroupId, ImageData);
                            break;
                        }
                        case "我的资产":
                        {
                            PlayerData playerData = Register(GroupId, MemberId);
                            Bitmap bitmap = new(480, 235);
                            Graphics graphics = Graphics.FromImage(bitmap);
                            graphics.Fill(Brushes.White, bitmap);
                            Font font = new("Microsoft YaHei", 23, FontStyle.Bold);
                            graphics.DrawString($"[{MemberId}]您的财富信息如下：", font, fontColor, 2, 2);
                            graphics.DrawLine(new Pen(Color.Black, 3), new Point(0, 55), new Point(480, 55));
                            graphics.DrawString($"●积分：{playerData.Points}", font, fontColor, 0, 65);
                            graphics.DrawString($"●点券：{playerData.Bonds}", font, fontColor, 0, 125);
                            graphics.DrawLine(new Pen(Color.Black, 3), new Point(0, 180), new Point(480, 180));
                            SendBmpMessage(GroupId, bitmap);
                            //SendMessage(GroupId, $"临时测试界面\n积分：{playerData.Points}\n点券：{playerData.Bonds}");
                            break;
                        }
                        case "我的背包":
                        {
                            Font font = new("Microsoft YaHei", 23, FontStyle.Regular);
                            PlayerData playerData = Register(GroupId, MemberId);
                            List<string> BagItemList = new();
                            foreach (var BagItem in playerData.BagItems)
                            {
                                string StrItemType = string.Empty;
                                Item item = Items[BagItem.Key];
                                switch (item.ItemType)
                                {
                                    case 0:
                                        StrItemType = "材料";
                                        break;
                                    case 1:
                                        StrItemType = "神器";
                                        break;
                                    case 2:
                                        StrItemType = "复活";
                                        break;
                                    case 3:
                                        StrItemType = "恢复";
                                        break;
                                }

                                int count = BagItem.Value;
                                if (count != 0)
                                {
                                    BagItemList.Add($"●[{StrItemType}]:{item.Name}⨉{count}");
                                }
                            }

                            if (BagItemList.Count == 0)
                            {
                                x.SendAtMessage("您的背包里面空空如也哦！");
                                break;
                            }

                            int height = BagItemList.Count * 38 + 110;
                            Bitmap ImageData = new(480, height);
                            Graphics graphics = Graphics.FromImage(ImageData);
                            graphics.Fill(Brushes.White, ImageData);
                            graphics.DrawString($"[{MemberId}]您的背包：", font, fontColor, 2, 2);
                            graphics.DrawLine(new Pen(Color.Black, 3), new Point(0, 55), new Point(480, 55));
                            int i = 65;
                            foreach (string itemStr in BagItemList)
                            {
                                graphics.DrawString(itemStr, font, fontColor, 0, i);
                                i += 38;
                            }

                            graphics.DrawLine(new Pen(Color.Black, 3), new Point(0, height - 30),
                                new Point(480, height - 30));
                            SendBmpMessage(GroupId, ImageData);
                            break;
                        }
                        default:
                            if (StrMess.StartsWith("使用"))
                            {
                                string ItemName = StrMess[2..];
                                int count = ItemName.GetCount(ref ItemName);
                                PlayerData playerData = Register(GroupId, MemberId);
                                Item? item;

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

                                if (!playerData.BagItems.ContainsKey(item.Id))
                                {
                                    playerData.BagItems[item.Id] = 0;
                                    PlayersData[GroupId][MemberId] = playerData;
                                }

                                if (playerData.BagItems[item.Id] < count)
                                {
                                    x.SendAtMessage($"你的背包中【{ItemName}】不足{count}个！");
                                    break;
                                }

                                UseItemEvent(GroupId, MemberId, item, count);
                            }
                            else if (StrMess.StartsWith("宠物副本"))
                            {
                                int index = StrMess[4..].GetCount("");
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
                                graphics.DrawLine(new Pen(Color.Black, 3), 0, 60, 480, 60);
                                graphics.DrawLine(new Pen(Color.Black, 3), 0, 498, 235, 498);
                                int n = 55;
                                foreach (var text in ReplicasString.TryGetRange(index, 10))
                                {
                                    graphics.DrawString(text, font, Brushes.Black, 0, n);
                                }

                                x.SendBmpMessage(bitmap);
                                log.Info($"为群({GroupId})成员({MemberId})完成宠物副本绘制");
                            }
                            else if (StrMess.StartsWith("进入副本"))
                            {
                                string ReplicaName = StrMess[4..];
                                Replica? replica = FindReplica(ReplicaName);
                                if (replica == null)
                                {
                                    break;
                                }

                                replica.Challenge(Register(x), 1);

                                x.SendMessageAsync("临时测试界面\n" +
                                                   $"副本名:{replica.Name}\n" +
                                                   $"奖励积分:{replica.RewardingPoint}");
                            }
                            else if (StrMess.StartsWith("宠物攻击"))
                            {
                                string? target = GetAtNumber(OriginalMess);
                                if (target == null)
                                {
                                    break;
                                }

                                PlayerData player = Register(GroupId, MemberId);
                                PlayerData tPlayer = Register(GroupId, target);
                                if (!HavePet(GroupId, MemberId))
                                {
                                    break;
                                }

                                if (!HavePet(tPlayer))
                                {
                                    x.SendAtMessage("对方并没有宠物，无法对目标发起攻击！");
                                    break;
                                }

                                /*int Attack = (int)(player.petData.Attack * (1 - ((double)player.petData.Attack /
                                    (player.petData.Attack + tPlayer.petData.Defense))));*/
                                int Attack = tPlayer.pet.Damage(player.pet);
                                tPlayer.pet.Health -= Attack;
                                await x.SendMessageAsync($"【{player.pet.PetName} VS {tPlayer.pet.PetName}】\n" +
                                                         $"属性:[{player.pet.PetAttribute}] -- [{tPlayer.pet.PetAttribute}]\n" +
                                                         $"你的宠物直接KO对方宠物\n" +
                                                         $"● 经验：+0\n" +
                                                         $"---------------\n" +
                                                         $"对方血量扣除：-{Attack}\n" +
                                                         $"我方血量扣除：-0\n" +
                                                         $"对方剩余血量：{tPlayer.pet.Health}\n" +
                                                         $"我方剩余血量：{player.pet.Health}");
                                PlayersData[GroupId][MemberId] = player;
                                PlayersData[GroupId][target] = tPlayer;
                            }
                            else if (StrMess.StartsWith("给予") && MemberId == MasterId)
                            {
                                PlayerData playerData = Register(GroupId, MemberId);
                                string ItemName = StrMess[2..];
                                Item? item = FindItem(ItemName);
                                if (item == null)
                                {
                                    x.SendAtMessage("该道具并不存在，请检查是否输错！");
                                    break;
                                }

                                if (!playerData.BagItems.ContainsKey(item.Id))
                                {
                                    playerData.BagItems[item.Id] = 0;
                                }


                                PlayersData[GroupId][MemberId].BagItems[item.Id]++;
                                await x.SendMessageAsync($"已给予{ItemName}");
                            }

                            break;
                    }
#pragma warning restore CS8602 // 解引用可能出现空引用。
                });

            /*bot.EventReceived
                .OfType<AtEvent>()
                .Where(receiver => RunGroupId.Contains(receiver.Receiver.GroupId))
                .Subscribe(receiver =>
                {
                    string GroupId = receiver.Receiver.GroupId;
                    SendMessage(GroupId, "嗨~想我了吗？无论何时何地，爱莉希雅都会回应你的期待");
                });*/

            SetConsoleCtrlHandler(cancelHandler, true);

            for (;;)
            {
                Console.Write("> ");
                string? UserWrite = Console.ReadLine();
                switch (UserWrite)
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
                        Console.WriteLine(string.Join("\n", RunGroupId));
                        break;
                    case "/reload":
                        SaveData();
                        ReadData();
                        Console.WriteLine("已重载数据");
                        break;
                    case "":
                        break;
                    default:
                        if (UserWrite == null)
                        {
                            break;
                        }

                        if (UserWrite.StartsWith("/AddGroup "))
                        {
                            string LGroupId = UserWrite[10..];
                            if (!long.TryParse(LGroupId, out _))
                            {
                                Console.WriteLine("请输入正确的群号！");
                                break;
                            }

                            RunGroupId.Add(LGroupId);
                            break;
                        }
                        else if (UserWrite.StartsWith("/DelGroup "))
                        {
                            string LGroupId = UserWrite[10..];
                            if (!long.TryParse(LGroupId, out _) && !RunGroupId.Remove(LGroupId))
                            {
                                Console.WriteLine("请输入正确的群号！");
                            }

                            break;
                        }

                        Console.WriteLine($"未知命令\"{UserWrite}\"，请输入/help查看命令");
                        break;
                }
            }
        }

        public static void CoverWriteLine(string Text = "")
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine(Text);
        }

        public static bool RandomBool()
        {
            return random.Next(0, 2) == 1;
        }

        /*static void EnergyRecovery(object? sender, ElapsedEventArgs e)
        {
            foreach (var GroupId in PlayersData.Keys)
            {
                foreach (var MemberId in PlayersData[GroupId].Keys)
                {
                    PetData? petData = PlayersData[GroupId][MemberId].pet;
                    if (petData != null && petData.Energy < petData.MaxEnergy)
                    {
#pragma warning disable CS8602 // 解引用可能出现空引用。
                        PlayersData[GroupId][MemberId].pet.Energy++;
#pragma warning restore CS8602 // 解引用可能出现空引用。
                    }
                }
            }
        }*/

        static void EnergyRecovery(object? sender, ElapsedEventArgs e)
        {
            foreach (var group in PlayersData)
            {
                string groupId = group.Key;
                foreach (var member in PlayersData[groupId])
                {
                    PlayersData[groupId][member.Key].EnergyAdd();
                }
            }
        }

        static bool CanActivity(PlayerData playerData, string GroupId, string MemberId)
        {
            if (!((GetNowUnixTime() - playerData.ActivityCD > 120) || (playerData.ActivityCD == 0)))
            {
                SendAtMessage(GroupId, MemberId,
                    $"时间还没到，距您下一次活动还差[{120 - GetNowUnixTime() + playerData.ActivityCD}]秒!");
                return false;
            }

            return HavePet(GroupId, MemberId);
        }

        static void WriteConfig()
        {
            File.WriteAllLines("./config.txt", new[]
            {
                "Address=" + Address,
                "QQNumber=" + QQNumber,
                "VerifyKey=" + VerifyKey,
                "RunGroupId=" + string.Join(',', RunGroupId)
            });
        }

        #region 发送消息

        public static void SendAtMessage(string GroupId, string MemberId, string Message)
        {
            SendMessage(GroupId, new MessageChainBuilder().At(MemberId).Plain(" " + Message).Build());
        }

        public static void SendBmpMessage(string GroupId, Image ImageData)
        {
            SendMessage(GroupId, new MessageChainBuilder().ImageFromBase64(ToBase64(ImageData)).Build());
        }

        public static async void SendMessage(string GroupId, MessageChain Message)
        {
            await MessageManager.SendGroupMessageAsync(GroupId, Message);
        }

        #endregion

        static string? GetAtNumber(MessageChain messageChain)
        {
            var test = messageChain.OfType<AtMessage>();
            foreach (var atMessage in test)
            {
                return atMessage.Target;
            }

            return null;
        }

        static string GetQQNumber()
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

        static int GetNowUnixTime()
        {
            return ToUnixTime(DateTime.UtcNow);
        }

        static int ToUnixTime(DateTime dateTime)
        {
            return (int)new DateTimeOffset(dateTime).ToUnixTimeSeconds();
        }

        static void KeysExit()
        {
            Console.Write("按任意键退出…");
            Console.ReadKey(true);
            Environment.Exit(0);
        }

        public static string ToBase64(Image bmp)
        {
            MemoryStream ms = new();
            bmp.Save(ms, ImageFormat.Png);
            byte[] arr = ms.ToArray();
            ms.Position = 0;
            ms.Close();

            string strbaser64 = Convert.ToBase64String(arr);
            return strbaser64;
        }

        static int Pow(int x, int y) => (int)Math.Pow(x, y);
    }
}