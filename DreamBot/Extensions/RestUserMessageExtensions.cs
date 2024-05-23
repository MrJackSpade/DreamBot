using Discord;
using Discord.Rest;

namespace DreamBot.Extensions
{
    public static class RestUserMessageExtensions
    {
        public static async Task TryModifyAsync(this RestUserMessage message, Action<MessageProperties> func, int tries, int timeoutMs = 3000, RequestOptions options = null)
        {
            do
            {
                try
                {
                    await message.ModifyAsync(func, options);
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    await Task.Delay(timeoutMs);
                }
            } while (tries-- > 0);
        }
    }
}