using Discord;

namespace DreamBot.Models
{
    internal class SlashCommandOption
    {
        public string[] Choices { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Name { get; set; }

        public bool Required { get; set; }

        public ApplicationCommandOptionType Type { get; set; } = ApplicationCommandOptionType.String;

        public SlashCommandOption(string name, string description, bool required, params string[] choices)
        {
            this.Name = name.ToLower();
            this.Description = description;
            this.Required = required;
            this.Choices = choices;
        }
    }
}