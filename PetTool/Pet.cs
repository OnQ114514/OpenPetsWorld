using System.Drawing;
using System.Text.Json.Serialization;
using Mirai.Net.Data.Messages.Receivers;
using OpenPetsWorld.Item;
using static OpenPetsWorld.OpenPetsWorld;
using static OpenPetsWorld.Program;

namespace OpenPetsWorld.PetTool;

public class Pet
{
    public int Energy = 100;
    public int Health;
    public int Experience;
    public int MaxEnergy = 100;
    public int MaxHealth;
    public int MaxExperience = 160;
    public int Level = 1;
    public string Name;
    public string Gender;
    public Stage Stage = Stage.Infancy;
    public string Attribute;
    public string Rank;
    public string State = "正常";
    public string? IconName;
    public string PettAlent = "无";
    public int Intellect = 4;
    public int Attack = 10;
    public int Defense = 10;
    public Artifact Artifact = Artifact.Null;
    public int Mood = 50;
    public List<Morphology>? Morphologies;

    public Pet()
    {
        //示例宠物
        IconName = "kiana.jpg";
        MaxHealth = Program.Random.Next(100, 301);
        Health = MaxHealth;
        Name = "test";

        #region 性别随机

        Gender = RandomBool() ? "雌" : "雄";

        #endregion

        #region 级别随机

        Rank = Ranks[Program.Random.Next(0, 4)];

        #endregion

        #region 属性随机

        Attribute = Attributes[Program.Random.Next(0, 5)];

        #endregion
    }

    [JsonIgnore] public int Power => (Attack + Defense + MaxHealth) / 10 + Intellect * 20;

    public string GetMoodSymbol()
    {
        string star = string.Empty;
        int starNumber = (int)Math.Round((double)Mood / 10);
        for (int i = 0; i < starNumber; i++)
        {
            star += "★";
        }

        return star;
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

    public int Evolved(out int level)
    {
        level = 0;
        if (Morphologies == null)
        {
            //无法进化
            return -1;
        }

        int target = (int)Stage;
        if (Morphologies.Count - 1 < target)
        {
            //已是最终形态
            return -2;
        }

        Stage += 1;
        var morphology = Morphologies[target];

        if (morphology.Level > Level)
        {
            //等级不足
            return -3;
        }

        level = morphology.Level;
        if (morphology.Name != null) Name = morphology.Name;
        if (morphology.IconName != null) IconName = morphology.IconName;

        return 0;
    }

    public static Pet Extract()
    {
        int index = Program.Random.Next(0, PetPool.Count);
        return PetPool[index];
    }

    public int Damage(Pet myPet)
    {
        return (myPet.Attack + myPet.Intellect * 20) *
               (1 - (Defense * Intellect * 20) / (Attack + Defense + Health / 10 + Intellect * 20));
    }

    public bool RemoveArtifact(GroupMessageReceiver receiver)
    {
        if (Artifact.Id == -1)
        {
            return false;
        }

        Player player = Player.Register(receiver);
        player.Bag.MergeValue(Artifact.Id, 1);
        Artifact = Artifact.Null;

        return true;
    }

    public Image Render()
    {
        var image = (Image)Wallpaper.Clone();
        using var graphics = Graphics.FromImage(image);

        string iconPath = $"./datapack/peticon/{IconName}";
        if (IconName != null && File.Exists(iconPath))
        {
            graphics.DrawImage(Image.FromFile(iconPath), 5, 5, 380, 380);
        }

        string[] abTexts =
        {
            $"心情:{GetMoodSymbol()}",
            $"精力:{Energy.ToFormat()}/{MaxEnergy.ToFormat()}",
            $"血量:{Health.ToFormat()}/{MaxHealth.ToFormat()}",
            $"经验:{Experience.ToFormat()}/{MaxExperience.ToFormat()}"
        };
        int n = 390;
        foreach (string abText in abTexts)
        {
            graphics.DrawString(abText, YaHei, Black, 15, n);
            n += 25;
        }

        string[] abTexts2 =
        {
            $"等级:{Level}",
            $"昵称:{Name}",
            $"性别:{Gender}",
            $"阶段:{Stage.ToStr()}",
            $"属性:{Attribute}",
            $"级别:{Rank}",
            $"状态:{State}",
            $"神器:{Artifact.Name}",
            $"天赋:{PettAlent}",
            $"战力:{Power.ToFormat()}",
            $"智力:{Intellect.ToFormat()}",
            $"攻击:{Attack.ToFormat()}",
            $"防御:{Defense.ToFormat()}"
        };

        int n2 = 20;
        foreach (string abText in abTexts2)
        {
            graphics.DrawString("◆" + abText, YaHei, Black, 395, n2);
            n2 += 35;
        }

        return image;
    }
}