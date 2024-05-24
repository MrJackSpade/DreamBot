using Discord.WebSocket;
using DreamBot.Attributes;
using DreamBot.Models;
using DreamBot.Shared.Models;
using System.ComponentModel.DataAnnotations;

namespace DreamBot.Plugins.Dream
{
    public class DreamCommand : BaseCommand
    {
        public DreamCommand(SocketSlashCommand command) : base(command)
        {
        }

        [Display(Name = "apply_default_styles", Description = "Apply default tags to increase quality")]
        public bool ApplyDefaultStyles { get; set; } = true;

        [Required]
        [Display(Name = "aspect_ratio")]
        public AspectRatio AspectRatio { get; set; }

        [Distinct]
        public List<string> Lora { get; set; } = [];

        public decimal LoraStrength { get; set; } = 1;

        [Distinct]
        [Display(Name = "negative_prompt")]
        public List<string> NegativePrompt { get; set; } = [];

        [Distinct]
        [Required]
        public List<string> Prompt { get; set; } = [];

        public long Seed { get; set; } = -1;
    }
}