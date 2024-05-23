namespace DreamBot.Database.Models
{
    public class GenerationDto
    {
        public GenerationDto()
        {
            GenerationData = [];
            GenerationProperties = [];
        }

        public ulong ChannelId { get; set; }

        public DateTime DateCreatedUtc { get; set; }

        public List<GenerationData> GenerationData { get; set; }

        public List<GenerationProperty> GenerationProperties { get; set; }

        public long Id { get; set; }

        public ulong MessageId { get; set; }

        public ulong UserId { get; set; }

        public void AddProperty(string name, object? value)
        {
            GenerationProperties.Add(new GenerationProperty()
            {
                Property = name,
                Value = value?.ToString()
            });
        }
    }
}