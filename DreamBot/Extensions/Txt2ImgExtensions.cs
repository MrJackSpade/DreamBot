using DreamBot.Models.Automatic;
using System.Text;

namespace DreamBot.Extensions
{
    public static class Txt2ImgExtensions
    {
        public static string ToDiscordString(this Txt2Img source, TimeSpan time)
        {
            StringBuilder sb = new();
            sb.Append($"✅ `{source.Prompt}` ");
            if (!string.IsNullOrWhiteSpace(source.NegativePrompt))
            {
                sb.Append($"❌ `{source.NegativePrompt}` ");
            }

            sb.Append($"🌱 `{source.Seed}` ");
            sb.Append($"📐 `{source.Width}x{source.Height}` ");
            int minutes = (int)time.TotalMinutes;

            if (minutes > 1)
            {
                sb.Append($"⏲️ `{minutes}m{(int)time.Seconds}s`");
            }
            else
            {
                sb.Append($"⏲️ `{(int)time.TotalSeconds}s`");
            }

            return sb.ToString();
        }
    }
}