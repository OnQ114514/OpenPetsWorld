using OpenPetsWorld.Item;
using static OpenPetsWorld.OpenPetsWorld;
using static OpenPetsWorld.Program;

namespace OpenPetsWorld;

public class Pet
{
    public int Energy = 100;
    public int Health;
    public int Experience;
    public double MaxEnergy = 100;
    public int MaxHealth;
    public int MaxExperience = 160;
    public int Level = 1;
    public string Name;
    public string Gender;
    public string Stage = "幼年期";
    public string Attribute;
    public string Rank;
    public string State = "正常";
    public string iconName;
    public string PettAlent = "无";
    public int Intellect = 4;
    public int Attack = 10;
    public int Defense = 10;
    public Artifact? artifact = null;
    public int Mood = 50;

    public Pet()
    {
        //示例宠物
        iconName = "kiana.jpg";
        MaxHealth = random.Next(100, 301);
        Health = MaxHealth;
        Name = "test";

        #region 性别随机

        Gender = RandomBool() ? "雌" : "雄";

        #endregion

        #region 级别随机

        Rank = Ranks[random.Next(0, 4)];

        #endregion

        #region 属性随机

        Attribute = Attributes[random.Next(0, 5)];

        #endregion
    }

    public int GetPower()
    {
        return (Attack + Defense + MaxHealth) / 10 + Intellect * 20;
    }

    public string GetMoodSymbol()
    {
        string Star = string.Empty;
        int StarNumber = (int)Math.Round((double)Mood / 10);
        for (int i = 0; i < StarNumber; i++)
        {
            Star += "★";
        }

        return Star;
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

    public static Pet Extract()
    {
        int index = random.Next(0, PetPool.Count);
        return PetPool[index];
    }

    public int Damage(Pet myPet)
    {
        return (myPet.Attack + myPet.Intellect * 20) *
               (1 - (Defense * Intellect * 20) / (Attack + Defense + Health / 10 + Intellect * 20));
    }
}