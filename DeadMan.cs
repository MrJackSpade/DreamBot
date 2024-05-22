namespace DreamBot
{
    internal class DeadMan
    {
        private readonly DateTime _triggerTime;

        private readonly ManualResetEvent _waitBlock = new(false);

        public DeadMan(int triggerMs)
        {
            _triggerTime = DateTime.Now.AddMilliseconds(triggerMs);
        }

        public bool Cancelled { get; private set; }

        public void Cancel()
        {
            _waitBlock.Set();
            Cancelled = true;
        }

        public void Wait()
        {
            int waitTime = (int)(_triggerTime - DateTime.Now).TotalMilliseconds;

            _waitBlock.WaitOne(waitTime);
        }
    }
}