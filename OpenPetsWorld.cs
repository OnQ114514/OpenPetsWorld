using Newtonsoft.Json;
using System.Drawing;
using Mirai.Net.Data.Messages.Receivers;
using static OpenPetsWorld.Program;
using File = System.IO.File;

namespace OpenPetsWorld
{
    public static class OpenPetsWorld
    {
        public static readonly Font font = new("微软雅黑", 20, FontStyle.Regular);
        public static readonly SolidBrush fontColor = new(Color.Black);
        static readonly string[] Ranks = { "普通", "精品", "稀有", "史诗", "传说" };
        public static readonly string[] SignTexts = { "奖励积分", "累签", "连签" };
        static readonly string[] Attributes = { "金", "木", "水", "火", "土" };

        public static readonly string[] UnitingPlace =
            { "神魔之井", "霹雳荒原", "石爪山脉", "燃烧平原", "诅咒之地", "洛克莫丹", "天山血池", "银松森林", "闪光平原" };

        public static readonly Dictionary<string, long> SentTime = new();
        public static Dictionary<string, Dictionary<string, PlayerData>> PlayersData = new();
        public static Dictionary<int, Item> Items = new();
        public static List<PetData> petPool = new();
        public static List<Replica> Replicas = new();

        public static void UseItemEvent(string GroupId, string MemberId, Item item, int count)
        {
            PlayerData playerData = Register(GroupId, MemberId);
            PetData? petData;
            switch (item.ItemType)
            {
                case 1:
                    return;
                case 2:
                {
#pragma warning disable CS8602 // 解引用可能出现空引用。
                    if (!HavePet(GroupId, MemberId, out petData) || petData == null)
                    {
                        return;
                    }

                    if (petData.Health == 0)
                    {
                        int ResHealth;
                        Resurrection resurrection = (Resurrection)item;
                        switch (resurrection.Mode)
                        {
                            case 0:
                                petData.Health =
                                    (int)(petData.MaxHealth < resurrection.Health
                                        ? petData.MaxHealth
                                        : resurrection.Health);
                                petData.RectOverflow();
                                ResHealth = petData.Health;
                                break;
                            case 1:
                                ResHealth = petData.Health = (int)Math.Round(petData.MaxHealth * resurrection.Health);
                                break;
                            case 2:
                                ResHealth = petData.Health = petData.MaxHealth;
                                break;
                            default:
                                throw new Exception($"恢复模式异常，模式为{resurrection.Mode}，物品Id为{resurrection.Id}");
                        }

                        count = 1;
                        SendAtMessage(GroupId, MemberId, $"成功使用【{item.Name}】×1，将宠物成功复活!\n◇回复血量：{ResHealth}");
                    }
                    else
                    {
                        SendAtMessage(GroupId, MemberId, "你的宠物没有死亡，无需复活！");
                        return;
                    }
                }
                    break;
                case 3:
                {
                    Recovery recovery = (Recovery)item;
                    if (!HavePet(GroupId, MemberId, out petData))
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
                        int ResHealth;
                        switch (recovery.Mode)
                        {
                            case 0:
                                petData.Health += (int)recovery.Health * count;
                                petData.RectOverflow();
                                ResHealth = petData.Health - OriginHealth;
                                break;
                            case 1:
                                petData.Health += (int)Math.Round(petData.MaxHealth * recovery.Health) * count;
                                ResHealth = petData.Health - OriginHealth;
                                break;
                            case 2:
                                petData.Health = petData.MaxHealth;
                                ResHealth = petData.MaxHealth - OriginHealth;
                                count = 1;
                                break;
                            default:
                                log.Error($"恢复模式异常，模式为{recovery.Mode}，物品Id为{recovery.Id}");
                                return;
                        }

                        SendAtMessage(GroupId, MemberId, $"成功使用【{recovery.Name}】×{count}，将宠物成功复活!\n◇回复血量：{ResHealth}");
                    }
                    else
                    {
                        SendAtMessage(GroupId, MemberId, "你的宠物当前不需要恢复生命！");
                        return;
                    }

                    break;
                }
                case 4:
                    if (!HavePet(GroupId, MemberId, out petData))
                    {
                        return;
                    }

                    if (petData.Level < item.Level)
                    {
                        SendAtMessage(GroupId, MemberId, $"该道具最低使用等级[{item.Level}]！");
                        return;
                    }

                    break;
                default:
                    SendAtMessage(GroupId, MemberId, "该道具不能直接使用，请更换道具！");
                    return;
            }

            playerData.BagItems[item.Id] -= count;
            playerData.pet = petData;
            PlayersData[GroupId][MemberId] = playerData;
#pragma warning restore CS8602 // 解引用可能出现空引用。
        }

        public static PlayerData Register(string GroupId, string MemberId)
        {
            if (!PlayersData.ContainsKey(GroupId))
            {
                PlayersData[GroupId] = new();
            }

            if (!PlayersData[GroupId].ContainsKey(MemberId))
            {
                PlayersData[GroupId][MemberId] = new();
            }

            return PlayersData[GroupId][MemberId];
        }

        public static PlayerData Register(GroupMessageReceiver x)
        {
            return Register(x.GroupId, x.Sender.Id);
        }

        public static bool HavePet(GroupMessageReceiver x, bool Send = true)
        {
            return HavePet(x.GroupId, x.Sender.Id, Send);
        }

        public static bool HavePet(string GroupId, string MemberId, bool Send = true)
        {
            PlayerData playerData = Register(GroupId, MemberId);
            if (playerData.pet != null)
            {
                return true;
            }

            if (Send)
            {
                SendAtMessage(GroupId, MemberId, "您当前还没有宠物,赶紧邂逅您的宠物!\n◇指令:砸蛋");
            }

            return false;
        }

        public static bool HavePet(string GroupId, string MemberId, out PetData? petData, bool Send = true)
        {
            if (HavePet(GroupId, MemberId, Send))
            {
                petData = Register(GroupId, MemberId).pet;
                return true;
            }

            petData = null;
            return false;
        }

        public static bool HavePet(PlayerData playerData)
        {
            if (playerData.pet != null)
            {
                return true;
            }

            return false;
        }

        public static Item? FindItem(string ItemName)
        {
            var items = (from litems in Items.Values
                where litems.Name == ItemName
                select litems).ToList();
            if (items.Count != 0)
            {
                return items[0];
            }

            return null;
        }

        public static Replica? FindReplica(string ReplicaName)
        {
            Replica? replica = null;
            foreach (var LReplica in from LReplica in Replicas
                     where LReplica.Name == ReplicaName
                     select LReplica)
            {
                replica = LReplica;
                break;
            }

            return replica;
        }

        #region 读写数据文件

        public static void ReadData()
        {
            #region 物品数据

            string ItemsDataPath = "./datapack/Items.json";
            if (File.Exists(ItemsDataPath))
            {
                var LItemsData = TRead<Dictionary<int, Item>>(ItemsDataPath);
                if (LItemsData != null)
                {
                    Items = (Dictionary<int, Item>)LItemsData;
                }
            }

            #endregion

            #region 玩家数据

            string PlayersDataPath = "./data/PlayersData.json";
            var LPlayerData = TRead<Dictionary<string, Dictionary<string, PlayerData>>>(PlayersDataPath);
            if (LPlayerData != null)
            {
                PlayersData = (Dictionary<string, Dictionary<string, PlayerData>>)LPlayerData;
            }

            #endregion

            #region 宠物池

            string petPoolPath = "./datapack/PetPool.json";
            var LPetPool = TRead<List<PetData>>(petPoolPath);
            if (LPetPool != null)
            {
                petPool = (List<PetData>)LPetPool;
            }

            #endregion

            #region 副本数据

            string ReplicaPath = "./datapack/replicas.json";
            if (File.Exists(ReplicaPath))
            {
                var LReplicaData = TRead<List<Replica>>(ReplicaPath);
                if (LReplicaData != null)
                {
                    Replicas = (List<Replica>)LReplicaData;
                }
            }

            #endregion
        }

        static object? TRead<T>(string DataPath)
        {
            if (File.Exists(DataPath))
            {
                try
                {
                    string json = File.ReadAllText(DataPath);
                    var Data = JsonConvert.DeserializeObject<T>(json);
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
            Console.Write("检测到读取数据文件时发生错误，是否删除数据文件？(true/false):");
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
            #region 玩家数据

            string PlayerJson = JsonConvert.SerializeObject(PlayersData);
            string path = "./data/PlayersData.json";
            if (!Directory.Exists("./data"))
            {
                Directory.CreateDirectory("./data");
            }
            File.WriteAllText(path, PlayerJson);

            #endregion
        }

        #endregion

        public class PlayerData
        {
            /// <summary>
            /// 积分
            /// </summary>
            public int Points;

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

            /// <summary>
            /// 宠物
            /// </summary>
            public PetData? pet;

            public int LastActivityUnixTime = 0;

            public void EnergyAdd()
            {
                if (pet != null && pet.Energy < pet.MaxEnergy)
                {
                    pet.Energy++;
                }
            }
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
            public string Name;
            public string Gender;
            public string Stage = "幼年期";
            public string Attribute;
            public string Rank;
            public string State = "正常";
            public string iconName;
            public string PettAlent = "无";
            public int Intellect = 4;
            public int Attack = 10;
            public int Defense = 10;
            public Artifact? artifact = null;
            public int Mood = 50;

            public PetData()
            {
                //示例宠物
                iconName = "kiana.jpg";
                MaxHealth = random.Next(100, 301);
                Health = MaxHealth;
                Name = "test";

                #region 性别随机

                Gender = RandomBool() ? "雌" : "雄";

                #endregion

                #region 级别随机

                Rank = Ranks[random.Next(0, 4)];

                #endregion

                #region 属性随机
                
                Attribute = Attributes[random.Next(0, 5)];

                #endregion
            }

            public int GetPower()
            {
                return (Attack + Defense + MaxHealth) / 10 + Intellect * 20;
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

                if (Health < 0)
                {
                    Health = 0;
                }
            }

            public static PetData Extract()
            {
                int index = random.Next(0, petPool.Count);
                return petPool[index];
            }

            public int Damage(PetData myPet)
            {
                return (myPet.Attack + myPet.Intellect * 20) *
                       (1 - (Defense * Intellect * 20) / (Attack + Defense + Health / 10 + Intellect * 20));
            }
        }

        #region 物品类

        /// <summary>
        /// 物品基类（材料）
        /// </summary>
        public class Item
        {
            public int Id = 0;

            /// <summary>
            /// 名称
            /// </summary>
            public string Name = "无";

            /// <summary>
            /// 类型
            /// </summary>
            public int ItemType;

            /// <summary>
            /// 描述
            /// </summary>
            public string? infoText;

            /// <summary>
            /// 描述附加图片
            /// </summary>
            public string? infoImageName;

            //TODO:实现最低使用等级判断
            /// <summary>
            /// 最低使用等级
            /// </summary>
            public int Level = 0;
        }

        /// <summary>
        /// 神器
        /// </summary>
        public class Artifact : Item
        {
            public int Attack = 0;
            public int Defense = 0;
            public int Energy = 0;
            public int Intellect = 0;
            public int Health = 0;

            public Artifact()
            {
                ItemType = 1;
            }
        }

        /// <summary>
        /// 复活
        /// </summary>
        public class Resurrection : Item
        {
            /// <summary>
            /// 0为回复至某值，1为回复到上限的百分之几，2回满
            /// </summary>
            public int Mode = 0;

            public double Health = 0;

            public Resurrection()
            {
                ItemType = 2;
            }
        }

        /// <summary>
        /// 恢复
        /// </summary>
        public class Recovery : Item
        {
            public double Health = 0;

            /// <summary>
            /// 0增加，1为增加到上限的百分之几，2回满
            /// </summary>
            public int Mode = 0;

            public Recovery()
            {
                ItemType = 3;
            }
        }

        /// <summary>
        /// 增益
        /// </summary>
        public class Gain : Item
        {
            public Gain()
            {
                ItemType = 4;
            }
        }

        #endregion

        public class Replica
        {
            public int Level = 0;
            public string Name;
            public Dictionary<int, int> RewardingItems = new();
            public int RewardingPoint = 0;
            public int ExpAdd;
            public string enemyName;
            public int Attack;
            public int Energy = 10;

            public Replica()
            {
            }

            public bool Challenge(PlayerData player, int count)
            {
                if (player.pet == null || player.pet.Energy < count * Energy)
                {
                    return false;
                }

                player.pet.Health -= Attack * count;
                player.pet.Energy -= Energy * count;
                player.pet.Experience += ExpAdd * count;
                player.Points += RewardingPoint * count;
                foreach (var item in RewardingItems)
                {
                    if (!player.BagItems.ContainsKey(item.Key))
                    {
                        player.BagItems[item.Key] = item.Value;
                    }
                    else
                    {
                        player.BagItems[item.Key] += item.Value;
                    }
                }

                return true;
            }
        }
    }
}