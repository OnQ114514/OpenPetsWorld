using System.Diagnostics;
using Newtonsoft.Json;
using OpenPetsWorld.Item;
using SkiaSharp;
using static OpenPetsWorld.Game;

namespace OpenPetsWorld;

public class Shop
{
    [JsonIgnore] private readonly int _pagesCount;
    public string Command = "";
    public string Name = "";
    public Dictionary<string, long> Commodities = new();

    public long this[string i] => Commodities[i];

    public Shop()
    {
        _pagesCount = (int)Math.Ceiling((double)Commodities.Count / 10);
    }

    public SKImage Render(int count)
    {
        var commList = Commodities.ToList()
            .SafeGetRange(count - 1, 10)
            .ConvertAll(commodity =>
            {
                var item = Items[commodity.Key];
                var price = commodity.Value;
                var type = item.ItemType.ToStr();
                return $"[{type}]·{item.Name} {price}";
            });
        
        using var surface = SKSurface.Create(new SKImageInfo(480, 600));
        using var canvas = surface.Canvas;

        using var font = Tools.FontRegister(23);
        using var paint = new SKPaint();
        paint.StrokeWidth = 3;
        paint.Color = SKColors.Black;
        paint.IsAntialias = true;

        canvas.Clear(SKColors.White);
        canvas.DrawText("宠物商店", 10, 30, font, paint);
        canvas.DrawLine(135, 30, 480, 30, paint);
        canvas.DrawLine(0, 465, 275, 465, paint);
        canvas.DrawText($"页数：{count}/{_pagesCount}", 280, 465, font, paint);
        canvas.DrawText($"◇指令：{Name}+页数", 5, 510, font, paint);
        canvas.DrawText($"◇指令：{Command}+物品*数量", 5, 550, font, paint);

        var y = 80;
        foreach (var comm in commList)
        {
            canvas.DrawText(comm, 5, y, font, paint);
            y += 40;
        }

        return surface.Snapshot();
    }
}