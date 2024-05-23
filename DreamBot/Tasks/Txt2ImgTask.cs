using DreamBot.Models.Automatic;

namespace DreamBot.Tasks
{
    public class Txt2ImgTask
    {
        public ManualResetEvent _completed = new(false);

        public EventHandler<Txt2ImgProgress> Completed;

        public EventHandler<Txt2ImgProgress> ProgressUpdated;

        public Txt2ImgTask(Txt2Img request, CancellationToken cancellationToken)
        {
            Request = request;
            CancellationToken = cancellationToken;
            cancellationToken.Register(() => _completed.Set());
        }

        public CancellationToken CancellationToken { get; private set; }

        public Txt2Img Request { get; private set; }

        public Txt2ImgProgress State { get; private set; }

        public void SetProgress(Txt2ImgProgress state)
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