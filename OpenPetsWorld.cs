using Newtonsoft.Json;
using System.Drawing;
using Mirai.Net.Data.Messages.Receivers;
using static OpenPetsWorld.Program;
using File = System.IO.File;
using OpenPetsWorld.Item;

namespace OpenPetsWorld
{
    public static class OpenPetsWorld
    {
        public static readonly Font font = new("微软雅黑", 20, FontStyle.Regular);
        public static readonly SolidBrush fontColor = new(Color.Black);
        //TODO:将下列变量添加至Misc类
        public static readonly string[] Ranks = { "普通", "精品", "稀有", "史诗", "传说" };
        public static readonly string[] SignTexts = { "奖励积分", "累签", "连签" };
        public static readonly string[] Attributes = { "金", "木", "水", "火", "土" };
        public static readonly string[] UnitingPlace =
            { "神魔之井", "霹雳荒原", "石爪山脉", "燃烧平原", "诅咒之地", "洛克莫丹", "天山血池", "银松森林", "闪光平原" };

        public static readonly Dictionary<string, long> SentTime = new();
        public static Dictionary<string, Dictionary<string, Player>> Players = new();
        public static Dictionary<int, BaseItem> Items = new();
        public static List<Pet> PetPool = new();
        public static List<Replica> Replicas = new();

        /*public static void UseItemEvent(string GroupId, string MemberId, BaseItem item, int count)
        {
            Player playerData = Player.Register(GroupId, MemberId);
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
                    SendAtMessage(GroupId, MemberId, "");
                    return;
            }

            playerData.BagItems[item.Id] -= count;
            playerData.pet = petData;
            Players[GroupId][MemberId] = playerData;
#pragma warning restore CS8602 // 解引用可能出现空引用。
        }*/

        public static bool HavePet(GroupMessageReceiver x, bool Send = true)
        {
            return HavePet(x.GroupId, x.Sender.Id, Send);
        }

        public static bool HavePet(GroupMessageReceiver x, out Pet? petData, bool Send = true)
        {
            return HavePet(x.GroupId, x.Sender.Id, out petData, Send);
        }

        public static bool HavePet(string GroupId, string MemberId, bool Send = true)
        {
            Player playerData = Player.Register(GroupId, MemberId);
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

        public static bool HavePet(string GroupId, string MemberId, out Pet? petData, bool Send = true)
        {
            if (HavePet(GroupId, MemberId, Send))
            {
                petData = Player.Register(GroupId, MemberId).pet;
                return true;
            }

            petData = null;
            return false;
        }

        public static bool HavePet(Player playerData)
        {
            if (playerData.pet != null)
            {
                return true;
            }

            return false;
        }

        public static BaseItem? FindItem(string ItemName)
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
                var LItemsData = ItemReader.Read(ItemsDataPath);
                if (LItemsData != null)
                {
                    Items = LItemsData;
                }
            }

            #endregion

            #region 玩家数据

            string PlayersDataPath = "./data/PlayersData.json";
            var LPlayerData = TRead<Dictionary<string, Dictionary<string, Player>>>(PlayersDataPath);
            if (LPlayerData != null)
            {
                Players = (Dictionary<string, Dictionary<string, Player>>)LPlayerData;
            }

            #endregion

            #region 宠物池

            string petPoolPath = "./datapack/PetPool.json";
            var LPetPool = TRead<List<Pet>>(petPoolPath);
            if (LPetPool != null)
            {
                PetPool = (List<Pet>)LPetPool;
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

            string PlayerJson = JsonConvert.SerializeObject(Players);
            string path = "./data/PlayersData.json";
            if (!Directory.Exists("./data"))
            {
                Directory.CreateDirectory("./data");
            }
            File.WriteAllText(path, PlayerJson);

            #endregion
        }

        #endregion

        public class Player
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
            public Pet? pet;

            public int LastActivityUnixTime = 0;

            public static Player Register(GroupMessageReceiver x)
            {
                return Register(x.GroupId, x.Sender.Id);
            }
            
            public static Player Register(string GroupId, string MemberId)
            {
                if (!Players.ContainsKey(GroupId))
                {
                    Players[GroupId] = new();
                }

                if (!Players[GroupId].ContainsKey(MemberId))
                {
                    Players[GroupId][MemberId] = new();
                }

                return Players[GroupId][MemberId];
            }
            
            public bool CanActivity(GroupMessageReceiver receiver)
            {
                return CanActivity(receiver.GroupId, receiver.Sender.Id);
            }
            
            public bool CanActivity(string GroupId, string MemberId)
            {
                if (GetNowUnixTime() - LastActivityUnixTime > 120 || (LastActivityUnixTime == 0))
                {
                    return HavePet(GroupId, MemberId);
                }

                SendAtMessage(GroupId, MemberId,
                    $"时间还没到，距您下一次活动还差[{120 - GetNowUnixTime() + LastActivityUnixTime}]秒!");
                return false;

            }
            
            public void EnergyAdd()
            {
                if (pet != null && pet.Energy < pet.MaxEnergy)
                {
                    pet.Energy++;
                }
            }
        }



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
            public string? iconName = null;

            public bool Challenge(Player player, int count)
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