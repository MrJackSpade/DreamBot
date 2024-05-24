using DreamBot.Services;
using DreamBot.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DreamBot.Extensions
{
    public static class DiscordServiceExtensions
    {
        public static async Task AddCommand<T>(this DiscordService service, string command, string description, Func<T, Task<string>> action, params SlashCommandOption[] slashCommandOptions) where T : BaseCommand
        {
            await service.AddCommand(command, description, typeof(T), a => action.Invoke((T)a), slashCommandOptions);
        }
    }
}