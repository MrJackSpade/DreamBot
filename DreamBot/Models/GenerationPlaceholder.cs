using Discord;
using Discord.Rest;
using DreamBot.Extensions;
using DreamBot.Services;
using System.Diagnostics;

namespace DreamBot.Models
{
    public class GenerationPlaceholder
    {
        private string _lastContent = string.Empty;

        private string _lastPreview = string.Empty;

        public GenerationPlaceholder(RestUserMessage message)
        {
            Message = message;
        }

        public RestUserMessage Message { get; set; }

        public async Task<Guid> TryUpdate(string title, string body, string? base64Data)
        {
            string newContent = $"{title}\r\n{body}";

            if (!this.HasChanged(newContent, base64Data))
            {
                Debug.WriteLine("Skipping update, no changes");
                return Guid.Empty;
            }
            else
            {
                Debug.WriteLine("Calling update with changes");
            }

            if (this.Message is null)
            {
                return Guid.Empty;
            }

            Guid toReturn = Guid.Empty;

            FileAttachment[] attachments = [];

            DisposableFileAttachment? disposableFileAttachment = null;

            if (!string.IsNullOrWhiteSpace(base64Data))
            {
                if (_lastPreview != base64Data)
                {
                    toReturn = Guid.NewGuid();

                    _lastPreview = base64Data;

                    disposableFileAttachment = ImageService.CreateFileAttachment(base64Data, $"{toReturn}.png");

                    attachments = [disposableFileAttachment.Attachment];
                }
            }

            // Modify the message and update the embed
            await this.Message.TryModifyAsync(properties =>
            {
                properties.Content = newContent;

                if (attachments.Length > 0)
                {
                    properties.Attachments = attachments;
                }
            }, 5);

            _lastContent = newContent;

            disposableFileAttachment?.Dispose();

            return toReturn;
        }

        private bool HasChanged(string content, string? base64Data) => _lastContent != content || _lastPreview != base64Data;
    }
}