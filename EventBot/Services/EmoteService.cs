using System;
using System.Collections.Generic;
using System.Text;
using NeoSmart.Unicode;
using System.Linq;
using Discord;
using DEmoji = Discord.Emoji;
using UEmoji = NeoSmart.Unicode.Emoji;

namespace EventBot.Services
{

    public class EmoteService
    {
        private IEnumerable<string> emoji;

        public EmoteService()
        {
            emoji = UEmoji.All.Select(e => e.Sequence.AsString);
        }

        public bool TryParse(string input, out IEmote emote)
        {
            if(Emote.TryParse(input, out Emote parsedEmote))
            {
                emote = parsedEmote;
                return true;
            }
            if(emoji.Contains(input))
            {
                emote = new DEmoji(input);
                return true;
            }
            emote = null;
            return false;
        }

        public IEmote Parse(string input)
        {
            if (!TryParse(input, out IEmote parsed))
                throw new ArgumentException("Failed to parse emote.");
            return parsed;
        }

    }
}