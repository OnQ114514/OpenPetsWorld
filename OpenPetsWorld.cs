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

        public static string[] Ranks = { "普通", "精品", "稀有", "史诗", "传说" };
        public static readonly string[] SignTexts = { "奖励积分", "累签", "连签" };
        public static string[] Attributes = { "金", "木", "水", "火", "土" };
        public static string[] UnitingPlace =
            { "神魔之井", "霹雳荒原", "石爪山脉", "燃烧平原", "诅咒之地", "洛克莫丹", "天山血池", "银松森林", "闪光平原" };

        public static readonly Dictionary<string, long> SentTime = new();
        public static Dictionary<string, Dictionary<string, Player>> Players = new();
        public static Dictionary<int, BaseItem> Items = new();
        public static List<Pet> PetPool = new();
        public static List<Replica> Replicas = new();
        
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
            #region 杂项

            string MiscPath = "./datapack/misc.json";
            Misc misc = (Misc)TRead<Misc>(MiscPath);
            if (misc != null)
            {
                Ranks = misc.Ranks;
                UnitingPlace = misc.UnitingPlace;
                Attributes = misc.Attributes;
            }

            #endregion

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