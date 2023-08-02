using Newtonsoft.Json;
using Mirai.Net.Data.Messages.Receivers;
using static OpenPetsWorld.Program;
using File = System.IO.File;
using OpenPetsWorld.Item;

namespace OpenPetsWorld
{
    public static class OpenPetsWorld
    {
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
        
        public static bool HavePet(GroupMessageReceiver x, bool send = true)
        {
            return HavePet(x.GroupId, x.Sender.Id, send);
        }

        public static bool HavePet(GroupMessageReceiver x, out Pet? petData, bool send = true)
        {
            return HavePet(x.GroupId, x.Sender.Id, out petData, send);
        }

        public static bool HavePet(string groupId, string memberId, bool send = true)
        {
            Player playerData = Player.Register(groupId, memberId);
            if (playerData.Pet != null)
            {
                return true;
            }

            if (send)
            {
                SendAtMessage(groupId, memberId, "您当前还没有宠物,赶紧邂逅您的宠物!\n◇指令:砸蛋");
            }

            return false;
        }

        public static bool HavePet(string groupId, string memberId, out Pet? petData, bool send = true)
        {
            if (HavePet(groupId, memberId, send))
            {
                petData = Player.Register(groupId, memberId).Pet;
                return true;
            }

            petData = null;
            return false;
        }

        public static bool HavePet(Player playerData)
        {
            if (playerData.Pet != null)
            {
                return true;
            }

            return false;
        }

        public static BaseItem? FindItem(string itemName)
        {
            var items = (from litems in Items.Values
                where litems.Name == itemName
                select litems).ToList();
            if (items.Count != 0)
            {
                return items[0];
            }

            return null;
        }

        public static Replica? FindReplica(string replicaName)
        {
            Replica? replica = null;
            foreach (var lReplica in from lReplica in Replicas
                     where lReplica.Name == replicaName
                     select lReplica)
            {
                replica = lReplica;
                break;
            }

            return replica;
        }

        #region 读写数据文件

        public static void ReadData()
        {
            #region 杂项

            string MiscPath = "./datapack/misc.json";
            Misc misc = (Misc)TryRead<Misc>(MiscPath);
            if (misc != null)
            {
                Ranks = misc.Ranks;
                UnitingPlace = misc.UnitingPlace;
                Attributes = misc.Attributes;
            }

            #endregion

            #region 物品数据

            string itemsDataPath = "./datapack/Items.json";
            if (File.Exists(itemsDataPath))
            {
                var LItemsData = ItemReader.Read(itemsDataPath);
                if (LItemsData != null)
                {
                    Items = LItemsData;
                }
            }

            #endregion

            #region 玩家数据

            string playersDataPath = "./data/PlayersData.json";
            var lPlayerData = TryRead<Dictionary<string, Dictionary<string, Player>>>(playersDataPath);
            if (lPlayerData != null)
            {
                Players = lPlayerData;
            }

            #endregion

            #region 宠物池

            string petPoolPath = "./datapack/PetPool.json";
            var lPetPool = TryRead<List<Pet>>(petPoolPath);
            if (lPetPool != null)
            {
                PetPool = lPetPool;
            }

            #endregion

            #region 副本数据

            string replicaPath = "./datapack/replicas.json";
            if (File.Exists(replicaPath))
            {
                var lReplicaData = TryRead<List<Replica>>(replicaPath);
                if (lReplicaData != null)
                {
                    Replicas = lReplicaData;
                }
            }

            #endregion
        }

        private static T? TryRead<T>(string dataPath) where T : class
        {
            if (File.Exists(dataPath))
            {
                try
                {
                    string json = File.ReadAllText(dataPath);
                    var data = JsonConvert.DeserializeObject<T>(json);
                    return data;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    ErrorDispose(dataPath);
                }
            }

            return null;
        }

        static void ErrorDispose(string path)
        {
            ReInput:
            Console.Write("检测到读取数据文件时发生错误，是否删除数据文件？(true/false):");
            if (!bool.TryParse(Console.ReadLine(), out bool delConfig))
            {
                goto ReInput;
            }

            if (delConfig)
            {
                File.Delete(path);
            }
            else
            {
                Environment.Exit(0);
            }
        }

        public static void SaveData()
        {
            #region 玩家数据

            string playerJson = JsonConvert.SerializeObject(Players);
            string path = "./data/PlayersData.json";
            if (!Directory.Exists("./data"))
            {
                Directory.CreateDirectory("./data");
            }

            File.WriteAllText(path, playerJson);

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
            public string? IconName = null;

            public bool Challenge(Player player, int count)
            {
                if (player.Pet == null || player.Pet.Energy < count * Energy)
                {
                    return false;
                }

                player.Pet.Health -= Attack * count;
                player.Pet.Energy -= Energy * count;
                player.Pet.Experience += ExpAdd * count;
                player.Points += RewardingPoint * count;
                foreach (var item in RewardingItems)
                {
                    if (!player.Bag.ContainsKey(item.Key))
                    {
                        player.Bag[item.Key] = item.Value;
                    }
                    else
                    {
                        player.Bag[item.Key] += item.Value;
                    }
                }

                return true;
            }
        }
    }
}