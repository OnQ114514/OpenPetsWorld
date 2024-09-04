using OpenPetsWorld.PetTool;
using SkiaSharp;
using Sora.Entities;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.LightLog;
using static OpenPetsWorld.Program;
using static OpenPetsWorld.Game;

namespace OpenPetsWorld;

public static class Commands
{
    public static MessageBody? Gacha(long group, long sender)
    {
        if (Banner.Count == 0)
        {
            Log.Error("Command", "宠物卡池为空！请检测数据文件");
            return null;
        }

        var builder = new MessageBodyBuilder();
        var player = Player.Register(group, sender);
        if (player.Pet != null)
        {
            return builder
                .At(sender)
                .Plain("您已经有宠物了,贪多嚼不烂哦!\n◇指令:宠物放生")
                .Build();
        }

        if (player.Points < PlayConfig.GachaPoint)
        {
            return builder
                .At(sender)
                .Plain($" 您的积分不足,无法进行砸蛋！\n【所需[{PlayConfig.GachaPoint}]积分】\n请发送【签到】获得积分")
                .Build();
        }

        player.Points -= PlayConfig.GachaPoint;
        var pet = Pet.Gacha();

        player.Pet = pet;

        return builder
            .At(sender)
            .Plain($" 恭喜您砸到了一颗{pet.Attribute}属性的宠物蛋")
            .Image(pet.Render())
            .Build();
    }

    public static MessageBody? GachaTen(long groupId, long senderId, string senderName)
    {
        if (Banner.Count == 0)
        {
            Log.Error("Command", "宠物卡池为空！请检测数据文件");
            return null;
        }

        var player = Player.Register(groupId, senderId);

        MessageBodyBuilder builder = new();
        if (player.Pet != null) return builder.At(senderId).Plain("您已经有宠物了,贪多嚼不烂哦!\n◇指令:宠物放生").Build();

        var neededPoint = PlayConfig.GachaPoint * 10;
        if (player.Points < neededPoint)
        {
            return builder
                .At(senderId)
                .Plain($" 您的积分不足,无法进行砸蛋!\n【所需[{neededPoint}]积分】\n请发送【签到】获得积分").Build();
        }

        player.Points -= neededPoint;

        List<string> texts = [];
        List<Pet> pets = [];
        for (var i = 0; i < 10; i++)
        {
            var pet = Pet.Gacha();
        }

        player.GachaPets = pets;

        using var image = Renders.GachaTen(texts, senderName);

        return builder.Image(image).Build();
    }

    public static MessageBody? Evolve(long groupId, long senderId)
    {
        if (!HavePet(groupId, senderId, out var pet)) return null;

        var statusCode = pet.Evolve(out var level);
        string text;
        MessageBodyBuilder builder = new();
        switch (statusCode)
        {
            case 0:
                var path = Path.GetFullPath($"./datapack/peticon/{pet.IconName}");
                if (pet.IconName != null) builder.Image(path);
                text = $" 你的{pet.Name}成功进化至[LV·{level}级]{pet.Stage.ToStr()}·{pet.Name}]！";
                break;

            case -1:
                text = "你的宠物暂时无法进化哦！";
                break;

            case -2:
                text = $"你的[{pet.Name}]已达到最终形态！！！";
                break;

            case -3:
                var process = string.Join("→",
                    pet.Morphologies.Select((morphology, index) =>
                        $"{index}-{morphology.Level}-{morphology.Name}"));
                text = $"你的[{pet.Name}]等级不足，以下为进化流程：\n" + process;
                break;
            default:
                throw new Exception();
        }

        return builder.At(senderId).Plain(text).Build();
    }

    public static MessageBody? LevelUp(long groupId, long senderId, int levelsToUpgrade = 1)
    {
        if (!HavePet(groupId, senderId, out var pet)) return null;
        
        string text;
        
        var originalPower = pet.Power;
        var currentLevel = pet.Level;
        long allExp = 0;
        long allHealth = 0;
        long allAttribute = 0;
        var addedLevel = 0;
        long nextExpNeeded = 0;

        for (var i = 0; i < levelsToUpgrade; i++)
        {
            if (currentLevel == PlayConfig.MaxLevel)
            {
                text = "你的宠物等级已达最高等级！";
                goto Send;
            }

            var expNeeded = GetLevelUpExp(currentLevel);

            // 检查是否有足够的经验升级
            if (allExp + expNeeded > pet.Experience) break;

            allHealth += 2 * Pow(currentLevel, 2) + 4 * currentLevel + 10;
            allAttribute += 3 * currentLevel + 1;
            currentLevel++;
            addedLevel++;
            allExp += expNeeded; // 累加经验
        }

        if (addedLevel == 0)
        {
            text = $"您的宠物经验不足,无法升级,升级到[Lv·{pet.Level + 1}]级还需要[{pet.MaxExperience - pet.Experience}]经验值!";
            goto Send;
        }

        // 确保不会扣除超过当前经验的值
        if (pet.Experience < allExp)
        {
            allExp = pet.Experience;
        }

        pet.Level = currentLevel;
        pet.MaxExperience = nextExpNeeded;
        pet.Experience -= allExp;
        pet.BaseMaxHealth += allHealth;
        pet.BaseAttack += allAttribute;
        pet.BaseDefense += allAttribute;

        text = $"您的[{pet.Name}]成功升级啦!\n"
               + "------------------\n"
               + $"● 等级提升：+{addedLevel}\n"
               + $"● 经验减少：-{allExp}\n"
               + $"● 生命提升：+{allHealth}\n"
               + $"● 攻击提升：+{allAttribute}\n"
               + $"● 防御提升：+{allAttribute}\n"
               + $"● 战力提升：+{pet.Power - originalPower}\n"
               + "------------------";

        Send:
        return new MessageBodyBuilder().At(senderId).Plain(text).Build();
    }

    public static void UseItem(GroupMessageEventArgs eventArgs)
    {
        var context = eventArgs.Message;
        Tools.ParseString(context, 2, out var itemName, out var count, out var target);
        if (!IsCompliant(eventArgs, count)) return;

        if (!Items.TryGetValue(itemName, out var item))
        {
            eventArgs.SendAtMessage("该道具并不存在，请检查是否输错！");
            return;
        }

        var player = Player.Register(eventArgs);
        player.Bag.TryAdd(item.Name, 0);
        if (count == -1) count = player.Bag[item.Name];

        item.Use(eventArgs, count);
    }

    public static void Trade(GroupMessageEventArgs eventArgs)
    {
        var context = eventArgs.Message;
        Tools.ParseString(context, 2, out var itemName, out var count, out var targetId);

        if (!IsCompliant(eventArgs, count)) return;

        if (!Items.TryGetValue(itemName, out var item))
        {
            eventArgs.SendAtMessage("该道具并不存在，请检查是否输错！");
            return;
        }

        if (!item.CanTrade)
        {
            eventArgs.SendAtMessage("该道具不可交易！");
            return;
        }

        var player = Player.Register(eventArgs);
        var group = eventArgs.SourceGroup.Id;

        if (targetId == null || !Players[group].TryGetValue(targetId.Value, out var targetPlayer))
        {
            eventArgs.SendAtMessage("目标玩家不存在！");
            return;
        }

        // 确保玩家和目标玩家的背包都有道具的条目
        player.Bag.TryAdd(item.Name, 0);
        targetPlayer.Bag.TryAdd(item.Name, 0);

        // 如果count为-1，则设置为玩家背包中的数量
        if (count == -1) count = player.Bag[item.Name];

        if (player.Bag[item.Name] < count)
        {
            eventArgs.SendAtMessage($"你的背包中【{item.Name}】不足{count}个！");
            return;
        }

        // 执行交易
        player.Bag[item.Name] -= count;
        targetPlayer.Bag[item.Name] += count;

        // 交易成功的反馈
        eventArgs.SendAtMessage("物品转让成功！");
    }

    public static void Attack(GroupMessageEventArgs eventArgs)
    {
        var context = eventArgs.Message;
        var groupId = eventArgs.SourceGroup.Id;

        var target = GetAtNumber(context.MessageBody);
        if (target == null)
        {
            eventArgs.SendAtMessage("目标玩家不存在！");
            return;
        }

        var player = Player.Register(eventArgs);
        var tPlayer = Player.Register(groupId, target.Value);
        if (!HavePet(eventArgs)) return;

        if (tPlayer.Pet == null)
        {
            eventArgs.SendAtMessage("对方并没有宠物，无法对目标发起攻击！");
            return;
        }

        var attack = tPlayer.Pet.Damage(player.Pet.Attack, player.Pet.Intellect);
        tPlayer.Pet.Health -= attack;
        eventArgs.Reply(
            $"【{player.Pet.Name} VS {tPlayer.Pet.Name}】\n" +
            $"属性:[{player.Pet.Attribute}] -- [{tPlayer.Pet.Attribute}]\n" +
            //TODO:完善结果
            $"你的宠物直接KO对方宠物\n" +
            $"● 经验：+0\n" +
            $"---------------\n" +
            $"对方血量扣除：-{attack}\n" +
            $"我方血量扣除：-0\n" +
            $"对方剩余血量：{tPlayer.Pet.Health}\n" +
            $"我方剩余血量：{player.Pet.Health}");
    }

    public static void Buy(GroupMessageEventArgs eventArgs)
    {
        var context = eventArgs.Message;

        Tools.ParseString(context, 2, out var itemName, out var count, out _);

        if (!IsCompliant(eventArgs, count)) return;

        if (!Items.TryGetValue(itemName, out var item))
        {
            eventArgs.SendAtMessage("该道具并不存在，请检查是否输错！");
            return;
        }

        if (PointShop.Commodities.TryGetValue(item.Name, out var unitPrice))
        {
            var price = unitPrice * count;

            var player = Player.Register(eventArgs);
            var succeeded = player.Buy(item.Name, count);
            eventArgs.SendAtMessage(succeeded
                ? $"购买成功！获得{count}个{item.Name},本次消费{price}积分！可发送[我的背包]查询物品！\n物品说明：{item.Description}"
                : $"你的积分不足[{price}]，无法购买！");
        }
        else
        {
            eventArgs.SendAtMessage("宠物商店内未有此物品的身影！");
        }
    }

    /// <summary>
    /// 合成
    /// </summary>
    public static async void Make(GroupMessageEventArgs eventArgs)
    {
        var senderId = eventArgs.Sender.Id;
        var context = eventArgs.Message;
        var textMessage = context.GetText();

        if (textMessage.Length < 3)
        {
            eventArgs.SendAtMessage("◇指令:合成+道具*数量");
            return;
        }

        Tools.ParseString(context, 2, out var itemName, out var count, out _);
        if (!IsCompliant(eventArgs, count)) return;

        if (!Items.TryGetValue(itemName, out var item))
        {
            eventArgs.SendAtMessage("此物品不存在，或者输入错误！");
            return;
        }

        if (!item.Make(eventArgs, count)) return;

        var path = $"./itemicon/{item.DescriptionImageName}.png";
        var icon = SKBitmap.Decode(path);
        await eventArgs.Reply(new MessageBodyBuilder()
            .Image(icon)
            .At(senderId)
            .Plain($" 合成成功了！恭喜你获得了道具·{itemName}*{count}")
            .Build());
    }

    public static void Sell(GroupMessageEventArgs eventArgs)
    {
        var context = eventArgs.Message;
        var textMessage = context.GetText();

        if (textMessage.Length < 3)
        {
            eventArgs.SendAtMessage("◇指令:出售+道具*数量");
            return;
        }

        Tools.ParseString(context, 2, out var itemName, out var count, out _);

        var item = Items[itemName];
        if (item.Price == null)
        {
            eventArgs.SendAtMessage("此物品不能出售！");
            return;
        }

        var player = Player.Register(eventArgs);
        if (player.Bag[itemName] < count)
        {
            eventArgs.SendAtMessage($"你背包中的【{itemName}】不足×{count}个!");
            return;
        }

        var points = item.Price.Value * count;
        player.Bag[itemName] -= count;
        player.Points += points;
        eventArgs.SendAtMessage($"成功出售【{itemName}×{count}】,成功卖出了【{points}】积分!");
    }

    /// <summary>
    /// 判断数量是否合法
    /// </summary>
    /// <param name="eventArgs"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    private static bool IsCompliant(GroupMessageEventArgs eventArgs, int count)
    {
        switch (count)
        {
            case -1:
                return true;
            case 0:
                eventArgs.SendAtMessage("格式错误！");
                return false;
            //TODO: 自定义最大数量
            case > 99999:
                eventArgs.SendAtMessage("数量超出范围！");
                return false;
            default:
                return true;
        }
    }
}