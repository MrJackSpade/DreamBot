using DreamBot.Tasks;

namespace DreamBot.Models
{
    public class QueuedTask : CancellableTask
    {
        public QueuedTask(ulong userId)
        {
            this.UserId = userId;
        }

        public TextToImageTask AutomaticTask { get; set; }

        public ulong MessageId { get; set; }

        public ulong UserId { get; private set; }
    }
}