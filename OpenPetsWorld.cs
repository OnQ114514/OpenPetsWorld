using System.Drawing;
using Manganese.Text;
using Newtonsoft.Json;
using Mirai.Net.Data.Messages.Receivers;
using static OpenPetsWorld.Program;
using File = System.IO.File;
using OpenPetsWorld.Item;
using OpenPetsWorld.PetTool;

namespace OpenPetsWorld
{
    public static class OpenPetsWorld
    {
        public static readonly string[] SignTexts = { "奖励积分", "累签", "连签" };
        public static string[] Ranks = Array.Empty<string>();
        public static string[] Attributes = Array.Empty<string>();
        public static string[] UnitingPlace = Array.Empty<string>();

        public static int BreaksTime = 120;

        public static readonly Dictionary<string, long> SentTime = new();
        public static Dictionary<string, Dictionary<string, Player>> Players = new();
        public static Dictionary<int, BaseItem> Items = new();
        public static List<Pet> PetPool = new();
        public static List<Replica> Replicas = new();
        public static Shop PointShop = new();

        public static Image Wallpaper = new Bitmap(650, 500);
        
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

        private static bool HavePet(string groupId, string memberId, out Pet? petData, bool send = true)
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
            var items = Items.Values.Where(litems => litems.Name == itemName).ToList();
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
            #region 宠物背景

            string wallpaperPath = "./datapack/wallpaper.jpg";
            if (File.Exists(wallpaperPath))
            {
                Wallpaper = Image.FromFile(wallpaperPath);
            }

            #endregion
            
            #region 杂项

            Log.Info("读取杂项数据中…");
            string MiscPath = "./datapack/misc.json";
            Misc misc = TryRead<Misc>(MiscPath);
            if (misc != null)
            {
                Ranks = misc.Ranks;
                UnitingPlace = misc.UnitingPlace;
                Attributes = misc.Attributes;
                BreaksTime = misc.BreaksTime;
            }

            #endregion

            #region 物品数据

            Log.Info("读取物品数据中…");
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

            Log.Info("读取玩家数据中…");
            string playersDataPath = "./data/players.json";
            var lPlayerData = TryRead<Dictionary<string, Dictionary<string, Player>>>(playersDataPath);
            if (lPlayerData != null)
            {
                Players = lPlayerData;
            }

            #endregion

            #region 宠物池
            
            Log.Info("读取宠物池数据中…");
            string petPoolPath = "./datapack/PetPool.json";
            var lPetPool = TryRead<List<Pet>>(petPoolPath);
            if (lPetPool != null)
            {
                PetPool = lPetPool;
            }

            #endregion

            #region 副本数据

            Log.Info("读取副本数据中…");
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

            #region 商品数据

            Log.Info("读取商品数据中…");
            string pointShopPath = "./datapack/pointshop.json";
            if (File.Exists(pointShopPath))
            {
                var lCommData = TryRead<Shop>(pointShopPath);
                if (lCommData != null)
                {
                    lCommData.Initialize();
                    PointShop = lCommData;
                }
            }

            #endregion
        }

        public static T? TryRead<T>(string dataPath) where T : class
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

            string playerJson = Players.ToJsonString();
            string path = "./data/players.json";
            if (!Directory.Exists("./data"))
            {
                Directory.CreateDirectory("./data");
            }

            File.WriteAllText(path, playerJson);

            #endregion
        }

        #endregion
    }
}