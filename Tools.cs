using System.Drawing;
using static OpenPetsWorld.Program;
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
            x.SendMessageAsync(new MessageChainBuilder().At(x.Sender.Id).Plain($" {Text}").Build());
        }

        public static void SendBmpMessage(this GroupMessageReceiver x, Image image)
        {
            x.SendMessageAsync(new MessageChainBuilder().ImageFromBase64(ToBase64(image)).Build());
        }

        public static List<T> SafeGetRange<T>(this List<T> list, int index, int count)
        {
            int tryCount = list.Count - index;
            if (tryCount > count)
            {
                return list.GetRange(index, count);
            }

            return list.GetRange(index, tryCount);
        }

        public static int GetCount(this string v, string symbol = "*")
        {
            if (v == string.Empty)
            {
                return -1;
            }
            string value = v[v.Length..];
            if (value.Contains(symbol))
            {
                if (int.TryParse(v[(v.Length + symbol.Length)..], out int count))
                {
                    return count;
                }
                else
                {
                    return 0;
                }
            }

            return -1;
        }

        public static int GetCount(this string v, ref string item, string symbol = "*")
        {
            if (v == string.Empty)
            {
                return -1;
            }
            int index = v.IndexOf("*", StringComparison.Ordinal);
            if (index != -1)
            {
                if (int.TryParse(v[(index + 1)..], out int count))
                {
                    item = v[..index];
                    return count;
                }
                else
                {
                    return 0;
                }
            }

            return -1;
        }
    }
}