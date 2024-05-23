namespace DreamBot.Database.Models
{
    public class GenerationData
    {
        public byte[] Data { get; set; }

        public Guid FileGuid { get; set; }

        public long GenerationId { get; set; }
    }
}