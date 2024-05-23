namespace DreamBot.Models
{
    public class CancellableTask
    {
        public EventHandler OnCancelled;

        public bool Cancelled { get; private set; }

        public void Cancel()
        {
            this.OnCancelled?.Invoke(this, null);
            this.Cancelled = true;
        }
    }
}