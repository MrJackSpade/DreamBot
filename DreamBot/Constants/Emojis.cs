using Discord;

namespace DreamBot.Constants
{
    internal static class Emojis
    {
        public const string STR_TRASH = "🗑️";

        public static IEmote FEAR => new Emoji("😱");

        public static IEmote TRASH => new Emoji(STR_TRASH);
    }
}