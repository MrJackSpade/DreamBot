using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DreamBot.Models;

namespace DreamBot.Services
{
    internal class ForumPostService
    {
        private readonly SocketThreadChannel _channel;

        private RestUserMessage? _firstMessage = null;

        private bool _missingFirstMessage = false;

        public ForumPostService(IChannel channel)
        {
            if (channel is SocketThreadChannel stc)
            {
                _channel = stc;
            }
            else
            {
                throw new ArgumentException("Channel is not socket thread channel");
            }
        }

        public async Task<RestUserMessage> GetFirstMessage()
        {
            if (_missingFirstMessage)
            {
                return null;
            }

            if (_firstMessage != null)
            {
                return _firstMessage;
            }

            IReadOnlyCollection<IMessage> pinned = await _channel.GetPinnedMessagesAsync();

            _firstMessage = pinned.FirstOrDefault() as RestUserMessage;

            _missingFirstMessage = _firstMessage == null;

            return _firstMessage;
        }

        public async Task<bool> IsCreator(ulong creatorId)
        {
            return (await this.GetFirstMessage())?.Author?.Id == creatorId;
        }

        public async Task UpdateImage(string imageData)
        {
            using DisposableFileAttachment disposableFileAttachment = ImageService.CreateThumb(imageData, "preview.png");

            await this.UpdateAttachment(disposableFileAttachment);
        }

        private async Task UpdateAttachment(DisposableFileAttachment disposableFileAttachment)
        {
            if ((await this.GetFirstMessage()) is not RestUserMessage sum)
            {
                return;
            }

            // Get the existing attachments of the message
            List<Attachment> existingAttachments = [.. sum.Attachments];

            existingAttachments = existingAttachments.Where(a => a.Filename != disposableFileAttachment.Attachment.FileName).ToList();

            HttpClient httpClient = new();

            List<FileAttachment> newAttachments = [];

            foreach (Attachment a in existingAttachments)
            {
                newAttachments.Add(new FileAttachment(await httpClient.GetStreamAsync(a.Url), a.Filename));
            }

            newAttachments.Add(disposableFileAttachment.Attachment);

            // Update the message with the modified attachments
            await sum.ModifyAsync(m => m.Attachments = newAttachments.ToArray());
        }
    }
}