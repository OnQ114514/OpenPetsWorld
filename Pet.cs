using Mirai.Net.Data.Messages.Receivers;
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
    public string IconName;
    public string PettAlent = "无";
    public int Intellect = 4;
    public int Attack = 10;
    public int Defense = 10;
    public Artifact Artifact = Artifact.Null;
    public int Mood = 50;

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

    public int Power => (Attack + Defense + MaxHealth) / 10 + Intellect * 20;

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
            receiver.SendAtMessage("你的宠物还未佩戴神器！");
            return false;
        }
        Player player = Player.Register(receiver);
        player.Bag.MergeValue(Artifact.Id, 1);
        Artifact = Artifact.Null;

        return true;
    }
}