using Discord;
using Discord.Commands;
using Discord.WebSocket;
using EventBot.Entities;
using EventBot.Misc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

namespace EventBot.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly DatabaseService _database;
        private readonly IServiceProvider _services;

        public event Func<LogMessage, Task> Log;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _database = services.GetRequiredService<DatabaseService>();
            _services = services;

            // Hook CommandExecuted to handle post-command-execution logic.
            _commands.CommandExecuted += CommandExecutedAsync;
            // Hook MessageReceived so we can process each message to see
            // if it qualifies as a command.
            _discord.MessageReceived += MessageReceivedAsync;
        }

        public async Task InitializeAsync()
        {
            _commands.AddTypeReader<Event>(new EventTypeReader());
            _commands.AddTypeReader<EventRole>(new EventRoleTypeReader());
            // Register modules that are public and inherit ModuleBase<T>.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // Ignore system messages, or messages from other bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            // This value holds the offset where the prefix ends
            var argPos = 0;
            // Perform prefix check. You may want to replace this with
            // (!message.HasCharPrefix('!', ref argPos))
            // for a more traditional command format like !help.
            var context = new SocketCommandContext(_discord, message);
            GuildConfig guildConfig = context.Guild != null ? _database.GuildConfigs.FirstOrDefault(g => g.GuildId == context.Guild.Id) : null;
            if (!message.HasMentionPrefix(_discord.CurrentUser, ref argPos))
                if (guildConfig != null)
                {
                    if (!message.HasStringPrefix(guildConfig.Prefix, ref argPos))
                        return;
                } else
                {
                    return;
                }

            await Log?.Invoke(new LogMessage(LogSeverity.Debug, "CommandHandlingService", $"Got potential command: {message.Content}"));
            // Perform the execution of the command. In this method,
            // the command service will perform precondition and parsing check
            // then execute the command if one is matched.

            await _commands.ExecuteAsync(context, argPos, _services);
            // Note that normally a result will be returned by this format, but here
            // we will handle the result in CommandExecutedAsync,
        }

        public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // command is unspecified when there was a search failure (command not found); we don't care about these errors
            if (!command.IsSpecified)
                return;

            // the command was successful, we don't care about this result, unless we want to log that a command succeeded.
            if (result.IsSuccess)
                return;

            // the command failed, let's notify the user that something happened.
            await context.Channel.SendMessageAsync($"error: {result}");
        }
    }
}
