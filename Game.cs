using System.Drawing;
using System.Timers;
using Manganese.Text;
using Newtonsoft.Json;
using OpenPetsWorld.Item;
using OpenPetsWorld.PetTool;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.LightLog;
using static OpenPetsWorld.Program;

namespace OpenPetsWorld
{
    public static class Game
    {
        /// <summary>
        /// 怪物入侵
        /// </summary>
        public static bool BossIntruding = false;

        /// <summary>
        /// 玩家数据
        /// </summary>
        public static Dictionary<long, Dictionary<long, Player>> Players = new();

        /// <summary>
        /// 物品
        /// </summary>
        public static Dictionary<string, BaseItem> Items = new();

        /// <summary>
        /// 宠物卡池
        /// </summary>
        public static List<Pet> Banner = new();

        /// <summary>
        /// 副本
        /// </summary>
        public static List<Instance> Instances = new();

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

        public static GameConfig PlayConfig = new();

        public static bool HavePet(GroupMessageEventArgs x, bool send = true)
        {
            return HavePet(x.SourceGroup.Id, x.Sender.Id, send);
        }

        public static bool HavePet(GroupMessageEventArgs x, out Pet pet, bool send = true)
        {
            return HavePet(x.SourceGroup.Id, x.Sender.Id, out pet, send);
        }

        public static bool HavePet(long groupId, long senderId, bool send = true)
        {
            var playerData = Player.Register(groupId, senderId);
            if (playerData.Pet != null)
            {
                return true;
            }

            if (send)
            {
                SendAtMessage(groupId, senderId, "您当前还没有宠物,赶紧邂逅您的宠物!\n◇指令:砸蛋");
            }

            return false;
        }

        public static bool HavePet(long groupId, long memberId, out Pet? pet, bool send = true)
        {
            if (HavePet(groupId, memberId, send))
            {
                pet = Player.Register(groupId, memberId).Pet;
                return true;
            }

            pet = null;
            return false;
        }

        public static void EnergyRecovery(object? sender, ElapsedEventArgs e)
        {
            foreach (var player in Players.Values.SelectMany(group => group.Values))
                player.EnergyAdd();
        }

        public static Gift? FindGift(string giftName)
        {
            var gifts = Gifts.Where(gifts => gifts.Name == giftName).ToList();
            return gifts.Count != 0 ? gifts[0] : null;
        }

        public static Instance? FindReplica(string replicaName)
        {
            return (from lReplica in Instances where lReplica.Name == replicaName select lReplica).FirstOrDefault();
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
            else
            {
                using var graphics = Graphics.FromImage(Wallpaper);
                graphics.Clear(Color.White);
            }

            #endregion

            #region 游戏配置

            Log.Info("Reading", "读取游戏配置数据中…");
            const string miscPath = "./datapack/game.json";
            var config = TryRead<GameConfig>(miscPath);
            if (config != null)
            {
                PlayConfig = config;
            }

            #endregion

            #region 物品数据

            Log.Info("Reading", "读取物品数据中…");
            const string itemsPath = "./datapack/items.json";
            if (File.Exists(itemsPath))
            {
                var localItems = ItemReader.Read(itemsPath);
                if (localItems != null)
                {
                    Items = localItems;
                }
            }
            else
            {
                Log.Warning("Reading", "OpenPetsWorld 总是需要一个数据包，您可以参照文档进行创建，或下载现有数据包，或从宠物世界转换！");
            }

            #endregion

            #region 玩家数据

            const string playerPath = "./data";
            Log.Info("Reading", "读取玩家数据中…");

            if (!Directory.Exists(playerPath))
            {
                Directory.CreateDirectory(playerPath);
                goto Banner;
            }

            var dirs = Directory.GetDirectories(playerPath);
            var group = new Dictionary<long, Dictionary<long, Player>>();
            
            foreach (var dir in dirs)
            {
                var playersPath = dir + "/players.json";
                
                var groupIdText = Path.GetFileName(dir);
                if (long.TryParse(groupIdText, out var groupId)) continue;
                
                var localPlayers = TryRead<Dictionary<long, Player>>(playersPath);
                if (localPlayers == null) continue;

                group[groupId] = localPlayers;
            }

            Players = group;

            #endregion

            #region 宠物池

            Banner:
            Log.Info("Reading", "读取宠物池数据中…");
            const string bannerPath = "./datapack/banner.json";
            var localBanner = TryRead<List<Pet>>(bannerPath);
            if (localBanner != null)
            {
                Banner = localBanner;
            }

            #endregion

            #region 副本数据

            Log.Info("Reading", "读取副本数据中…");
            const string replicaPath = "./datapack/replicas.json";
            if (File.Exists(replicaPath))
            {
                var localInstance = TryRead<List<Instance>>(replicaPath);
                if (localInstance != null)
                {
                    Instances = localInstance;
                }
            }

            #endregion

            #region 商品数据

            Log.Info("Reading", "读取商品数据中…");
            const string pointShopPath = "./datapack/pointshop.json";
            if (File.Exists(pointShopPath))
            {
                var localComm = TryRead<Shop>(pointShopPath);
                if (localComm != null)
                {
                    PointShop = localComm;
                }
            }

            #endregion

            #region 礼包数据

            Log.Info("Reading", "读取礼包数据中…");
            const string giftsPath = "./datapack/gifts.json";
            if (File.Exists(giftsPath))
            {
                var localGifts = TryRead<List<Gift>>(giftsPath);
                if (localGifts != null)
                {
                    Gifts = localGifts;
                }
            }

            #endregion
        }

        private static T? TryRead<T>(string dataPath) where T : class
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
            Console.Write("检测到读取数据文件时发生错误，是否删除数据文件？(y/N):");

            var input = Console.ReadLine();
            if (input == "Y")
            {
                File.Delete(path);
                KeysExit();
                return;
            }

            KeysExit();
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