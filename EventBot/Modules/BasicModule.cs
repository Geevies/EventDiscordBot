using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using EventBot.Attributes;
using EventBot.Services;

namespace EventBot.Modules
{
    public class BasicModule : ModuleBase<SocketCommandContext>
    {
        private readonly DatabaseService _database;
        public BasicModule(DatabaseService database)
        {
            _database = database;
        }

        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        [RequireContext(ContextType.Guild)]
        [Command("prefix")]
        [Summary("Gets prefix.")]
        public async Task PrefixCommand()
        {
            var guildConfig = _database.GuildConfigs.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
            if (guildConfig == null)
                throw new Exception("No guild config was foumd.");
            if(guildConfig.Prefix != null)
                await ReplyAsync($"Current prefix is `{guildConfig.Prefix}`");
            else
                await ReplyAsync($"There is no prefix set for this guild.");
        }

        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        [RequireContext(ContextType.Guild)]
        [Command("prefix")]
        [Summary("Sets prefix.")]
        public async Task PrefixCommand(
            [Summary("New prefix to set")] string newPrefix)
        {
            var guildConfig = _database.GuildConfigs.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
            if (guildConfig == null)
                throw new Exception("No guild config was foumd.");
            guildConfig.Prefix = newPrefix;
            await _database.SaveChangesAsync();
            await ReplyAsync($"Prefix has been set to `{guildConfig.Prefix}`");
        }

        [Group("help")]
        public class HelpModule : ModuleBase<SocketCommandContext>
        {

            private readonly CommandService _commands;
            public HelpModule(CommandService commands)
            {
                _commands = commands;
            }

            [Command]
            [Summary("Lists all commands with there descriptions.")]
            public async Task DefaultHelpAsync()
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Command list")
                    .WithColor(Color.DarkBlue)
                    .WithCurrentTimestamp()
                    .WithFields(_commands.Commands
                        .Where(c => c.Attributes.Where(a => a is NoHelpAttribute || (a is RequireContextAttribute requireContext)).Count() == 0)
                        .Select(c =>
                            new EmbedFieldBuilder()
                            {
                                Name = $"`{string.Join(", ", c.Aliases)} {string.Join(" ", c.Parameters.Select(p => p.IsOptional ? $"[{p.Name}]" : $"<{p.Name}>"))}`",
                                Value = c.Summary
                            })
                    );
                await Context.User.SendMessageAsync(embed: embed.Build());
            }
        }
    }
}
