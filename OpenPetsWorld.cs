using Manganese.Text;
using Mirai.Net.Data.Messages.Receivers;
using Newtonsoft.Json;
using OpenPetsWorld.Item;
using OpenPetsWorld.PetTool;
using System.Drawing;
using static OpenPetsWorld.Program;
using File = System.IO.File;

namespace OpenPetsWorld
{
    public static class OpenPetsWorld
    {
        public static readonly string[] SignTexts = { "奖励积分", "累签", "连签" };
        public static string[] Ranks = Array.Empty<string>();
        public static string[] Attributes = Array.Empty<string>();
        public static string[] UnitingPlace = Array.Empty<string>();

        public static int BreaksTime = 120;

        /// <summary>
        /// 怪物入侵
        /// </summary>
        public static bool BossIntruding = false;

        /// <summary>
        /// 玩家数据
        /// </summary>
        public static Dictionary<string, Dictionary<string, Player>> Players = new();

        /// <summary>
        /// 物品
        /// </summary>
        public static Dictionary<int, BaseItem> Items = new();

        /// <summary>
        /// 宠物卡池
        /// </summary>
        public static List<Pet> PetPool = new();

        /// <summary>
        /// 副本
        /// </summary>
        public static List<Replica> Replicas = new();

        /// <summary>
        /// 礼包
        /// </summary>
        private static List<Gift> Gifts = new();

        /// <summary>
        /// 宠物神榜刷新时间
        /// </summary>
        public static string UpdateTime = "";

        /// <summary>
        /// 积分商店
        /// </summary>
        public static Shop PointShop = new();

        /// <summary>
        /// 背景
        /// </summary>
        public static Image Wallpaper = new Bitmap(650, 500);

        public static int MaxIqAdd;
        public static int MinIqAdd;
        public static int MaxAttrAdd;
        public static int MinAttrAdd;
        public static int MaxExpAdd;
        public static int MinExpAdd;

        public static int MaxLevel = 300;

        /// <summary>
        /// 单次砸蛋所需积分
        /// </summary>
        public static int ExtractNeededPoint = 500;

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
            var playerData = Player.Register(groupId, memberId);
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
            return playerData.Pet != null;
        }

        public static Gift? FindGift(string giftName)
        {
            var gifts = Gifts.Where(gifts => gifts.Name == giftName).ToList();
            return gifts.Count != 0 ? gifts[0] : null;
        }

        public static BaseItem? FindItem(string itemName)
        {
            var items = Items.Values.Where(item => item.Name == itemName).ToList();
            return items.Count != 0 ? items[0] : null;
        }

        public static Replica? FindReplica(string replicaName)
        {
            return (from lReplica in Replicas where lReplica.Name == replicaName select lReplica).FirstOrDefault();
        }

        #region 读写数据文件

        /// <summary>
        /// 读取游戏数据
        /// </summary>
        public static void ReadData()
        {
            #region 宠物背景

            const string wallpaperPath = "./datapack/wallpaper.jpg";
            if (File.Exists(wallpaperPath))
            {
                Wallpaper = Image.FromFile(wallpaperPath);
            }

            #endregion

            #region 杂项

            Log.Info("读取杂项数据中…");
            const string MiscPath = "./datapack/misc.json";
            Misc misc = TryRead<Misc>(MiscPath);
            if (misc != null)
            {
                Ranks = misc.Ranks;
                UnitingPlace = misc.UnitingPlace;
                Attributes = misc.Attributes;
                BreaksTime = misc.BreaksTime;

                MaxAttrAdd = misc.MaxAttrAdd;
                MinAttrAdd = misc.MinAttrAdd;
                MaxExpAdd = misc.MaxExpAdd;
                MinExpAdd = misc.MinExpAdd;
                MaxIqAdd = misc.MaxIQAdd;
                MinIqAdd = misc.MinIQAdd;

                ExtractNeededPoint = misc.ExtractNeededPoint;
            }

            #endregion

            #region 物品数据

            Log.Info("读取物品数据中…");
            const string itemsDataPath = "./datapack/items.json";
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
            var dirs = Directory.GetDirectories("./data");
            Dictionary<string, Dictionary<string, Player>> group = new();
            foreach (var dir in dirs)
            {
                string playersPath = dir + "/players.json";
                var groupName = Path.GetFileName(dir);
                var lPlayers = TryRead<Dictionary<string, Player>>(playersPath);
                if (lPlayers == null) continue;

                group[groupName] = lPlayers;
            }

            Players = group;

            #endregion

            #region 宠物池

            Log.Info("读取宠物池数据中…");
            const string petPoolPath = "./datapack/PetPool.json";
            var lPetPool = TryRead<List<Pet>>(petPoolPath);
            if (lPetPool != null)
            {
                PetPool = lPetPool;
            }

            #endregion

            #region 副本数据

            Log.Info("读取副本数据中…");
            const string replicaPath = "./datapack/replicas.json";
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
            const string pointShopPath = "./datapack/pointshop.json";
            if (File.Exists(pointShopPath))
            {
                var lCommData = TryRead<Shop>(pointShopPath);
                if (lCommData != null)
                {
                    PointShop = lCommData;
                }
            }

            #endregion

            #region 礼包数据

            Log.Info("读取礼包数据中…");
            const string giftsPath = "./datapack/gifts.json";
            if (File.Exists(giftsPath))
            {
                var lGifts = TryRead<List<Gift>>(giftsPath);
                if (lGifts != null)
                {
                    Gifts = lGifts;
                }
            }

            #endregion
        }

        public static T? TryRead<T>(string dataPath) where T : class
        {
            if (!File.Exists(dataPath)) return null;

            try
            {
                var json = File.ReadAllText(dataPath);
                var data = JsonConvert.DeserializeObject<T>(json);
                return data;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                ErrorDispose(dataPath);
            }

            return null;
        }

        private static void ErrorDispose(string path)
        {
            while (true)
            {
                Console.Write("检测到读取数据文件时发生错误，是否删除数据文件？(Y/N):");

                var input = Console.ReadLine();
                switch (input)
                {
                    case "N":
                        KeysExit();
                        break;
                    case "Y":
                        File.Delete(path);
                        KeysExit();
                        break;
                }
            }
        }

        public static void SaveData()
        {
            #region 玩家数据

            JsonSerializerSettings setting = new JsonSerializerSettings
            {
                DefaultValueHandling = DefaultValueHandling.Ignore
            };

            foreach (var group in Players)
            {
                var dir = "./data/" + group.Key;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                string playerJson = group.Value.ToJsonString(setting);
                string path = dir + "/players.json";
                File.WriteAllText(path, playerJson);
            }

            #endregion
        }

        #endregion
    }
}