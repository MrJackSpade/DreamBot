namespace DreamBot.Database.Models
{
    public class Generation
    {
        public ulong ChannelId { get; set; }

        public DateTime DateCreated { get; set; }

        public long Id { get; set; }

        public ulong MessageId { get; set; }

        public ulong UserId { get; set; }
    }
}