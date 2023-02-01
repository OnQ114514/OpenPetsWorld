using Mirai.Net.Data.Events.Concretes.Message;
using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Sessions;
using Mirai.Net.Sessions.Http.Managers;
using Mirai.Net.Utils.Scaffolds;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Timers;
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
        private static ControlCtrlDelegate cancelHandler = new(HandlerRoutine);

        public static bool HandlerRoutine(int CtrlType)
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
        static List<string> RunGroupId = new() { };
        const string MasterId = "58554566";
        static readonly HttpClient httpClient = new();
        public static Random random = new();

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
                    return;
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
                Console.WriteLine("正在连接Mirai");
                await bot.LaunchAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                KeysExit();
                return;
            }
            #endregion            

            Console.WriteLine("OpenPetsWorld已连接！");

            #region WriteConfig
            if (!File.Exists("./config.txt"))
            {
                WriteConfig();
            }
            #endregion

            ReadData();

            Timer timer = new(60000)
            {
                Enabled = true,
                AutoReset = true
            };
            timer.Elapsed += Elapsed;

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
                        case "我的宠物":
                            if (HavePet(GroupId, MemberId))
                            {
                                Image imagedata = Image.FromFile("./wallpaper.jpg");
                                Graphics sourcegra = Graphics.FromImage(imagedata);//存入画布
                                sourcegra.DrawImage(Image.FromFile("./kiana.jpg"), 5, 5, 380, 380);
                                PetData? petData = PlayersData[GroupId][MemberId].petData;
                                List<object> LPetsData = petData.ToList();

                                sourcegra.DrawString($"◆心情:{petData.GetMoodSymbol()}", font, fontColor, 15, 390);
                                int n = 415;
                                for (int i = 0; i < AbTexts.Length; i++)
                                {
                                    string AbText = AbTexts[i];
                                    sourcegra.DrawString($"◆{AbText}:{LPetsData[i]}/{LPetsData[i + 3]}", font, fontColor, 15, n);
                                    n += 25;
                                }

                                int n2 = 20;
                                for (int i = 0; i < AbTexts2.Length; i++)
                                {
                                    string AbText = AbTexts2[i];
                                    sourcegra.DrawString($"◆{AbText}:{LPetsData[i + 6]}", font, fontColor, 395, n2);
                                    n2 += 35;
                                }

                                SendBmpMessage(GroupId, imagedata);
                            }
                            break;
                        case "砸蛋":
                            {
                                PlayerData playerData = Register(GroupId, MemberId);
                                if (playerData.petData == null)
                                {
                                    if (playerData.Points < 500)
                                    {
                                        SendAtMessage(GroupId, MemberId, "您的积分不足,无法进行砸蛋!\n【所需[500]积分】\n请发送【签到】获得积分");
                                        break;
                                    }
                                    playerData.Points -= 500;
                                    PetData petData = new();
                                    playerData.petData = petData;
                                    PlayersData[GroupId][MemberId] = playerData;
                                    SendAtMessage(GroupId, MemberId, $"恭喜您砸到了一颗{petData.PetAttribute}属性的宠物蛋");
                                }
                                else
                                {
                                    SendAtMessage(GroupId, MemberId, "您已经有宠物了,贪多嚼不烂哦!\n◇指令:宠物放生");
                                }
                            }
                            break;
                        case "修炼":
                            {
                                PlayerData playerData = Register(GroupId, MemberId);
                                if (CanActivity(playerData, GroupId, MemberId))
                                {
                                    playerData.ActivityCD = GetNowUnixTime();
                                    PetData? petData = PlayersData[GroupId][MemberId].petData;
                                    petData.Energy -= 10;
                                    int AddExp = random.Next(250, 550);
                                    petData.Experience += AddExp;
                                    PlayersData[GroupId][MemberId].petData = petData;
                                    PlayersData[GroupId][MemberId] = playerData;
                                    SendAtMessage(GroupId, MemberId, $"您的【{petData.PetName}】正在{UnitingPlace[random.Next(0, UnitingPlace.Length)]}刻苦的修炼！\r\n------------------\r\n·修炼时间：+120秒\r\n·耗费精力：-10点\r\n·增加经验：+{AddExp}\n------------------");
                                }
                            }
                            break;
                        case "学习":
                            {
                                PlayerData playerData = Register(GroupId, MemberId);
                                if (CanActivity(playerData, GroupId, MemberId))
                                {
                                    PetData? petData = PlayersData[GroupId][MemberId].petData;
                                    petData.Energy -= 10;
                                    petData.Intellect += random.Next(2, 6);
                                    PlayersData[GroupId][MemberId].petData = petData;
                                    PlayersData[GroupId][MemberId].ActivityCD = GetNowUnixTime();
                                    SendAtMessage(GroupId, MemberId, $"您的【{petData.PetName}】出门上学啦！\n------------------\n●学习耗时：+120秒\n●减少精力：-10点\n●获得智力：+2\n------------------");
                                }
                            }
                            break;
                        case "洗髓":
                            {
                                PlayerData playerData = Register(GroupId, MemberId);
                                if (CanActivity(playerData, GroupId, MemberId))
                                {
                                    PetData? petData = PlayersData[GroupId][MemberId].petData;
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
                                    PlayersData[GroupId][MemberId].petData = petData;
                                    PlayersData[GroupId][MemberId].ActivityCD = GetNowUnixTime();
                                    SendAtMessage(GroupId, MemberId, $"您的【{petData.PetName}】正在洗髓伐毛！\n------------------\n●洗髓耗时：+120秒\n●减少精力：-10点\n●减少智力：-1\n●增加{AddAttText} ：+{AddAttNumber}\n------------------");
                                }
                            }
                            break;
                        case "宠物升级":
                            if (HavePet(GroupId, MemberId))
                            {
                                PetData? LPetData = PlayersData[GroupId][MemberId].petData;
                                if (LPetData.Experience >= LPetData.MaxExperience)
                                {
                                    int OriginalMaxExp = LPetData.MaxExperience;
                                    LPetData.Experience -= OriginalMaxExp;
                                    int n = LPetData.Level + 1;
                                    int MaxExpAdd = 5 * Pow(n, 3) + 15 * Pow(n, 2) + 40 * n + 100;
                                    LPetData.MaxExperience = MaxExpAdd;
                                    int MaxHealthAdd = 2 * Pow(n, 2) + 4 * n + 10;
                                    LPetData.MaxHealth += MaxHealthAdd;
                                    int AttAndDefAdd = 3 * n + 1;
                                    LPetData.Attack += AttAndDefAdd;
                                    LPetData.Defense += AttAndDefAdd;
                                    LPetData.Level++;
                                    PlayersData[GroupId][MemberId].petData = LPetData;
                                    SendAtMessage(GroupId, MemberId, $"您的[{LPetData.PetName}]成功升级啦!\n------------------\n● 等级提升：+1\n● 经验减少：-{OriginalMaxExp}\n● 生命提升：+{MaxHealthAdd}\n● 攻击提升：+{AttAndDefAdd}\n● 防御提升：+{AttAndDefAdd}\n● 战力提升：+{"null"}\n------------------");
                                }
                            }
                            break;
                        case "宠物放生":
                            if (HavePet(GroupId, MemberId))
                            {
                                PetData? LPetsData = PlayersData[GroupId][MemberId].petData;
                                SendAtMessage(GroupId, MemberId, $"危险操作\n（LV·{LPetsData.Level}-{LPetsData.PetRank}-{LPetsData.PetName}）\n将被放生，请在1分钟内回复：\n【确定放生】");
                                SentTime[MemberId] = GetNowUnixTime();
                            }
                            break;
                        case "确定放生":
                            if (SentTime.ContainsKey(MemberId))
                            {
                                if (GetNowUnixTime() - SentTime[MemberId] <= 60)
                                {
                                    PlayersData[GroupId][MemberId].petData = null;
                                    SentTime.Remove(MemberId);
                                    SendAtMessage(GroupId, MemberId, "成功放生宠物,您的宠物屁颠屁颠的走了!");
                                }
                            }
                            break;
                        case "签到":
                            {
                                PlayerData playerData = Register(GroupId, MemberId);
                                int NowUnixTime = GetNowUnixTime();
                                if (NowUnixTime - playerData.LastSignedUnixTime <= 86400)
                                {
                                    SendAtMessage(GroupId, MemberId, "今天已签到过了,明天再来吧!");
                                    break;
                                }
                                if (NowUnixTime - playerData.LastSignedUnixTime >= 172800 && playerData.LastSignedUnixTime != 0)
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
                                Stream stream = await httpClient.GetStreamAsync($"http://q2.qlogo.cn/headimg_dl?dst_uin={MemberId}&spec=100");
                                Bitmap ImageData = new(230, 90);
                                Graphics sourcegra = Graphics.FromImage(ImageData);//存入画布
                                sourcegra.FillRectangle(Brushes.White, new Rectangle(0, 0, 230, 90));
                                sourcegra.DrawImage(Image.FromStream(stream), 0, 0, 90, 90);
                                sourcegra.DrawString(x.Sender.Name, new Font("黑体", 15, FontStyle.Bold), fontColor, 95, 5);
                                Font sSignFont = new("黑体", 13, FontStyle.Regular);
                                string[] SignTexts2 = { 5500.ToString(), playerData.SignedDays.ToString(), $"{playerData.ContinuousSignedDays}/30" };
                                int n = 30;
                                for (int i = 0; i < 3; i++)
                                {
                                    string SignText = SignTexts[i];
                                    string SignText2 = SignTexts2[i];
                                    sourcegra.DrawString($"{SignText}：{SignText2}", sSignFont, fontColor, 95, n);
                                    n += 18;
                                }
                                #endregion
                                SendBmpMessage(GroupId, ImageData);
                            }
                            break;
                        case "我的资产":
                            {
                                PlayerData playerData = Register(GroupId, MemberId);
                                Bitmap bitmap = new(480, 235);
                                Graphics graphics = Graphics.FromImage(bitmap);
                                graphics.FillRectangle(Brushes.White, new Rectangle(0, 0, 480, 235));
                                Font font = new("Microsoft YaHei", 23, FontStyle.Regular);
                                graphics.DrawString($"[{MemberId}]您的财富信息如下：", font, fontColor, 2, 2);
                                graphics.DrawLine(new Pen(Color.Black, 3), new Point(0, 55), new Point(480, 55));
                                graphics.DrawString($"●积分：{playerData.Points}", font, fontColor, 0, 65);
                                graphics.DrawString($"●点券：{playerData.Bonds}", font, fontColor, 0, 125);
                                graphics.DrawLine(new Pen(Color.Black, 3), new Point(0, 180), new Point(480, 180));
                                SendBmpMessage(GroupId, bitmap);
                                //SendMessage(GroupId, $"临时测试界面\n积分：{playerData.Points}\n点券：{playerData.Bonds}");
                            }
                            break;
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
                                        default:
                                            break;
                                    }
                                    int count = BagItem.Value;
                                    if (count != 0)
                                    {
                                        BagItemList.Add($"●[{StrItemType}]:{item.Name}⨉{count}");
                                    }
                                }
                                int height = BagItemList.Count * 38 + 110;
                                Bitmap ImageData = new(480, height);
                                Graphics graphics = Graphics.FromImage(ImageData);
                                graphics.FillRectangle(Brushes.White, new Rectangle(0, 0, 480, height));
                                graphics.DrawString($"[{MemberId}]您的背包：", font, fontColor, 2, 2);
                                graphics.DrawLine(new Pen(Color.Black, 3), new Point(0, 55), new Point(480, 55));
                                int i = 65;
                                foreach (string itemStr in BagItemList)
                                {
                                    graphics.DrawString(itemStr, font, fontColor, 0, i);
                                    i += 38;
                                }
                                graphics.DrawLine(new Pen(Color.Black, 3), new Point(0, height - 30), new Point(480, height - 30));
                                SendBmpMessage(GroupId, ImageData);
                            }
                            break;
                        default:
                            if (StrMess.StartsWith("使用"))
                            {
                                string ItemName = StrMess[2..];
                                int count = ItemName.IndexOf("*") + 1;
                                int UseCount = 0;
                                PlayerData playerData = Register(GroupId, MemberId);
                                Item? item = null;

                                if (count == 0)
                                {
                                    UseCount = 1;
                                    item = FindItem(ItemName);
                                }
                                else
                                {
                                    if (!int.TryParse(ItemName[count..], out UseCount))
                                    {
                                        SendAtMessage(GroupId, MemberId, "格式错误！");
                                        break;
                                    }
                                    if (UseCount > 99999)
                                    {
                                        SendAtMessage(GroupId, MemberId, "数量超出范围！");
                                        break;
                                    }
                                    item = FindItem(ItemName[0..(count - 1)]);
                                }
                                CoverWriteLine(UseCount.ToString());

                                if (item == null)
                                {
                                    SendAtMessage(GroupId, MemberId, "该道具并不存在，请检查是否输错！");
                                    break;
                                }
                                if (!playerData.BagItems.ContainsKey(item.Id))
                                {
                                    playerData.BagItems[item.Id] = 0;
                                    PlayersData[GroupId][MemberId] = playerData;
                                }
                                if (playerData.BagItems[item.Id] < UseCount)
                                {
                                    SendAtMessage(GroupId, MemberId, $"你的背包中【{ItemName}】不足{UseCount}个！");
                                    break;
                                }
                                UseItemEvent(GroupId, MemberId, item, UseCount);
                                break;
                            }
                            else if (StrMess.StartsWith("给予") && MemberId == MasterId)
                            {
                                PlayerData playerData = Register(GroupId, MemberId);
                                string ItemName = StrMess[2..];
                                Item? item = FindItem(ItemName);
                                if (item == null)
                                {
                                    SendAtMessage(GroupId, MemberId, "该道具并不存在，请检查是否输错！");
                                    break;
                                }
                                if (!playerData.BagItems.ContainsKey(item.Id))
                                {
                                    playerData.BagItems[item.Id] = 0;
                                }
                                PlayersData[GroupId][MemberId].BagItems[item.Id]++;
                                SendMessage(GroupId, $"已给予{ItemName}");
                            }
                            break;
                    }
#pragma warning restore CS8602 // 解引用可能出现空引用。
                });

            bot.EventReceived
                .OfType<AtEvent>()
                .Where(receiver => RunGroupId.Contains(receiver.Receiver.GroupId))
                .Subscribe(receiver =>
                {
                    string GroupId = receiver.Receiver.GroupId;
                    SendMessage(GroupId, "嗨~想我了吗？无论何时何地，爱莉希雅都会回应你的期待");
                });

            SetConsoleCtrlHandler(cancelHandler, true);

            for (; ; )
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
                        return;
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

        static void CoverWriteLine(string Text = "")
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine(Text);
            Console.Write("> ");
        }

        public static bool RandomBool()
        {
            return random.Next(0, 2) == 1;
        }

        static void Elapsed(object? sender, ElapsedEventArgs e)
        {
            foreach (var GroupId in PlayersData.Keys)
            {
                foreach (var MemberId in PlayersData[GroupId].Keys)
                {
                    PetData? petData = PlayersData[GroupId][MemberId].petData;
                    if (petData != null)
                    {
                        if (petData.Energy < petData.MaxEnergy)
                        {
                            petData.Energy++;
                        }
                        PlayersData[GroupId][MemberId].petData = petData;
                    }
                }
            }
        }

        static bool CanActivity(PlayerData playerData, string GroupId, string MemberId)
        {
            if (!((GetNowUnixTime() - playerData.ActivityCD > 120) || (playerData.ActivityCD == 0)))
            {
                SendAtMessage(GroupId, MemberId, $"时间还没到，距您下一次活动还差[{120 - GetNowUnixTime() + playerData.ActivityCD}]秒!");
                return false;
            }
            return HavePet(GroupId, MemberId);
        }

        static void WriteConfig()
        {
            File.WriteAllLines("./config.txt", new string[]
            {
                "Address=" + Address,
                "QQNumber=" + QQNumber,
                "VerifyKey=" + VerifyKey,
                "RunGroupId=" + string.Join( ',', RunGroupId)
            });
        }

        #region 发送消息
        public static void SendAtMessage(string GroupId, string MemberId, string Message)
        {
            SendMessage(GroupId, new MessageChainBuilder().
                At(MemberId).
                Plain(" " + Message).
                Build());
        }

        public static void SendBmpMessage(string GroupId, Image ImageData)
        {
            SendMessage(GroupId, new MessageChainBuilder().
                ImageFromBase64(ToBase64(ImageData)).
                Build());
        }

        public static async void SendMessage(string GroupId, MessageChain Message)
        {
            await MessageManager.SendGroupMessageAsync(GroupId, Message);
        }

        public static async void SendMessage(string GroupId, string Message)
        {
            await MessageManager.SendGroupMessageAsync(GroupId, Message);
        }
        #endregion

        static string GetQQNumber()
        {
            Console.Write("QQ号：");
            string? QQNumber = Console.ReadLine();
            if (!long.TryParse(QQNumber, out _))
            {
                Console.WriteLine("请输入正确的QQ号！");
                return GetQQNumber();
            }
            return QQNumber;
        }

        static int GetNowUnixTime()
        {
            return (int)new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        }

        static void KeysExit()
        {
            Console.Write("按任意键退出…");
            Console.ReadKey(true);
        }

        public static string ToBase64(Image bmp)
        {
            MemoryStream ms = new();
            bmp.Save(ms, ImageFormat.Png);
            //byte[] arr = new byte[ms.Length];
            byte[] arr = ms.ToArray();
            ms.Position = 0;
            //ms.Read(arr, 0, (int)ms.Length);                
            ms.Close();

            string strbaser64 = Convert.ToBase64String(arr);
            return strbaser64;
        }

        static int Pow(int x, int y)
        {
            return (int)Math.Pow(x, y);
        }
    }
}