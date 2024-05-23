namespace DreamBot.Database.Models
{
    public class GenerationProperty
    {
        public long GenerationId { get; set; }

        public string Property { get; set; }

        public string? Value { get; set; }
    }
}