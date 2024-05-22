using System.ComponentModel.DataAnnotations;

namespace DreamBot.Models.Commands
{
    internal class PurgeCommand : BaseCommand
    {
        [Required]
        public ulong TargetUserId { get; set; }

        [Required]
        public int Days { get; set; }
    }
}