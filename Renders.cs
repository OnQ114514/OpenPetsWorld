using SkiaSharp;

namespace OpenPetsWorld;

public static class Renders
{
    public static SKImage BagRender(List<string> items, string senderId)
    {
        var height = items.Count * 38 + 110;
        using var surface = SKSurface.Create(new SKImageInfo(480, height));
        using var canvas = surface.Canvas;

        using var font = Tools.FontRegister(23);
        // 字体画笔
        using var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.Color = SKColors.Black;
        paint.StrokeWidth = 3;

        canvas.Clear(SKColors.White);
        canvas.DrawText($"[@{senderId}]您的背包：", 2, 32, font, paint);
        canvas.DrawLine(0, 55, 480, 55, paint);
        var endY = height - 30;
        canvas.DrawLine(0, endY, 480, endY, paint);

        var i = 95;
        foreach (var item in items)
        {
            canvas.DrawText(item, 5, i, font, paint);
            i += 38;
        }

        return surface.Snapshot();
    }

    public static async Task<SKImage> SignRender(long points, int signedDays, int continuousSignedDays,
        string senderName, long senderId)
    {
        using var surface = SKSurface.Create(new SKImageInfo(230, 90));
        using var canvas = surface.Canvas;
        // 清空背景为白色
        canvas.Clear(SKColors.White);
        // 获取头像流
        await using var stream =
            await Program.HttpClient.GetStreamAsync($"https://q2.qlogo.cn/headimg_dl?dst_uin={senderId}&spec=100");
        // 从流中加载头像
        using var headImg = SKBitmap.Decode(stream);
        canvas.DrawBitmap(headImg, new SKRect(0, 0, 90, 90));
        using var paint = new SKPaint();
        paint.Color = SKColors.Black;
        paint.IsAntialias = true;
        // 绘制发送者名称
        canvas.DrawText(senderName, new SKPoint(95, 20), Tools.FontRegister(15, SKFontStyle.Bold), paint);

        // 签到信息
        string[] signTexts =
        [
            $"奖励积分：{points}",
            $"累签天数：{signedDays}",
            $"连签天数：{continuousSignedDays}/31"
        ];

        var n = 42;
        var font = Tools.FontRegister(13);
        foreach (var text in signTexts)
        {
            canvas.DrawText(text, 93, n, font, paint);
            n += 20;
        }

        return surface.Snapshot();
    }

    public static SKImage AssetRender(long points, long bonds, string senderName)
    {
        using var surface = SKSurface.Create(new SKImageInfo(480, 235));
        using var canvas = surface.Canvas;
        // 清空背景为白色
        canvas.Clear(SKColors.White);

        using var paint = new SKPaint();
        paint.Color = SKColors.Black;
        paint.IsAntialias = true;
        paint.StrokeWidth = 3;

        // 绘制发送者名称和财富信息
        canvas.DrawText($"@{senderName} 财富信息如下：", 2, 30, Tools.FontRegister(23, SKFontStyle.Bold), paint);
        // 绘制分割线
        canvas.DrawLine(0, 55, 480, 55, paint);
        canvas.DrawLine(0, 180, 480, 180, paint);

        using var font = Tools.FontRegister(23);
        // 绘制积分和点券信息
        canvas.DrawText($"●积分：{points}", 0, 97, font, paint);
        canvas.DrawText($"●点券：{bonds}", 0, 157, font, paint);

        return surface.Snapshot();
    }

    public static SKImage InstanceListRender(List<string> instances, int index, int maxIndex)
    {
        using var surface = SKSurface.Create(new SKImageInfo(480, 640));
        using var canvas = surface.Canvas;

        using var font = Tools.FontRegister(23);
        using var paint = new SKPaint();
        paint.Color = SKColors.Black;
        paint.IsAntialias = true;
        paint.StrokeWidth = 3;

        canvas.Clear(SKColors.White);
        canvas.DrawText("当前开放副本如下：", 5, 38, font, paint);
        canvas.DrawText($"◆ 副本页数：{index}/{maxIndex}", 220, 510, font, paint);
        canvas.DrawText("◇指令：宠物副本+页数", 5, 550, font, paint);
        canvas.DrawText("◇指令：进入副本+副本名(*次数)", 5, 590, font, paint);
        canvas.DrawLine(0, 60, 480, 60, paint);
        canvas.DrawLine(0, 500, 215, 500, paint);

        var n = 100;
        foreach (var text in instances)
        {
            canvas.DrawText(text, 5, n, font, paint);
            n += 40;
        }

        return surface.Snapshot();
    }

    public static SKImage InstanceRender(string petName, string enemyName, InstanceResult result)
    {
        var height = (int)Math.Ceiling(result.Items.Count / 2D) * 40 + 155;
        if (result.Items.Count != 0) height += 50;

        using var surface = SKSurface.Create(new SKImageInfo(600, height));
        using var canvas = surface.Canvas;

        using var font = Tools.FontRegister(23);
        using var title = Tools.FontRegister(23, SKFontStyle.Bold);
        using var paint = new SKPaint();
        paint.Color = SKColors.Black;
        paint.IsAntialias = true;
        paint.StrokeWidth = 3;

        canvas.Clear(SKColors.White);
        canvas.DrawText($"[ {petName} VS {enemyName} ]", 300, 38, SKTextAlign.Center, title, paint);

        string[] left =
        [
            $"◆战斗结果：胜利",
            $"◆获得经验：{result.Experience}",
            $"◆获得积分：{result.Points}"
        ];

        string[] right =
        [
            $"◇消耗精力：{result.Energy}",
            $"◇血量减少：{result.Damage}",
            $"◇战后状态：{result.PetState}"
        ];
        var n = 78;
        for (var index = 0; index < left.Length; index++)
        {
            var text1 = left[index];
            var text2 = right[index];

            canvas.DrawText(text1, 15, n, font, paint);
            canvas.DrawText(text2, 305, n, font, paint);
            n += 35;
        }

        if (result.Items.Count == 0) return surface.Snapshot();

        #region 绘制物品

        canvas.DrawText("掉落物品", 300, 185, SKTextAlign.Center, title, paint);
        canvas.DrawLine(0, 175, 230, 175, paint);
        canvas.DrawLine(370, 175, 600, 175, paint);

        var strItems = result.Items.ConvertAll(x => $"{x.Name}*{x.Count}");

        var x = 220;
        for (var index = 0; index < strItems.Count; index++)
        {
            var item = strItems[index];

            if (index % 2 == 0)
            {
                canvas.DrawText(item, 15, x, font, paint);
                continue;
            }

            canvas.DrawText(item, 305, x, font, paint);
            x += 40;
        }

        #endregion

        return surface.Snapshot();
    }

    public static SKImage GachaTen(List<string> pets, string senderName)
    {
        using var surface = SKSurface.Create(new SKImageInfo(480, 480));
        using var canvas = surface.Canvas;
        
        using var font = Tools.FontRegister(23);
        using var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.Color = SKColors.Black;

        canvas.Clear(SKColors.White);
        canvas.DrawText($"[@{senderName}]", 3,3, font, paint);
        canvas.DrawText("◇指令：选择+数字", 3, 440, font, paint);

        var y = 40;
        foreach (var text in pets)
        {
            canvas.DrawText(text, 3, y, font, paint);
            y += 40;
        }

        return surface.Snapshot();
    }
}