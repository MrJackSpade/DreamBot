using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace DreamBot.Models.Commands
{
    public class CreateThreadCommand : BaseCommand
    {
        public string Description { get; set; } = string.Empty;

        [IgnoreDataMember]
        [Display(Name = "default_style")]
        public string DefaultStyle { get; set; }

        [Required]
        public string Title { get; set; }
    }
}