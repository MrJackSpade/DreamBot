using DreamBot.Shared.Utils;

namespace DreamBot.Shared.Models
{
    public class GeneratedImage
    {
        public GeneratedImage(Guid fileName, string base64)
        {
            FileName = Ensure.NotDefault(fileName);
            Data = base64 ?? throw new ArgumentNullException(nameof(base64));
        }

        public string Data { get; }

        public Guid FileName { get; }
    }
}