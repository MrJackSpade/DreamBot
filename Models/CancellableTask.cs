namespace DreamBot.Models
{
    public class CancellableTask
    {
        public EventHandler OnCancelled;

        public bool Cancelled { get; private set; }

        public void Cancel()
        {
            OnCancelled?.Invoke(this, null);
            Cancelled = true;
        }
    }
}