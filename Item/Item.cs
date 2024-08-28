using Newtonsoft.Json;
using OpenPetsWorld.PetTool;
using Sora.EventArgs.SoraEvent;
using YukariToolBox.LightLog;
using static OpenPetsWorld.Game;

namespace OpenPetsWorld.Item;

/// <summary>
/// 物品基类
/// </summary>
public class BaseItem
{
    /// <summary>
    /// 名称
    /// </summary>
    public string Name = "无";

    /// <summary>
    /// 类型
    /// </summary>
    public ItemType ItemType;

    private readonly string? _description;

    /// <summary>
    /// 描述
    /// </summary>
    [JsonIgnore]
    public string Description
    {
        get
        {
            var text = _description ?? "无该物品描述";
            return text;
        }
    }

    /// <summary>
    /// 描述附加图片
    /// </summary>
    public string? DescriptionImageName = null;

    /// <summary>
    /// 最低使用等级
    /// </summary>
    protected readonly int Level = 0;

    /// <summary>
    /// 配方
    /// </summary>
    public Formulation? Formulation;

    /// <summary>
    /// 可交易
    /// </summary>
    public bool CanTrade = true;

    public virtual bool Use(GroupMessageEventArgs eventArgs, int count)
    {
        if (count == 0)
        {
            eventArgs.SendAtMessage($"你的背包中没有【{Name}】");
        }
        
        var player = Player.Register(eventArgs);
        if (player.Bag[Name] < count)
        {
            eventArgs.SendAtMessage($"你的背包中【{Name}】不足{count}个！");
            return false;
        }

        if (Level > 0) 
        {
            if (!HavePet(eventArgs, out var petData))
            {
                return false;
            }

            if (petData.Level < Level)
            {
                eventArgs.SendAtMessage($"该道具最低使用等级[{Level}]！");
                return false;
            }
        }

        player.Bag[Name] -= count;

        return true;
    }

    public bool Make(GroupMessageEventArgs receiver, int count)
    {
        if (Formulation == null)
        {
            receiver.SendAtMessage("此物品暂不支持合成！");
            return false;
        }

        var player = Player.Register(receiver);
        foreach (var item in Formulation.Items)
        {
            player.Bag.TryGetValue(item.Name, out var itemCount);

            var countNeeded = item.Count * count;
            if (itemCount >= countNeeded) continue;

            receiver.SendAtMessage($"你的背包中如下道具：\n[{Items[item.Name].Name}]不足{countNeeded}个");
            return false;
        }

        foreach (var item in Formulation.Items)
        {
            player.Bag[item.Name] -= item.Count * count;
        }

        player.Bag.MergeValue(Name, count);
        return true;
    }

    public static FItem operator *(BaseItem item, int count)
    {
        return new FItem
        {
            Name = item.Name,
            Count = count
        };
    }
}

/// <summary>
/// 材料
/// </summary>
public class Material : BaseItem
{
    public Material()
    {
        ItemType = ItemType.Material;
    }

    public override bool Use(GroupMessageEventArgs receiver, int count)
    {
        receiver.SendAtMessage("该道具不能直接使用，请更换道具！");
        return false;
    }
}

/// <summary>
/// 神器
/// </summary>
public class Artifact : BaseItem
{
    public int Attack = 0;
    public int Defense = 0;
    public int Energy = 0;
    public int Intellect = 0;
    public int Health = 0;

    public static Artifact Null = new()
    {
        Name = "无"
    };

    public Artifact()
    {
        ItemType = ItemType.Artifact;
    }

    public override bool Use(GroupMessageEventArgs receiver, int count)
    {
        if (!base.Use(receiver, 1)) return false;
        var player = Player.Register(receiver);
        player.Pet.Artifact = this;
        List<string> message = new()
        {
            $"您的[{player.Pet.Name}]戴着神器真是威风凌凌呢!",
            $"● 神器名称：{Name}",
            $"● 佩戴等级：LV·{Level}"
        };
        if (Health != 0) message.Add($"● 生命提升{Health.ToSignedString()}");
        if (Attack != 0) message.Add($"● 攻击提升{Attack.ToSignedString()}");
        if (Defense != 0) message.Add($"● 防御提升{Defense.ToSignedString()}");
        if (Intellect != 0) message.Add($"● 智力提升{Intellect.ToSignedString()}");
        if (Energy != 0) message.Add($"● 精力提升{Energy.ToSignedString()}");
        receiver.SendAtMessage(string.Join("\n", message));
        return true;
    }
}

/// <summary>
/// 复活
/// </summary>
public class Resurrection : BaseItem
{
    /// <summary>
    /// 0为回复至某值，1为回复到上限的百分之几，2回满
    /// </summary>
    public int Mode = 0;

    public double Health = 0;

    public Resurrection()
    {
        ItemType = ItemType.Resurrection;
    }

    public override bool Use(GroupMessageEventArgs receiver, int count)
    {
        if (!base.Use(receiver, count)) return false;
        var petData = Player.Register(receiver).Pet;
        if (petData.Health != 0) return true;

        long resHealth;
        switch (Mode)
        {
            case 0:
                resHealth = petData.Health =
                    ((long)(petData.MaxHealth < Health
                        ? petData.MaxHealth
                        : Health) * count);
                petData.RectOverflow();
                break;
            case 1:
                resHealth = petData.Health = (int)Math.Round(petData.MaxHealth * Health * count);
                petData.RectOverflow();
                break;
            case 2:
                resHealth = petData.Health = petData.MaxHealth;
                break;
            default:
                Log.Error("ItemUse", $"恢复模式异常，模式为{Mode}，物品名为{Name}");
                return false;
        }

        receiver.SendAtMessage($"成功使用【{Name}】×1，将宠物成功复活!\n◇回复血量：{resHealth}");

        return true;
    }
}

/// <summary>
/// 恢复
/// </summary>
public class Recovery : BaseItem
{
    public double Health = 0;

    /// <summary>
    /// 0增加，1为增加到上限的百分之几，2回满
    /// </summary>
    public int Mode = 0;

    public Recovery()
    {
        ItemType = ItemType.Recovery;
    }

    public override bool Use(GroupMessageEventArgs receiver, int count)
    {
        if (!base.Use(receiver, count)) return false;
        var pet = Player.Register(receiver).Pet;
        if (pet.Health == 0)
        {
            receiver.SendAtMessage("您的宠物已死亡，请先进行复活！");
            return false;
        }

        if (pet.Health < pet.MaxHealth)
        {
            var originHealth = pet.Health;
            long resHealth;
            switch (Mode)
            {
                case 0:
                    pet.Health += (long)Health * count;
                    pet.RectOverflow();
                    resHealth = pet.Health - originHealth;
                    break;
                case 1:
                    pet.Health += (long)Math.Round(pet.MaxHealth * Health) * count;
                    resHealth = pet.Health - originHealth;
                    break;
                case 2:
                    pet.Health = pet.MaxHealth;
                    resHealth = pet.MaxHealth - originHealth;
                    count = 1;
                    break;
                default:
                    Log.Error("ItemUse", $"恢复模式异常，模式为{Mode}，物品名为{Name}");
                    return false;
            }

            receiver.SendAtMessage($"成功使用【{Name}】×{count}，将宠物成功复活!\n◇回复血量：{resHealth}");
            return true;
        }

        receiver.SendAtMessage("你的宠物当前不需要恢复生命！");
        return false;
    }
}

/// <summary>
/// 增益
/// </summary>
public class Gain : BaseItem
{
    public long Attack = 0;
    public long Defense = 0;
    public long Intellect = 0;
    public long Health = 0;
    public long Points = 0;
    public long Experience = 0;
    public long MaxEnergy = 0;

    public Gain()
    {
        ItemType = ItemType.Gain;
    }

    public override bool Use(GroupMessageEventArgs receiver, int count)
    {
        if (!base.Use(receiver, count)) return false;

        var player = Player.Register(receiver);
        var pet = player.Pet;

        List<string> message = [$"成功使用[{Name}] ×{count}，触发以下效果："];

        if (Attack != 0) message.Add($"◇攻击永久提升：{Attack * count}");
        if (Defense != 0) message.Add($"◇防御永久提升：{Defense * count}");
        if (Intellect != 0) message.Add($"◇智力永久提升：{Intellect * count}");
        if (Health != 0) message.Add($"◇生命永久提升：{Health * count}");
        if (Health != 0) message.Add($"◇精力永久提升：{MaxEnergy * count}");
        if (Experience != 0) message.Add($"◇获得经验：{Experience * count}");
        if (Points != 0) message.Add($"◇获得积分：{Points * count}");

        pet.BaseAttack += Attack * count;
        pet.BaseDefense += Defense * count;
        pet.BaseIntellect += Intellect * count;
        pet.BaseMaxHealth += Health * count;
        pet.Experience += Experience * count;
        pet.BaseMaxEnergy += MaxEnergy;

        player.Points += Points * count;

        receiver.SendAtMessage(string.Join("\n", message));
        return true;

    }
}

/// <summary>
/// 宠物
/// </summary>
public class PetItem : BaseItem
{
    private Pet _pet;

    public override bool Use(GroupMessageEventArgs receiver, int count)
    {
        var player = Player.Register(receiver);
        if (player.Pet != null) return false;

        player.Pet = _pet;
        return true;

    }
}