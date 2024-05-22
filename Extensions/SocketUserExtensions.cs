using Discord;
using Discord.WebSocket;

namespace DreamBot.Extensions
{
    internal static class SocketUserExtensions
    {
        public static async Task<Guid> SendFileAsync(this SocketUser user, string message, string base64)
        {
            Guid toReturn = Guid.NewGuid();

            byte[] imageData = Convert.FromBase64String(base64);

            // Create a memory stream from the byte array
            using MemoryStream imageStream = new(imageData);

            // Create an attachment from the memory stream
            FileAttachment file = new(imageStream, toReturn.ToString() + ".png");

            await user?.SendFileAsync(file, message);

            return toReturn;
        }
    }
}