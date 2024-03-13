using DreamBot.Tasks;

namespace DreamBot.Models.Automatic
{
    internal record QueueTxt2ImgTaskResult(Txt2ImgTask AutomaticTask, int QueuePosition, DateTime QueueTime)
    {
        public bool Cancelled => this.AutomaticTask?.CancellationToken.IsCancellationRequested ?? false;
    }
}