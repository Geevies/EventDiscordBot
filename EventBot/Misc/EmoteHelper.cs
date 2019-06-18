using Discord;
using System;

namespace EventBot.Services
{

    public static class EmoteHelper
    {
        //private IEnumerable<string> emoji;

        private static readonly int[] EmojiRanges =
        {
            0x1F600,0x1F64F, // Emoticons
            0x1F300,0x1F5FF, // Misc Symbols and Pictographs
            0x1F680,0x1F6FF, // Transport and Map
            0x1F1E6,0x1F1FF, // Regional country flags
            0x2600,0x26FF, // Misc symbols
            0x2700,0x27BF, // Dingbats
            0xE0020,0xE007F, // Tags
            0xFE00,0xFE0F, // Variation Selectors
            0x1F900,0x1F9FF, // Supplemental Symbols and Pictographs
            0x1F018,0x1F270, // Various asian characters
            0x238C,0x2454, // Misc items
            0x20D0,0x20FF // Combining Diacritical Marks for Symbols
        };

        public static bool TryParse(string input, out IEmote emote)
        {
            if(Emote.TryParse(input, out Emote parsedEmote))
            {
                emote = parsedEmote;
                return true;
            }
            if(isEmoji(input))
            {
                emote = new Emoji(input);
                return true;
            }
            emote = null;
            return false;
        }

        public static IEmote Parse(string input)
        {
            if (!TryParse(input, out IEmote parsed))
                throw new ArgumentException("Failed to parse emote.");
            return parsed;
        }

        public static bool isEmoji(string input)
        {
            if (input.Length % 2 == 1) {
                return false;
            }
            for (var i = 0; i < input.Length; i += 2)
            {
                if (!char.IsSurrogatePair(input[i], input[i + 1]))
                {
                    return false;
                }
                var utf32 = char.ConvertToUtf32(input[i], input[i + 1]);
                if (utf32 != 0x200D && !isEmojiChar(utf32))
                {
                    return false;
                }
            }
            return true;
        }


        public static bool isEmojiChar(int u32)
        {;
            for (int i = 0; i < EmojiRanges.Length; i+= 2)
                if (u32 >= EmojiRanges[i] && u32 <= EmojiRanges[i + 1])
                    return true;
            return false;
        }
    }
}