using System.Drawing;
using Mirai.Net.Data.Messages;
using static OpenPetsWorld.Program;
using static OpenPetsWorld.OpenPetsWorld;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Utils.Scaffolds;
using OpenPetsWorld.Item;

namespace OpenPetsWorld
{
    internal static class Tools
    {
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
            int availableCount = list.Count - index;
            int rangeCount = Math.Min(count, availableCount);

            if (rangeCount <= 0)
            {
                // 如果范围中的元素数小于等于0，则返回一个空列表
                return new List<T>();
            }

            return list.GetRange(index, rangeCount);
        }

        public static long ToUnixTime(this DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
        }

        public static void MergeValue<T>(this Dictionary<T, int> dictionary, T key, int value) where T : notnull
        {
            if (!dictionary.TryAdd(key, value))
            {
                dictionary[key] += value;
            }
        }

        /*public static int GetDayCount()
        {
            DateTime time = DateTime.Now;
            int day = DateTime.DaysInMonth(time.Year, time.Month);
            return day;
        }*/

        public static string ToFormat(this int v)
        {
            const int hundredMillion = 100000000;
            const int tenThousand = 10000;            
            const string format = "0.0";
            decimal origin = v;
            
            if (v >= hundredMillion)
            {
                return (origin / hundredMillion).ToString(format) + "E";
            }
            else if (v >= tenThousand)
            {
                return (origin / tenThousand).ToString(format) + "W";
            }
            else
            {
                return v.ToString();
            }
        }
        
        public static string ToSignedString(this int value)
        {
            if (value > 0)
            {
                return "+" + value.ToString();
            }

            return value.ToString();
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

        public static int GetICT(this MessageChain v, int skipCount, ref string item, out string? target)
        {
            target = null;
            string symbol = "*";
            var str = v.GetPlainMessage()[skipCount..];
            int result;
            int targetIndex = str.IndexOf("-", StringComparison.Ordinal);
            if (targetIndex != -1)
            {
                result = str.GetCount(symbol[..targetIndex]);
                var succeed = long.TryParse(str[(targetIndex + 1)..], out long number);
                target = succeed ? number.ToString() : GetAtNumber(v);
            }
            else
            {
                result = str.GetCount(symbol);
            }

            int countIndex = str.IndexOf(symbol, StringComparison.Ordinal);
            item = countIndex != -1 ? str[..countIndex] : str;

            if (str == string.Empty && result == -1)
            {
                return -1;
            }
            
            return 1;

            /*if (v == string.Empty)
            {
                return -1;
            }

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

            return -1;*/
        }
    }
}