using DreamBot.Models.Automatic;

namespace DreamBot.Shared.Interfaces
{
    public interface IAutomaticService
    {
        TextToImageTaskQueueResult Txt2Image(TextToImageRequest settings, CancellationToken token);
    }
}