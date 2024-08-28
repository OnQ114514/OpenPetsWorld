using System.Drawing;
using Newtonsoft.Json;
using OpenPetsWorld.Item;
using Sora.EventArgs.SoraEvent;
using static OpenPetsWorld.Game;
using static OpenPetsWorld.Program;

namespace OpenPetsWorld.PetTool;

public class Pet
{
    public long Energy = 100;
    public long Health;
    public long Experience;
    public int Level = 1;
    public string Name;
    public long MaxExperience = 160;

    [JsonIgnore]
    public long MaxEnergy => BaseMaxEnergy + Artifact.Energy;

    [JsonIgnore]
    public long MaxHealth => BaseMaxHealth + Artifact.Health;

    [JsonIgnore]
    public long Intellect => BaseIntellect + Artifact.Intellect;

    [JsonIgnore]
    public long Attack => BaseAttack + Artifact.Attack;

    [JsonIgnore]
    public long Defense => BaseDefense + Artifact.Defense;

    [JsonProperty(PropertyName = "MaxEnergy")]
    public long BaseMaxEnergy = 100;
    [JsonProperty(PropertyName = "MaxHealth")]
    public long BaseMaxHealth;
    [JsonProperty(PropertyName = "longellect")]
    public long BaseIntellect = 4;
    [JsonProperty(PropertyName = "Attack")]
    public long BaseAttack = 10;
    [JsonProperty(PropertyName = "Defense")]
    public long BaseDefense = 10;

    /// <summary>
    /// 性别
    /// </summary>
    [JsonIgnore]
    public string Gender
    {
        get
        {
            if (_humanoid)
            {
                return _gender ? "男" : "女";
            }

            return _gender ? "雄" : "雌";
        }
    }

    /// <summary>
    /// 实际性别
    /// </summary>
    private bool _gender;
    /// <summary>
    /// 是否显示为人的性别
    /// </summary>
    private bool _humanoid;

    public Stage Stage = Stage.Infancy;
    public string Attribute;
    public string Rank;
    //TODO:完善状态
    public string State = "正常";
    public string? IconName;
    public string PetTalent = "无";
    
    public Artifact Artifact = Artifact.Null;
    public int Mood = 50;
    public List<Morphology>? Morphologies;

    [JsonIgnore]
    public long Power => (Attack + Defense + MaxHealth) / 10 + Intellect * 20;

    private string GetMoodSymbol()
    {
        var star = string.Empty;
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

    /// <summary>
    /// 进化
    /// </summary>
    /// <param name="level">进化所需等级</param>
    /// <returns></returns>
    public int Evolve(out int level)
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

    public static Pet Gacha()
    {
        var index = Program.Random.Next(0, Banner.Count);
        return Banner[index];
    }

    /// <summary>
    /// 对该宠物造成伤害的公式
    /// </summary>
    /// <param name="attack"></param>
    /// <param name="intellect"></param>
    /// <returns></returns>
    public long Damage(long attack, long intellect = 0)
    {
        //TODO:支持动态公式
        return (attack + intellect * 20) *
               (1 - Defense * Intellect * 20 / (Attack + Defense + Health / 10 + Intellect * 20));
    }

    public bool RemoveArtifact(GroupMessageEventArgs receiver)
    {
        if (Artifact.Name == Item.Artifact.Null.Name)
        {
            return false;
        }

        var player = Player.Register(receiver);
        player.Bag.MergeValue(Artifact.Name, 1);
        Artifact = Artifact.Null;

        return true;
    }

    public Image Render()
    {
        var image = (Image)Wallpaper.Clone();
        using var graphics = Graphics.FromImage(image);

        var iconPath = $"./datapack/peticon/{IconName}";
        if (IconName != null && File.Exists(iconPath))
        {
            graphics.DrawImage(Image.FromFile(iconPath), 5, 5, 380, 380);
        }

        string[] abTexts =
        [
            $"心情:{GetMoodSymbol()}",
            $"精力:{Energy.ToFormat()}/{MaxEnergy.ToFormat()}",
            $"血量:{Health.ToFormat()}/{MaxHealth.ToFormat()}",
            $"经验:{Experience.ToFormat()}/{MaxExperience.ToFormat()}"
        ];
        int n = 390;
        foreach (var abText in abTexts)
        {
            graphics.DrawString(abText, YaHei, Black, 15, n);
            n += 25;
        }

        string[] abTexts2 =
        [
            $"等级:{Level}",
            $"昵称:{Name}",
            $"性别:{Gender}",
            $"阶段:{Stage.ToStr()}",
            $"属性:{Attribute}",
            $"级别:{Rank}",
            $"状态:{State}",
            $"神器:{Artifact.Name}",
            $"天赋:{PetTalent}",
            $"战力:{Power.ToFormat()}",
            $"智力:{Intellect.ToFormat()}",
            $"攻击:{Attack.ToFormat()}",
            $"防御:{Defense.ToFormat()}"
        ];

        int n2 = 20;
        foreach (var abText in abTexts2)
        {
            graphics.DrawString("◆" + abText, YaHei, Black, 395, n2);
            n2 += 35;
        }

        return image;
    }
}