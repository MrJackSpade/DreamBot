using Discord;

namespace DreamBot.Constants
{
    internal static class Emojis
    {
        public static IEmote STAR => new Emoji("⭐");

        public const string STR_TRASH = "🗑️";
        public static IEmote TRASH => new Emoji(STR_TRASH);

        public static IEmote LOLICE
        {
            get
            {
                return Emote.Parse("<:lolice:1227026462834688141>");
            }
        }

        public static IEmote FEAR => new Emoji("😱");
    }
}