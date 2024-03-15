using System.Drawing;
using Mirai.Net.Data.Messages;
using Mirai.Net.Utils.Scaffolds;
using OpenPetsWorld.PetTool;
using static OpenPetsWorld.Program;
using static OpenPetsWorld.Game;

namespace OpenPetsWorld;

// 由于现在的mirai基本无法脱离overflow+onebot的组合，所以打算移植到其他onebot框架上，将消息处理部分移至Core类
// 为什么保留Mirai.Net的MessageChain?因为真的好用（
// 以后换其他框架我可能会自己实现一个MessageChain（
public static class Core
{
    public static MessageChain? OpenEgg(string group, string member)
    {
        if (PetPool.Count == 0)
        {
            Log.Error("宠物卡池为空！请检测数据文件");
            return null;
        }

        var player = Player.Register(group, member);
        if (player.Pet == null)
        {
            if (player.Points < ExtractNeededPoint)
            {
                return new MessageChainBuilder().At(member)
                    .Plain($"您的积分不足,无法进行砸蛋!\n【所需[{ExtractNeededPoint}]积分】\n请发送【签到】获得积分")
                    .Build();
            }

            player.Points -= ExtractNeededPoint;
            var pet = Pet.Gacha();

            player.Pet = pet;

            return new MessageChainBuilder()
                .At(member)
                .Plain($" 恭喜您砸到了一颗{pet.Attribute}属性的宠物蛋")
                .ImageFromBase64(ToBase64(pet.Render()))
                .Build();
        }
        else
        {
            return new MessageChainBuilder().At(member)
                .Plain("您已经有宠物了,贪多嚼不烂哦!\n◇指令:宠物放生")
                .Build();
        }
    }

    public static MessageChain? OpenTenEggs(string group, string member)
    {
        if (PetPool.Count == 0)
        {
            Log.Error("宠物卡池为空！请检测数据文件");
            return null;
        }

        var player = Player.Register(group, member);

        MessageChainBuilder builder = new();
        if (player.Pet != null) return builder.At(member).Plain("您已经有宠物了,贪多嚼不烂哦!\n◇指令:宠物放生").Build();

        var neededPoint = ExtractNeededPoint * 10;
        if (player.Points < neededPoint)
        {
            return builder.At(member).Plain($"您的积分不足,无法进行砸蛋!\n【所需[{neededPoint}]积分】\n请发送【签到】获得积分").Build();
        }

        player.Points -= neededPoint;

        List<string> texts = new();
        List<Pet> pets = new();
        for (var i = 0; i < 10; i++)
        {
            var pet = Pet.Gacha();
            pets.Add(pet);

            texts.Add($"[{i + 1}]{pet.Rank}-{pet.Name}");
        }

        player.GachaPets = pets;

        #region 绘图

        using Bitmap bitmap = new(480, 480);
        using var graphics = Graphics.FromImage(bitmap);
        using Font font = new("Microsoft YaHei", 23, FontStyle.Regular);

        graphics.Clear(Color.White);
        graphics.DrawString($"[@{member}]", font, Black, 3, 3);
        graphics.DrawString("◇指令：选择+数字", font, Black, 3, 440);

        var y = 40;
        foreach (var text in texts)
        {
            graphics.DrawString(text, font, Black, 3, y);
            y += 40;
        }

        #endregion

        return builder.ImageFromBase64(ToBase64(bitmap)).Build();
    }

    public static MessageChain? Evolve(string group, string member)
    {
        if (!HavePet(group, member, out var pet)) return null;

        var statusCode = pet.Evolve(out var level);
        string text;
        MessageChainBuilder builder = new();
        switch (statusCode)
        {
            case 0:
                var path = Path.GetFullPath($"./datapack/peticon/{pet.IconName}");
                if (pet.IconName != null) builder.ImageFromPath(path);
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
                throw new();
        }

        return builder.At(member).Plain(text).Build();
    }

    public static MessageChain? LevelUp(string group, string member, int levelsToUpgrade = 1)
    {
        if (!HavePet(group, member, out var pet)) return null;

        string text;

        var originalPower = pet.Power;
        var currentLevel = pet.Level;
        var allExp = 0;
        var allHealth = 0;
        var allAttribute = 0;
        var addedLevel = 0;
        var nextExpNeeded = 0;

        for (var i = 0; i < levelsToUpgrade; i++)
        {
            if (currentLevel == MaxLevel)
            {
                text = "你的宠物等级已达最高等级！";
                goto Send;
            }

            var expNeeded = nextExpNeeded = GetLevelUpExp(currentLevel);
            var tempExp = allExp + expNeeded;

            if (allExp > pet.Experience) break;

            allHealth += 2 * Pow(currentLevel, 2) + 4 * currentLevel + 10;
            allAttribute += 3 * currentLevel + 1;

            currentLevel++;
            addedLevel++;

            allExp = tempExp;
        }

        if (addedLevel == 0)
        {
            text =
                $"您的宠物经验不足,无法升级,升级到[Lv·{pet.Level + 1}]级还需要[{pet.MaxExperience - pet.Experience}]经验值!";
            goto Send;
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
        return new MessageChainBuilder().At(member).Plain(text).Build();
    }
}