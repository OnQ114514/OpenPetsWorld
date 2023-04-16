using System.Drawing;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Utils.Scaffolds;

namespace OpenPetsWorld
{
    internal static class Tools
    {
        public static void Fill(this Graphics v, Color color, Image image)
        {
            v.FillRectangle(new SolidBrush(color), new Rectangle(0, 0, image.Width, image.Height));
        }

        public static void Fill(this Graphics v, Brush brush, Image image)
        {
            v.FillRectangle(brush, new Rectangle(0, 0, image.Width, image.Height));
        }

        public static void ClearText(this Graphics v) 
        {
            v.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        }

        public static void SendAtMessage(this GroupMessageReceiver x, string Text)
        {
            x.SendMessageAsync(new MessageChainBuilder().
                At(x.Sender.Id).
                Plain($" {Text}").
                Build());
        }
    }
}
