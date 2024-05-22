using DreamBot.Attributes;
using System.ComponentModel.DataAnnotations;

namespace DreamBot.Models.Commands
{
    internal class GenerateImageCommand : BaseCommand
    {
        [Required]
        [Display(Name = "aspect_ratio")]
        public AspectRatio AspectRatio { get; set; }

        [Distinct]
        [Display(Name = "negative_prompt")]
        public List<string> NegativePrompt { get; set; } = [];

        [Distinct]
        [Required]
        public List<string> Prompt { get; set; } = [];

        [Display(Name = "apply_default_styles", Description = "Apply default tags to increase quality")]
        public bool ApplyDefaultStyles { get; set; } = true;

        [Distinct]
        public List<string> Lora { get; set; } = [];

        public decimal LoraStrength { get; set; } = 1;

        public long Seed { get; set; } = -1;
    }
}