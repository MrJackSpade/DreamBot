﻿using DreamBot.Plugins.Interfaces;
using DreamBot.Shared.Models;

namespace DreamBot.Plugins.Interfaces
{
    public interface ICommandProvider : IPlugin
    {
        string Command { get; }

        string Description { get; }

        SlashCommandOption[] SlashCommandOptions { get; }
    }

    public interface ICommandProvider<in TCommand> : ICommandProvider
    {
        Task<CommandResult> OnCommand(TCommand command);
    }
}