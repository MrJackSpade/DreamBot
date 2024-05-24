using DreamBot.Tasks;

namespace DreamBot.Models.Automatic
{
    public record TextToImageTaskQueueResult(TextToImageTask AutomaticTask, int QueuePosition, DateTime QueueTime)
    {
        public bool Cancelled => this.AutomaticTask?.CancellationToken.IsCancellationRequested ?? false;
    }
}