using Discord;
using Discord.WebSocket;
using EventBot.Entities;
using EventBot.Misc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace EventBot.Services
{
    public class EventManagementService
    {
        private readonly DiscordSocketClient _discord;
        private readonly DatabaseService _database;
        private readonly IServiceProvider _services;

        public EventManagementService(IServiceProvider services)
        {
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _database = services.GetRequiredService<DatabaseService>();
            _services = services;

            _discord.ReactionAdded += ReactionAddedAsync;
            _discord.MessageDeleted += MessageDeletedAsync;
        }

        public async Task TryJoinEvent(IGuildUser user, EventRole er, string extra, bool extraChecks = true)
        {
            if (er.Event.GuildId != user.GuildId)
                throw new Exception("Cross guild events are forbidden.");
            if (extraChecks && er.ReamainingOpenings <= 0)
                throw new Exception("No openings are left.");
            if(er.Event.Participants.Where(p => p.UserId == user.Id).Count() > 0)
                throw new Exception("You are already participating.");
            if(extraChecks && !er.Event.Active)
                throw new Exception("Event is closed.");

            if (er.Event.Guild.ParticipantRoleId != 0)
                await user.AddRoleAsync(user.Guild.GetRole(er.Event.Guild.ParticipantRoleId));
            
            var ep = new EventParticipant()
            {
                UserId = user.Id,
                Event = er.Event,
                Role = er
            };
            var embed = new EmbedBuilder()
                    .WithTitle($"{user} has joined event `{er.Event.Title}`")
                    .WithDescription($"They have chosen `{er.Title}` role.")
                    .WithColor(Color.Green);
            if (extra != null && extra != string.Empty)
            {
                embed.AddField("Provided details", $"`{extra}`");
                ep.UserData = extra;
            }
            _database.Add(ep);
            await _database.SaveChangesAsync();
            await UpdateEventMessage(er.Event);
            if (er.Event.Guild.EventRoleConfirmationChannelId != 0)
                await (await user.Guild.GetTextChannelAsync(er.Event.Guild.EventRoleConfirmationChannelId)).SendMessageAsync(embed: embed.Build());
        }

        public Event FindEventBy(IGuild guild, bool bypassActive = false)
        {
            return _database.Events.OrderByDescending(e => e.Opened).FirstOrDefault(e => e.GuildId == guild.Id && (e.Active || bypassActive));
        }

        public Event FindEventBy(IGuild guild, int? eventId, bool bypassActive = false)
        {
            if (eventId == null)
                return FindEventBy(guild, bypassActive);
            return _database.Events.OrderByDescending(e => e.Opened).FirstOrDefault(e => e.GuildId == guild.Id && e.Id == eventId && (e.Active || bypassActive));
        }

        public async Task UpdateEventMessage(Event ev)
        {
            if (ev.MessageChannelId == 0 || ev.MessageId == 0)
                return;
            var channel = (ITextChannel) _discord.GetChannel(ev.MessageChannelId);
            var message = (IUserMessage) await channel.GetMessageAsync(ev.MessageId);
            await message.ModifyAsync(m => m.Embed = GenerateEventEmbed(ev).Build());
        }

        public EmbedBuilder GenerateEventEmbed(Event @event)
        {
            var embed = new EmbedBuilder()
                .WithTitle($"{@event.Title}")
                .WithDescription(@event.Description)
                .WithFooter($"EventId: {@event.Id}")
                .WithColor(Color.Purple)
                ;
            if (@event.Type == Event.EventParticipactionType.Quick)
                embed.Description += "\r\nTo participate in this event react with following emotes:";
            if (@event.Type == Event.EventParticipactionType.Detailed)
                embed.Description += "\r\nTo participate in this event use command `join <emote or id> <extra information>` as following emotes are available:";

            embed.WithFields(@event.Roles
                .OrderBy(e => e.SortNumber)
                .Select(e => new EmbedFieldBuilder()
                    .WithName($"{e.Emote} `{e.Id}`: *{e.Title}*`{ (e.MaxParticipants > 0 ? $" - {e.ReamainingOpenings} remaining" : "")} - {e.ParticipantCount} participating.`")
                    .WithValue($"{e.Description}")
                ));
            return embed;
        }

        public async Task MessageDeletedAsync(Cacheable<IMessage, ulong> message, ISocketMessageChannel socketMessage)
        {
            var @event = _database.Events.FirstOrDefault(e => e.MessageId == message.Id);
            if(@event != null)
            {
                @event.MessageId = 0;
                @event.MessageChannelId = 0;
                await _database.SaveChangesAsync();
            }
                
        }


        public async Task ReactionAddedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel socketMessage, SocketReaction reaction)
        {
            if (!reaction.User.IsSpecified || reaction.User.Value.IsBot)
                return;
            var @event = _database.Events.FirstOrDefault(e => e.MessageId == message.Id);
            if (@event != null)
            {
                var role = @event.Roles.FirstOrDefault(r => reaction.Emote.Equals(EmoteHelper.Parse(r.Emote)));
                if(role != null)
                {
                    var userMessage = await message.GetOrDownloadAsync();
                    if (reaction.User.IsSpecified)
                        await userMessage.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
                    try
                    {
                        if (!(reaction.User.GetValueOrDefault() is IGuildUser guildUser))
                            throw new Exception("Reaction must be made inside guild");
                        await TryJoinEvent(guildUser, role, null);
                    }
                    catch (Exception ex)
                    {
                        if (reaction.User.IsSpecified)
                            await reaction.User.Value.SendMessageAsync($"Error occured while processing your reaction: \r\n{ex.GetType()}: {ex.Message}");
                    }
                }
            }
        }
    }
}
