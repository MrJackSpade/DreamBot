using Discord;

namespace DreamBot.Attributes
{
    internal class OptionTypeAttribute : Attribute
    {
        public ApplicationCommandOptionType Type { get; private set; }

        public OptionTypeAttribute(ApplicationCommandOptionType type)
        {
            Type = type;
        }
    }
}