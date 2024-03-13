using Discord;
using Discord.Rest;
using DreamBot.Extensions;
using System.Diagnostics;

namespace DreamBot.Models
{
    internal class GenerationEmbed
    {
        private string _lastContent = string.Empty;

        private string _lastFileName = string.Empty;

        private string _lastPreview = string.Empty;

        public RestUserMessage Message { get; set; }

        public async Task<bool> TryUpdate(string title, string body, string? base64Data)
        {
            string newContent = $"{title}\r\n{body}";

            if (!this.HasChanged(newContent, base64Data))
            {
                Debug.WriteLine("Skipping update, no changes");
                return false;
            }
            else
            {
                Debug.WriteLine("Calling update with changes");
            }

            if (Message is null)
            {
                return false;
            }

            FileAttachment[] attachments = [];

            MemoryStream? imageStream = null;

            if (!string.IsNullOrWhiteSpace(base64Data))
            {
                if (_lastPreview != base64Data)
                {
                    _lastPreview = base64Data;

                    _lastFileName = $"{Guid.NewGuid()}.png";

                    byte[] imageData = Convert.FromBase64String(base64Data);

                    // Create a memory stream from the byte array
                    imageStream = new(imageData);

                    // Create an attachment from the memory stream
                    attachments = [new(imageStream, _lastFileName)];
                }
            }

            // Modify the message and update the embed
            await Message.TryModifyAsync(properties =>
            {
                properties.Content = newContent;

                if (attachments.Length > 0)
                {
                    properties.Attachments = attachments;
                }
            }, 5);

            _lastContent = newContent;

            imageStream?.Dispose();

            return true;
        }

        private bool HasChanged(string content, string? base64Data)
        {
            return _lastContent != content || _lastPreview != base64Data;
        }
    }
}