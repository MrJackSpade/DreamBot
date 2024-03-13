using Discord;

namespace DreamBot.Models
{
    internal class SlashCommandOption
    {
        public SlashCommandOption(string name, string description = "", bool required = false)
        {
            Name = name.ToLower();
            Description = description;
            Required = required;
        }

        public string Description { get; set; } = string.Empty;

        public string Name { get; set; }

        public bool Required { get; set; }

        public ApplicationCommandOptionType Type { get; set; } = ApplicationCommandOptionType.String;
    }
}