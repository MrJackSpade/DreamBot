﻿using Discord;
using Discord.WebSocket;

namespace DreamBot.Models.Events
{
    internal class ReactionEventArgs(Cacheable<IUserMessage, ulong> userMessage, Cacheable<IMessageChannel, ulong> messageChannel, SocketReaction socketReaction)
    {
        public Cacheable<IMessageChannel, ulong> MessageChannel { get; set; } = messageChannel;

        public SocketReaction SocketReaction { get; set; } = socketReaction ?? throw new ArgumentNullException(nameof(socketReaction));

        public Cacheable<IUserMessage, ulong> UserMessage { get; set; } = userMessage;
    }
}