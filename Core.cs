using System.Drawing;
using Mirai.Net.Data.Messages;
using Mirai.Net.Utils.Scaffolds;
using OpenPetsWorld.PetTool;
using static OpenPetsWorld.Program;
using static OpenPetsWorld.Game;

namespace OpenPetsWorld;

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

        MessageChainBuilder builder = new();
        var player = Player.Register(group, member);
        if (player.Pet == null)
        {
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

        return builder.At(member).Plain("您已经有宠物了,贪多嚼不烂哦!\n◇指令:宠物放生").Build();
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
}