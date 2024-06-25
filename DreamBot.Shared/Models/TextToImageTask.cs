using DreamBot.Models.Automatic;

namespace DreamBot.Tasks
{
    public class TextToImageTask
    {
        public ManualResetEvent _completed = new(false);

        public Func<TextToImageProgress, Task> Completed;

        public Func<TextToImageProgress, Task> ProgressUpdated;

        public TextToImageTask(TextToImageRequest request, CancellationToken cancellationToken)
        {
            Request = request;
            CancellationToken = cancellationToken;
            cancellationToken.Register(() => _completed.Set());
        }

        public CancellationToken CancellationToken { get; private set; }

        public TextToImageRequest Request { get; private set; }

        public TextToImageProgress State { get; private set; }

        public async Task SetProgress(TextToImageProgress state)
        {
            if (State is null || State != state)
            {
                State = state;

                if (ProgressUpdated != null)
                {
                    await ProgressUpdated.Invoke(state);
                }

                if (state.Completed)
                {
                    if (Completed != null)
                    {
                        await Completed.Invoke(state);
                    }

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