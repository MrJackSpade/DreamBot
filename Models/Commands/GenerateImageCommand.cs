using System.ComponentModel.DataAnnotations;

namespace DreamBot.Models.Commands
{
    internal class GenerateImageCommand : BaseCommand
    {
        [Required]
        [Display(Name = "aspect_ratio")]
        public AspectRatio AspectRatio { get; set; }

        [Display(Name = "negative_prompt")]
        public string NegativePrompt { get; set; } = string.Empty;

        [Required]
        public string Prompt { get; set; } = string.Empty;

        public long Seed { get; set; } = -1;
    }
}