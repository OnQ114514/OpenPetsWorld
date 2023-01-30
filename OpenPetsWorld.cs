using Newtonsoft.Json;
using System.Drawing;
using System.Linq;
using System.Reflection;
using static 班级小爱莉.Program;

namespace 班级小爱莉
{
    internal class OpenPetsWorld
    {
        public const string MaxMood = "50";
        public readonly static Font font = new("宋体", 20, FontStyle.Regular);
        public readonly static SolidBrush fontColor = new(Color.Black);
        public readonly static string[] Ranks = new string[] { "普通", "精品", "稀有", "史诗", "传说" };
        public readonly static string[] AbTexts = new string[] { "精力", "血量", "经验" };
        public readonly static string[] AbTexts2 = new string[] { "等级", "昵称", "性别", "阶段", "属性", "级别", "状态", "神器", "天赋", "战力", "智力", "攻击", "防御" };
        public readonly static string[] SignTexts = new string[] { "奖励积分", "累签", "连签" };
        public readonly static string[] Attributes = new string[] { "金", "木", "水", "火", "土" };
        public readonly static string[] UnitingPlace = new string[] { "神魔之井", "霹雳荒原", "石爪山脉", "燃烧平原", "诅咒之地", "洛克莫丹", "天山血池", "银松森林", "闪光平原" };
        public static Dictionary<string, PetData> PetsData = new();
        public static Dictionary<string, long> SentTime = new();
        public static Dictionary<string, PlayerData> PlayersData = new();
        public static Dictionary<int, Item> Items = new();

        public static void UseItemEvent(string GroupId, string MemberId, Item item, int count)
        {
            PetData petData = PetsData[MemberId];
            switch (item.ItemType)
            {
                case 1:
                    return;
                case 2:
                    if (!HavePet(GroupId, MemberId))
                    {                        
                        return;
                    }
                    if (petData.Health == 0)
                    {
                        int ResHealth;
                        switch (item.Mode)
                        {
                            case 0:
                                petData.Health = (int)(petData.MaxHealth < item.Health ? petData.MaxHealth : item.Health);
                                petData.RectOverflow();
                                ResHealth = petData.Health;
                                break;
                            case 1:
                                ResHealth = petData.Health = (int)Math.Round(petData.MaxHealth * item.Health);
                                break;
                            case 2:
                                ResHealth = petData.Health = petData.MaxHealth;
                                break;
                            default:
                                throw new Exception($"恢复模式异常，模式为{item.Mode}，物品Id为{item.Id}");
                        }
                        count = 1;
                        SendAtMessage(GroupId, MemberId, $"成功使用【{item.Name}】×1，将宠物成功复活!\n◇回复血量：{ResHealth}");
                    }
                    else
                    {
                        SendAtMessage(GroupId, MemberId, "你的宠物没有死亡，无需复活！");
                        return;
                    }
                    break;
                case 3:
                    if (!HavePet(GroupId, MemberId))
                    {
                        return;
                    }
                    if (petData.Health == 0)
                    {
                        SendAtMessage(GroupId, MemberId, "您的宠物已死亡，请先进行复活！");
                        return;
                    }
                    if (petData.Health < petData.MaxHealth)
                    {
                        int OriginHealth = petData.Health;
                        int ResHealth = 0;
                        switch (item.Mode)
                        {
                            case 0:                                
                                petData.Health += (int)item.Health * count;
                                petData.RectOverflow();
                                ResHealth = petData.Health - OriginHealth;
                                break;
                            case 1:
                                petData.Health += (int)Math.Round(petData.MaxHealth * item.Health) * count;;
                                ResHealth = petData.Health - OriginHealth;
                                break;
                            case 2:
                                petData.Health = petData.MaxHealth;
                                ResHealth = petData.MaxHealth - OriginHealth;
                                count = 1;
                                break;
                            default:
                                throw new Exception($"恢复模式异常，模式为{item.Mode}，物品Id为{item.Id}");
                        }
                        SendAtMessage(GroupId, MemberId, $"成功使用【{item.Name}】×{count}，将宠物成功复活!\n◇回复血量：{ResHealth}");
                    }
                    else
                    {
                        SendAtMessage(GroupId, MemberId, "你的宠物当前不需要恢复生命！");
                        return;
                    }
                    break;
                case 0:
                default:
                    SendAtMessage(GroupId, MemberId, "该道具不能直接使用，请更换道具！");
                    return;
            }
            PlayersData[MemberId].BagItems[item.Id] -= count;
            PetsData[MemberId] = petData;
        }

        public static PlayerData Register(string MemberId)
        {
            if (!PlayersData.ContainsKey(MemberId))
            {
                PlayersData[MemberId] = new();
            }
            return PlayersData[MemberId];
        }

        public static bool HavePet(string GroupId, string MemberId)
        {
            if (PetsData.ContainsKey(MemberId))
            {
                return true;
            }
            SendAtMessage(GroupId, MemberId, "您当前还没有宠物,赶紧邂逅您的宠物!\n◇指令:砸蛋");
            return false;
        }

        public static Item? FindItem(string ItemName)
        {
            Item? item = null;
            foreach (var LItem in from LItem in Items.Values
                                  where LItem.Name == ItemName
                                  select LItem)
            {
                item = LItem;
                break;
            }
            return item;
        }

        #region 读写数据文件
        public static void ReadData()
        {
            #region 物品数据
            string ItemsDataPath = "./Items.json";
            if (File.Exists(ItemsDataPath))
            {
                string json = File.ReadAllText(ItemsDataPath);
                var LItemsData = JsonConvert.DeserializeObject<Dictionary<int, Item>>(json);
                if (LItemsData != null)
                {
                    Items = LItemsData;
                }
            }
            #endregion
            #region 宠物数据         
            string PetsDataPath = "./PetsData.json";
            var LPetData = TRead<PetData>(PetsDataPath);
            if (LPetData != null)
            {
                PetsData = LPetData;
            }
            #endregion
            #region 玩家数据
            string PlayersDataPath = "./PlayersData.json";
            var LPlayerData = TRead<PlayerData>(PlayersDataPath);
            if (LPlayerData != null)
            {
                PlayersData = LPlayerData;
            }
            #endregion
        }

        static Dictionary<string, T>? TRead<T>(string DataPath)
        {
            if (File.Exists(DataPath))
            {
                try
                {
                    string json = File.ReadAllText(DataPath);
                    var Data = JsonConvert.DeserializeObject<Dictionary<string, T>>(json);
                    return Data;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    ErrorDispose(DataPath);
                }
            }
            return null;
        }

        static void ErrorDispose(string Path)
        {
        ReInput:
            Console.Write("检测到读取宠物世界数据时发生错误，是否删除配置文件？(true/false):");
            if (!bool.TryParse(Console.ReadLine(), out bool DelConfig))
            {
                goto ReInput;
            }

            if (DelConfig)
            {
                File.Delete(Path);
            }
            else
            {
                Environment.Exit(0);
            }
        }

        public static void SaveData()
        {
            #region 宠物数据
            string PetJson = JsonConvert.SerializeObject(PetsData);
            File.WriteAllText("./PetsData.json", PetJson);
            #endregion

            #region 玩家数据
            string PlayerJson = JsonConvert.SerializeObject(PlayersData);
            File.WriteAllText("./PlayersData.json", PlayerJson);
            #endregion
        }
        #endregion        

        public class PlayerData
        {
            /// <summary>
            /// 积分
            /// </summary>
            public int Points = 0;
            /// <summary>
            /// 点券
            /// </summary>
            public int Bonds = 0;
            /// <summary>
            /// 累签
            /// </summary>
            public int SignedDays = 0;
            /// <summary>
            /// 连签
            /// </summary>
            public int ContinuousSignedDays = 0;
            /// <summary>
            /// 上次签到时间
            /// </summary>
            public int LastSignedUnixTime = 0;
            /// <summary>
            /// 背包
            /// </summary>
            public Dictionary<int, int> BagItems = new();
            public int ActivityCD = 0;
        }

        public class PetData
        {
            public int Energy = 100;
            public int Health;
            public int Experience = 0;
            public double MaxEnergy = 100;
            public int MaxHealth;
            public int MaxExperience = 160;
            public int Level = 1;
            public string PetName;
            public string PetGender;
            public string PetStage = "幼年期";
            public string PetAttribute;
            public string PetRank;
            public string PetState = "正常";
            public string ArtifactName = "无";
            public string PettAlent = "无";
            public string Power = "暂不支持";
            public int Intellect = 4;
            public int Attack = 10;
            public int Defense = 10;
            //public Artifact artifact;
            public int Mood = 50;
            private static readonly FieldInfo[] fieldInfos = typeof(PetData).GetFields();

            public PetData()
            {
                MaxHealth = random.Next(100, 301);
                Health = MaxHealth;
                PetName = "test";
                #region 性别随机
                PetGender = RandomBool() ? "雌" : "雄";
                #endregion
                #region 级别随机
                PetRank = Ranks[random.Next(0, 4)];
                #endregion
                #region 属性随机
                PetAttribute = Attributes[random.Next(0, 5)];
                #endregion

            }

            public List<object> ToList()
            {
                List<object> ListData = new();
                ListData = (from fieldInfo in fieldInfos
                            select fieldInfo.GetValue(this)).ToList();
                return ListData;
            }

            public string GetMoodSymbol()
            {
                string Star = string.Empty;
                int StarNumber = (int)Math.Round((double)Mood / 10);
                for (int i = 0; i < StarNumber; i++)
                {
                    Star += "★";
                }
                return Star;
            }

            public void RectOverflow()
            {
                if (Health > MaxHealth)
                {
                    Health = MaxHealth;
                }
            }
        }

        #region 物品类
        public class Item
        {
            public int Id = 0;
            public string Name = "无";
            public int ItemType = 0;
            public int Attack = 0;
            public int Defense = 0;
            public int Energy = 0;
            public int Intellect = 0;
            public double Health = 0;
            public int Level = 0;
            /// <summary>
            /// (回血)0增加，1为增加到上限的百分之几，2回满
            /// (复活)0为回复至某值，1为回复到上限的百分之几，2回满
            /// </summary>
            public int Mode = 0;
        }

        public class Artifact : Item
        {
            public Artifact()
            {
                ItemType = 1;
            }
        }

        public class Resurrection : Item
        {
            public Resurrection()
            {
                ItemType = 2;
            }
        }

        public class Recovery : Item
        {
            public Recovery()
            {
                ItemType = 3;
            }
        }
        #endregion
    }
}
