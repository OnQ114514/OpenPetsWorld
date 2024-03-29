using System.Drawing;
using Newtonsoft.Json;
using OpenPetsWorld.Item;
using static OpenPetsWorld.Program;
using static OpenPetsWorld.OpenPetsWorld;

namespace OpenPetsWorld;

public class Shop
{
    [JsonIgnore] private readonly int _pagesCount;
    public string Command = "";
    public string Name = "";
    public Dictionary<int, int> Commodities = new();

    public int this[int i] => Commodities[i];

    public Shop()
    {
        _pagesCount = (int)Math.Ceiling((double)Commodities.Count / 10);
    }

    public Bitmap Render(int count)
    {
        List<string> commList = Commodities.Select(commodity =>
        {
            BaseItem item = Items[commodity.Key];
            int price = commodity.Value;
            string type = item.ItemType.ToStr();
            return $"[{type}]·{item.Name} {price}";
        }).ToList().SafeGetRange(count - 1, 10);
        
        Bitmap bitmap = new(480, 600);
        using var graphics = Graphics.FromImage(bitmap);
        using Font font = new("微软雅黑", 22);
        using Pen pen = new(Color.Black, 3);
        graphics.Clear(Color.White);
        graphics.DrawString("宠物商店", font, Black, 10, 5);
        graphics.DrawLine(pen, 130, 22, 480, 22);
        graphics.DrawLine(pen, 0, 465, 275, 465);
        graphics.DrawString($"页数：{count}/{_pagesCount}", font, Black, 280, 445);
        graphics.DrawString($"◇指令：{Name}+页数", font, Black, 5, 480);
        graphics.DrawString($"◇指令：{Command}+物品*数量", font, Black, 5, 520);
                    
        int coord = 45;
        foreach (var comm in commList)
        {
            graphics.DrawString(comm, font, Black, 5, coord);
            coord += 40;
        }

        return bitmap;
    }
}