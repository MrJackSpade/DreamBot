using Discord;

namespace DreamBot.Models
{
    internal class DisposableFileAttachment : IDisposable
    {
        private readonly MemoryStream _data;

        private bool disposedValue;

        public DisposableFileAttachment(MemoryStream data, FileAttachment attachment)
        {
            this._data = data;
            this.Attachment = attachment;
        }

        public FileAttachment Attachment { get; private set; }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    this._data.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this.disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DisposableFileAttachment()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }
    }
}