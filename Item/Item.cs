using Mirai.Net.Data.Messages.Receivers;
using OpenPetsWorld.Extra;
using static OpenPetsWorld.OpenPetsWorld;
using static OpenPetsWorld.Program;

namespace OpenPetsWorld.Item;

/// <summary>
/// 物品基类
/// </summary>
public class BaseItem
{
    public int Id = 0;

    /// <summary>
    /// 名称
    /// </summary>
    public string Name = "无";

    /// <summary>
    /// 类型
    /// </summary>
    public ItemType ItemType;

    /// <summary>
    /// 描述
    /// </summary>
    public string? description;

    /// <summary>
    /// 描述附加图片
    /// </summary>
    public string? descriptionImageName = null;

    /// <summary>
    /// 最低使用等级
    /// </summary>
    public int Level = 0;

    public Formulation? Formulation;

    public virtual bool Use(GroupMessageReceiver receiver, int count)
    {
        Player player = Player.Register(receiver);
        if (player.Bag[Id] < count)
        {
            receiver.SendAtMessage($"你的背包中【{Name}】不足{count}个！");
            return false;
        }

        if (Level > 0)
        {
            if (!HavePet(receiver, out Pet? petData))
            {
                return false;
            }

            if (petData.Level < Level)
            {
                receiver.SendAtMessage($"该道具最低使用等级[{Level}]！");
                return false;
            }
        }

        player.Bag[Id] -= count;

        return true;
    }

    public bool Make(GroupMessageReceiver receiver, int count)
    {
        if (Formulation == null)
        {
            receiver.SendAtMessage("此物品暂不支持合成！");
            return false;
        }

        Player player = Player.Register(receiver);
        foreach (var item in Formulation)
        {
            player.Bag.TryGetValue(item.Id, out int itemCount);

            int countNeeded = item.Count * count;
            if (itemCount < countNeeded)
            {
                receiver.SendAtMessage($"你的背包中如下道具：\n[{Items[item.Id]}]不足{countNeeded}个");
                return false;
            }
        }

        foreach (var item in Formulation)
        {
            player.Bag[item.Id] -= item.Count * count;
        }
        
        player.Bag.MergeValue(Id, count);
        return true;
    }
    
    public static FItem operator *(BaseItem item, int count)
    {
        return new FItem()
        {
            Id = item.Id,
            Count = count
        };
    }
}

public class Material : BaseItem
{
    public Material()
    {
        ItemType = ItemType.Material;
    }

    public override bool Use(GroupMessageReceiver receiver, int count)
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

    public static Artifact Null = new Artifact()
    {
        Id = -1,
        Name = "无"
    };

    public Artifact()
    {
        ItemType = ItemType.Artifact;
    }

    public override bool Use(GroupMessageReceiver receiver, int count)
    {
        if (!base.Use(receiver, 1)) return false;
        Player player = Player.Register(receiver);
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

    public override bool Use(GroupMessageReceiver receiver, int count)
    {
        if (!base.Use(receiver, count)) return false;
        Pet petData = Player.Register(receiver).Pet;
        if (petData.Health == 0)
        {
            int ResHealth;
            switch (Mode)
            {
                case 0:
                    ResHealth = petData.Health =
                        ((int)(petData.MaxHealth < Health
                            ? petData.MaxHealth
                            : Health) * count);
                    petData.RectOverflow();
                    break;
                case 1:
                    ResHealth = petData.Health = (int)Math.Round(petData.MaxHealth * Health * count);
                    petData.RectOverflow();
                    break;
                case 2:
                    ResHealth = petData.Health = petData.MaxHealth;
                    break;
                default:
                    throw new Exception($"恢复模式异常，模式为{Mode}，物品Id为{Id}");
            }

            receiver.SendAtMessage($"成功使用【{Name}】×1，将宠物成功复活!\n◇回复血量：{ResHealth}");
        }

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

    public override bool Use(GroupMessageReceiver receiver, int count)
    {
        if (!base.Use(receiver, count)) return false;
        Pet pet = Player.Register(receiver).Pet;
        if (pet.Health == 0)
        {
            receiver.SendAtMessage("您的宠物已死亡，请先进行复活！");
            return false;
        }

        if (pet.Health < pet.MaxHealth)
        {
            int OriginHealth = pet.Health;
            int ResHealth;
            switch (Mode)
            {
                case 0:
                    pet.Health += (int)Health * count;
                    pet.RectOverflow();
                    ResHealth = pet.Health - OriginHealth;
                    break;
                case 1:
                    pet.Health += (int)Math.Round(pet.MaxHealth * Health) * count;
                    ResHealth = pet.Health - OriginHealth;
                    break;
                case 2:
                    pet.Health = pet.MaxHealth;
                    ResHealth = pet.MaxHealth - OriginHealth;
                    count = 1;
                    break;
                default:
                    Log.Error($"恢复模式异常，模式为{Mode}，物品Id为{Id}");
                    return false;
            }

            receiver.SendAtMessage($"成功使用【{Name}】×{count}，将宠物成功复活!\n◇回复血量：{ResHealth}");
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
    public int Attack = 0;
    public int Defense = 0;
    public int Intellect = 0;
    public int Health = 0;

    public Gain()
    {
        ItemType = ItemType.Gain;
    }

    public override bool Use(GroupMessageReceiver receiver, int count)
    {
        if (base.Use(receiver, count))
        {
            Pet pet = Player.Register(receiver).Pet;
            List<string> message = new()
            {
                $"成功使用[{Name}] ×{count}，触发以下效果："
            };
            if (Attack != 0)
            {
                message.Add($"◇攻击永久提升：{Attack}");
            }

            if (Defense != 0)
            {
                message.Add($"◇防御永久提升：{Defense}");
            }

            if (Intellect != 0)
            {
                message.Add($"◇智力永久提升：{Intellect}");
            }

            if (Health != 0)
            {
                message.Add($"◇生命永久提升：{Health}");
            }
            pet.Attack += Attack;
            pet.Defense += Defense;
            pet.Intellect += Intellect;
            pet.Health += Health;
            receiver.SendAtMessage(string.Join("\n", message));
            return true;
        }

        return false;
    }
}