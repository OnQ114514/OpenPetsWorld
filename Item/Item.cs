using Newtonsoft.Json;
using OpenPetsWorld.PetTool;
using Sora.EventArgs.SoraEvent;
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
    /// 是否可交易
    /// </summary>
    public bool CanTrade = true;

    /// <summary>
    /// 出售价格
    /// </summary>
    public long? Price = null;

    /// <summary>
    /// 是否可对他人使用
    /// </summary>
    public bool CanUseToOther = false;

    public virtual bool UseToOther(GroupMessageEventArgs eventArgs, long count, long target)
    {
        if (CanUseToOther) return true;
        
        eventArgs.SendAtMessage("无法对其使用此类道具！");
        return false;
    }

    public virtual bool Use(GroupMessageEventArgs eventArgs, long count)
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

    public bool Make(GroupMessageEventArgs receiver, long count)
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

    public override bool Use(GroupMessageEventArgs receiver, long count)
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
    public long Attack = 0;
    public long Defense = 0;
    public long Energy = 0;
    public long Intellect = 0;
    public long Health = 0;

    public static Artifact Null = new()
    {
        Name = "无"
    };

    public Artifact()
    {
        ItemType = ItemType.Artifact;
    }

    public override bool Use(GroupMessageEventArgs receiver, long count)
    {
        if (!base.Use(receiver, 1)) return false;
        var player = Player.Register(receiver);
        player.Pet.Artifact = this;
        List<string> message =
        [
            $"您的[{player.Pet.Name}]戴着神器真是威风凌凌呢!",
            $"● 神器名称：{Name}",
            $"● 佩戴等级：LV·{Level}"
        ];
        
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
    public long Energy = 0;
    public long Mood = 0;
    public double Health = 0;

    public Resurrection()
    {
        ItemType = ItemType.Resurrection;
    }

    public override bool Use(GroupMessageEventArgs receiver, long count)
    {
        if (!base.Use(receiver, count)) return false;
        var pet = Player.Register(receiver).Pet;
        if (pet.Health != 0)
        {
            receiver.SendAtMessage("你的宠物没有死亡，无需复活！");
            return false;
        }

        var builder = new MessageBodyBuilder();
        builder
            .At(receiver.Sender.Id)
            .Plain($" 成功使用【{Name}】×{count}，将宠物成功复活！\n");

        var resHealth = Recovery.GetRecoveryValue(Health * count, ref pet.Health, pet.MaxHealth);
        var resEnergy = Recovery.GetRecoveryValue(Energy * count, ref pet.Energy, pet.MaxEnergy);
        var resMood = Recovery.GetRecoveryValue(Mood * count, ref pet.Mood, PlayConfig.MaxMood);

        if (resHealth != 0) builder.Plain($"◇回复血量：{resHealth}\n");
        if (resEnergy != 0) builder.Plain($"◇回复精力：{resEnergy}\n");
        if (resMood != 0) builder.Plain($"◇回复心情：{resMood}\n");

        if (resHealth == 0 && resEnergy == 0 && resMood == 0)
        {
            receiver.SendAtMessage("你的宠物当前不需要恢复！");
            return false;
        }

        receiver.Reply(builder.Build());
        return true;
    }

    public override bool UseToOther(GroupMessageEventArgs eventArgs, long count, long target)
    {
        if (!base.UseToOther(eventArgs, count, target)) return false;

        return true;
    }
}

/// <summary>
/// 恢复
/// </summary>
public class Recovery : BaseItem
{
    // x>=1增加，0<x<1为增加到上限的百分之几，x=-1回满
    public long Energy = 0;
    public long Mood = 0;
    public double Health = 0;

    public Recovery()
    {
        ItemType = ItemType.Recovery;
    }

    public override bool Use(GroupMessageEventArgs receiver, long count)
    {
        if (!base.Use(receiver, count)) return false;
        var pet = Player.Register(receiver).Pet;
        if (pet.Health == 0)
        {
            receiver.SendAtMessage("您的宠物已死亡，请先进行复活！");
            return false;
        }

        var builder = new MessageBodyBuilder();
        builder
            .At(receiver.Sender.Id)
            .Plain($" 成功使用【{Name}】×{count}，触发以下效果：\n");

        var recHealth = GetRecoveryValue(Health * count, ref pet.Health, pet.MaxHealth);
        var recEnergy = GetRecoveryValue(Energy * count, ref pet.Energy, pet.MaxEnergy);
        var recMood = GetRecoveryValue(Mood * count, ref pet.Mood, PlayConfig.MaxMood);

        if (recHealth != 0) builder.Plain($"◇回复血量：{recHealth}\n");
        if (recEnergy != 0) builder.Plain($"◇回复精力：{recEnergy}\n");
        if (recMood != 0) builder.Plain($"◇回复心情：{recMood}\n");

        if (recHealth == 0 && recEnergy == 0 && recMood == 0)
        {
            receiver.SendAtMessage("你的宠物当前不需要恢复！");
            return false;
        }

        receiver.Reply(builder.Build());
        return true;
    }
    
    public override bool UseToOther(GroupMessageEventArgs eventArgs, long count, long target)
    {
        if (!base.UseToOther(eventArgs, count, target)) return false;
        
        return true;
    }

    internal static long GetRecoveryValue(double value, ref long currentValue, long maxValue)
    {
        var originValue = currentValue;

        switch (value)
        {
            case >= 1:
                currentValue += (long)value;
                break;
            case > 0 and < 1:
                currentValue += (long)Math.Round(maxValue * value);
                break;
            case -1:
                currentValue = maxValue;
                break;
        }

        currentValue = Math.Min(currentValue, maxValue);
        return currentValue - originValue;
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

    public override bool Use(GroupMessageEventArgs receiver, long count)
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
    
    public override bool UseToOther(GroupMessageEventArgs eventArgs, long count, long target)
    {
        if (!base.UseToOther(eventArgs, count, target)) return false;
        
        return true;
    }
}

/// <summary>
/// 宠物
/// </summary>
public class PetItem : BaseItem
{
    private Pet _pet;

    public override bool Use(GroupMessageEventArgs receiver, long count)
    {
        var player = Player.Register(receiver);
        if (player.Pet != null) return false;

        player.Pet = _pet;
        return true;
    }
}

public class Clear : BaseItem
{
    
}