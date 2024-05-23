using Discord;
using DreamBot.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace DreamBot.Services
{
    internal static class ImageService
    {
        public static DisposableFileAttachment CreateFileAttachment(string base64, string fileName)
        {
            byte[] imageData = Convert.FromBase64String(base64);

            // Create a memory stream from the byte array
            MemoryStream imageStream = new(imageData);

            // Create an attachment from the memory stream
            return new DisposableFileAttachment(imageStream, new FileAttachment(imageStream, fileName));
        }

        public static DisposableFileAttachment CreateThumb(string base64, string fileName)
        {
            byte[] imageData = Convert.FromBase64String(base64);

            // Load the image from the byte array
            using Image image = Image.Load(imageData);

            // Calculate the dimensions for cropping
            int cropSize = Math.Min(image.Width, image.Height);
            int offsetX = (image.Width - cropSize) / 2;
            int offsetY = (image.Height - cropSize) / 2;

            // Crop the image to a square and resize it to 128x128
            image.Mutate(x => x
                .Crop(new Rectangle(offsetX, offsetY, cropSize, cropSize))
                .Resize(128, 128));

            // Save the modified image to a memory stream
            MemoryStream imageStream = new();

            image.Save(imageStream, PngFormat.Instance);

            // Reset the memory stream position
            imageStream.Position = 0;

            return new DisposableFileAttachment(imageStream, new FileAttachment(imageStream, fileName));
        }
    }
}