using Mirai.Net.Data.Messages;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Utils.Scaffolds;
using System.Drawing;
using static OpenPetsWorld.Program;

namespace OpenPetsWorld
{
    internal static class Tools
    {
        public static void ClearText(this Graphics v)
        {
            v.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        }

        public static void SendAtMessage(this GroupMessageReceiver x, string text)
        {
            x.SendMessageAsync(new MessageChainBuilder().At(x.Sender.Id).Plain($" {text}").Build());
        }

        public static void SendBmpMessage(this GroupMessageReceiver x, Image image)
        {
            x.SendMessageAsync(new MessageChainBuilder().ImageFromBase64(ToBase64(image)).Build());
        }

        public static List<T> SafeGetRange<T>(this List<T> list, int index, int count)
        {
            var availableCount = list.Count - index;
            var rangeCount = Math.Min(count, availableCount);

            return rangeCount <= 0 ?
                // 如果范围中的元素数小于等于0，则返回一个空列表
                new List<T>() : list.GetRange(index, rangeCount);
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
            //const string format = "0.0";
            decimal origin = v;

            if (v >= hundredMillion)
            {
                origin /= hundredMillion;
                return Math.Round(origin, 1, MidpointRounding.ToZero) + "E";
                //return (origin / hundredMillion).ToString(format) + "E";
            }
            else if (v >= tenThousand)
            {
                origin /= tenThousand;
                return Math.Round(origin, 1, MidpointRounding.ToZero) + "W";
                //return (origin / tenThousand).ToString(format) + "W";
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

        public static void ParseString(MessageChain chain, int skipCount, out string name, out int count, out string? target)
        {
            string plainMessage = chain.GetPlainMessage()[skipCount..];
            name = string.Empty;
            count = 1;
            target = null;

            int countIndex = plainMessage.IndexOf('*');
            int targetIndex = plainMessage.IndexOf('-');

            // 提取名称
            if (countIndex != -1)
            {
                name = plainMessage[..countIndex];
            }
            else if (targetIndex != -1)
            {
                name = plainMessage[..targetIndex];
            }
            else
            {
                name = plainMessage;
            }

            // 提取数量
            if (countIndex != -1)
            {
                try
                {
                    var countText = plainMessage[(countIndex + 1)..(targetIndex != -1 ? targetIndex : plainMessage.Length)];
                    int.TryParse(countText, out count);
                }
                catch
                {
                    // ignored
                }
            }

            // 提取目标
            if (targetIndex != -1)
            {
                try
                {
                    var targetText = plainMessage[(targetIndex + 1)..];
                    if (long.TryParse(targetText, out long targetNumber))
                    {
                        target = targetNumber.ToString();
                    }
                    else
                    {
                        var number = GetAtNumber(chain);
                        if (number != null)
                        {
                            target = number;
                        }
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}