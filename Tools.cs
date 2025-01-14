﻿using SkiaSharp;
using Sora.Entities;
using Sora.EventArgs.SoraEvent;
using static OpenPetsWorld.Program;

namespace OpenPetsWorld
{
    internal static class Tools
    {
        public static async Task SendAtMessage(this GroupMessageEventArgs x, string text)
        {
            await x.Reply(new MessageBodyBuilder().At(x.Sender.Id).Plain($" {text}").Build());
        }

        public static async Task SendBmpMessage(this GroupMessageEventArgs x, SKImage image)
        {
            await x.Reply(new MessageBodyBuilder().Image(image).Build());
        }
        
        public static async Task SendBmpMessage(this GroupMessageEventArgs x, SKBitmap image)
        {
            await x.Reply(new MessageBodyBuilder().Image(image).Build());
        }

        public static List<T> SafeGetRange<T>(this List<T> list, int index, int count)
        {
            var availableCount = list.Count - index;
            var rangeCount = Math.Min(count, availableCount);

            return rangeCount <= 0
                ?
                // 如果范围中的元素数小于等于0，则返回一个空列表
                []
                : list.GetRange(index, rangeCount);
        }

        public static long ToUnixTime(this DateTime dateTime)
        {
            return new DateTimeOffset(dateTime).ToUnixTimeSeconds();
        }

        public static void MergeValue<T>(this Dictionary<T, long> dictionary, T key, long value) where T : notnull
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

        public static string ToFormat(this long v)
        {
            const long trillion = 1000000000000;
            const long hundredMillion = 100000000;
            const long tenThousand = 10000;
            decimal origin = v;

            switch (v)
            {
                case >= trillion:
                    origin /= hundredMillion;
                    return Math.Round(origin, 1, MidpointRounding.ToZero) + "WE";
                case >= hundredMillion:
                    origin /= hundredMillion;
                    return Math.Round(origin, 1, MidpointRounding.ToZero) + "E";
                case >= tenThousand:
                    origin /= tenThousand;
                    return Math.Round(origin, 1, MidpointRounding.ToZero) + "W";
                default:
                    return v.ToString();
            }
        }

        public static string ToSignedString(this long value)
        {
            if (value > 0)
            {
                return "+" + value;
            }

            return value.ToString();
        }

        public static int GetCount(this string v, string symbol = "*")
        {
            if (v == string.Empty)
            {
                return -1;
            }

            var value = v[v.Length..];
            if (value.Contains(symbol))
            {
                if (int.TryParse(v[(v.Length + symbol.Length)..], out int count))
                {
                    return count;
                }

                return 0;
            }

            return -1;
        }

        //TODO:使用更好的跳过策略而不是硬编码skipCount
        public static void ParseString(MessageContext message, int skipCount, out string name, out long count,
            out long? target)
        {
            var text = message.GetText()[skipCount..];
            name = string.Empty;
            count = 1;
            target = null;

            var countIndex = text.IndexOf('*');
            var targetIndex = text.IndexOf('-');

            // 提取名称
            name = countIndex != -1 ? text[..countIndex] : targetIndex != -1 ? text[..targetIndex] : text;

            // 提取数量
            if (countIndex != -1)
            {
                var countText = text[(countIndex + 1)..(targetIndex != -1 ? targetIndex : text.Length)];
                if (countText.Trim().Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    count = -1;
                }
                else if (!long.TryParse(countText, out count))
                {
                    count = 1; // 默认值
                }
            }

            // 提取目标
            if (targetIndex == -1) return;
            
            var targetText = text[(targetIndex + 1)..].Trim();
            if (long.TryParse(targetText, out var targetNumber))
            {
                target = targetNumber;
            }
            else
            {
                target = GetAtNumber(message.MessageBody) ?? null;
            }
        }

        public static SKFont FontRegister(int size)
        {
            return FontRegister(size, SKFontStyle.Normal);
        }
        
        public static SKFont FontRegister(int size, SKFontStyle style)
        {
            var typeface = FontStyleSet.CreateTypeface(style);
            // SKFont和System.Drawing的Font的比例似乎不同，所以除以0.75
            var font = new SKFont(typeface, size / 0.75F);
            return font;
        }
    }
}