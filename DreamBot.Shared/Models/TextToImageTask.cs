using DreamBot.Models.Automatic;

namespace DreamBot.Tasks
{
    public class TextToImageTask
    {
        public ManualResetEvent _completed = new(false);

        public EventHandler<TextToImageProgress> Completed;

        public EventHandler<TextToImageProgress> ProgressUpdated;

        public TextToImageTask(TextToImageRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            CancellationToken = cancellationToken;
            cancellationToken.Register(() => _completed.Set());
        }

        public CancellationToken CancellationToken { get; private set; }

        public TextToImageRequest Request { get; private set; }

        public TextToImageProgress State { get; private set; }

        public void SetProgress(TextToImageProgress state)
        {
            if (State is null || State != state)
            {
                State = state;

                ProgressUpdated?.Invoke(this, state);

                if (state.Completed)
                {
                    Completed?.Invoke(this, state);

                    _completed.Set();
                }
            }
        }

        public void Wait()
        {
            _completed.WaitOne();
        }
    }
}