using Discord;

namespace DreamBot.Attributes
{
    internal class OptionTypeAttribute : Attribute
    {
        public OptionTypeAttribute(ApplicationCommandOptionType type)
        {
            Type = type;
        }

        public ApplicationCommandOptionType Type { get; private set; }
    }
}