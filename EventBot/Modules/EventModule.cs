﻿using Discord;
using Discord.Commands;
using EventBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using EventBot.Entities;
using Discord.WebSocket;
using Discord.Addons.Interactive;

namespace EventBot.Modules
{
    [RequireContext(ContextType.Guild)]
    public class EventModule : ModuleBase<SocketCommandContext>
    {
        private readonly EventManagementService _events;
        private readonly DatabaseService _database;
        public EventModule(EventManagementService events, DatabaseService database)
        {
            _events = events;
            _database = database;
        }

        [Command("join")]
        [Summary("Joins latest or specified event with specified event role.")]
        public async Task JoinAsync(
            [Summary("Role emote or role id to join.")] string emoteOrId,
            [Summary("Extra information that migth be needed by organizers.")] string extraInformation = null,
            [Summary("Optional event ID for joining event that is not most recent one.")] Event @event = null)
        {
            EventRole er;
            if (!(Context.User is SocketGuildUser guildUser))
                throw new Exception("This command must be executed inside guild.");
            if (@event == null)
                @event = _events.FindEventBy(Context.Guild);
            if (@event == null & !(int.TryParse(emoteOrId, out int roleId)))
                throw new Exception("Unable to locate any events for this guild.");
            else if (@event == null)
                er = _database.EventRoles.FirstOrDefault(r => r.Id == roleId);
            else
                er = @event.Roles.FirstOrDefault(r => r.Emote == emoteOrId);
            if (er == null)
                throw new ArgumentException("Invalid emote or event id specified");
            if (@event.MessageId == 0)
                throw new Exception("You can't join not opened event.");

            await _events.TryJoinEvent(guildUser, er, extraInformation);
            await Context.Message.DeleteAsync(); // Protect somewhat sensitive data.
        }

        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        [Group("event")]
        public class EventManagementModule : InteractiveBase<SocketCommandContext>
        {
            private readonly EventManagementService _events;
            private readonly DatabaseService _database;
            public EventManagementModule(EventManagementService events, DatabaseService database)
            {
                _events = events;
                _database = database;
            }

            [Command("config logchannel")]
            [Summary("Sets logging channel for role changes.")]
            public async Task SetRoleChannelAsync(
            [Summary("Channel to use for logging.")] IChannel channel)
            {
                var guild = _database.GuildConfigs.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
                if (guild == null)
                    throw new Exception("This command must be executed inside guild.");

                guild.EventRoleConfirmationChannelId = channel.Id;
                var s = _database.SaveChangesAsync();
                await ReplyAsync($"Event role changes now will be logged to `{channel.Name}` channel.");
                await s;
            }

            [Command("config partrole")]
            [Summary("Sets role to assign when they selelct role.")]
            public async Task SetParticipationRole(
            [Summary("Role to assign.")] IRole role)
            {
                var guild = _database.GuildConfigs.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
                if (guild == null)
                    throw new Exception("This command must be executed inside guild.");

                guild.ParticipantRoleId = role.Id;
                var s = _database.SaveChangesAsync();
                await ReplyAsync($"Event participants will be given `{role.Name}` role.");
                await s;
            }

            [Command("new")]
            [Summary("Creates new event.")]
            public async Task NewEvent(
            [Summary("Title for the event.")] string title,
            [Summary("Description for the event.")] string description,
            [Summary("Type of event registration.")] Event.EventParticipactionType type = Event.EventParticipactionType.Quick)
            {
                var guild = _database.GuildConfigs.FirstOrDefault(g => g.GuildId == Context.Guild.Id);
                if (guild == null)
                    throw new Exception("This command must be executed inside guild.");
                var @event = new Event()
                {
                    Title = title,
                    Description = description,
                    Type = type,
                    Guild = guild
                };

                _database.Add(@event);
                await _database.SaveChangesAsync();
                await ReplyAsync($"Created new {@event.Type} event `{title}`, with description of `{description}`. It's ID is `{@event.Id}`.");
            }

            [Command("update title")]
            [Summary("Updates event title.")]
            public async Task UpdateEventTitle(
            [Summary("Title for the event.")] string title,
            [Summary("Event to update, if not specified, updates latest event.")] Event @event = null)
            {
                if (@event == null)
                    @event = _events.FindEventBy(Context.Guild);
                if (@event == null)
                    throw new Exception("Unable to locate any events for this guild.");
                if (!@event.Active)
                    throw new Exception("This event is finalized. Please make a new event.");
                @event.Title = title;
                await _database.SaveChangesAsync();
                await ReplyAsync($"Updated event(`{@event.Id}`) title to `{@event.Title}`");
                await _events.UpdateEventMessage(@event);
            }
            [Command("update description")]
            [Summary("Updates event description.")]
            public async Task UpdateEventDescription(
            [Summary("Description for the event.")] string description,
            [Summary("Event to update, if not specified, updates latest event.")] Event @event = null)
            {
                if (@event == null)
                    @event = _events.FindEventBy(Context.Guild);
                if (@event == null)
                    throw new Exception("Unable to locate any events for this guild.");
                if (!@event.Active)
                    throw new Exception("This event is finalized. Please make a new event.");
                @event.Description = description;
                await _database.SaveChangesAsync();
                await ReplyAsync($"Updated event(`{@event.Id}`) description to `{@event.Description}`");
                await _events.UpdateEventMessage(@event);
            }

            [Command("update type")]
            [Summary("Updates event type.")]
            public async Task UpdateEventType(
            [Summary("Type of event registration.")] Event.EventParticipactionType type,
            [Summary("Event to update, if not specified, updates latest event.")] Event @event = null)
            {
                if (type == Event.EventParticipactionType.Unspecified)
                    return;
                if (@event == null)
                    @event = _events.FindEventBy(Context.Guild);
                if (@event == null)
                    throw new Exception("Unable to locate any events for this guild.");
                if (!@event.Active)
                    throw new Exception("This event is finalized. Please make a new event.");
                if (@event.MessageId != 0 && @event.Type != type)
                    throw new Exception("Can't change event registration type when it's open for registration. Maube you meant to set type to `Unspecified` (-1)");
                if(@event.Type != type)
                    @event.Type = type;                
                await _database.SaveChangesAsync();
                await ReplyAsync($"Updated event(`{@event.Id}`) type to `{@event.Type}`");
            }


            [Command("role new")]
            [Summary("Adds new role to the event.")]
            public async Task NewEventRole(
            [Summary("Title for the role.")] string title,
            [Summary("Description for the role.")] string description,
            [Summary("Emote for the role.")] string emote,
            [Summary("Max openings, if number is negative, opening count is unlimited.")] int maxOpenings = -1,
            [Summary("Event to that role is meant for.")] Event @event = null)
            {
                if (@event == null)
                    @event = _events.FindEventBy(Context.Guild);
                if (@event == null)
                    throw new Exception("Unable to locate any events for this guild.");
                if (!@event.Active)
                    throw new Exception("This event is finalized. Please make a new event.");
                if (@event.Roles != null && @event.Roles.Count >= 20)
                    throw new Exception("There are too many roles for this event.");
                if(@event.MessageId != 0)
                    throw new Exception("Can't add new roles to event with open reigstration.");
                if (!EmoteHelper.TryParse(emote, out IEmote parsedEmote))
                    throw new ArgumentException("Invalid emote provided.");
                if(@event.Roles != null && @event.Roles.Count(r => r.Emote == parsedEmote.ToString()) > 0)
                    throw new ArgumentException("This emote is already used by other role.");
                var er = new EventRole()
                {
                    Title = title,
                    Description = description,
                    MaxParticipants = maxOpenings,
                    Emote = parsedEmote.ToString(),
                    Event = @event
                };
                _database.Add(er);
                await _database.SaveChangesAsync();
                await ReplyAsync($"Added event role `{er.Id}` for event `{er.Event.Id}`, title: `{er.Title}`, description: `{er.Description}`, maxPart: `{er.MaxParticipants}`, emote: {er.Emote}");
            }

            [Command("role update title")]
            [Summary("Updates role's title")]
            public async Task UpdateEventRoleTitle(
            [Summary("Role witch to update.")] EventRole eventRole,
            [Summary("New title for role.")][Remainder] string title)
            {
                if(eventRole == null)
                    throw new Exception("Please provide correct role.");
                if (!eventRole.Event.Active)
                    throw new Exception("This event is finalized. Please make a new event.");
                eventRole.Title = title;
                var s = _database.SaveChangesAsync();
                await ReplyAsync($"Updated event role `{eventRole.Id}` title to `{eventRole.Title}`");
                await s;
                await _events.UpdateEventMessage(eventRole.Event);
            }

            [Command("role update desc")]
            [Summary("Updates role's description.")]
            public async Task UpdateEventRoleDescription(
            [Summary("Role witch to update.")] EventRole eventRole,
            [Summary("New description for role.")][Remainder] string description)
            {
                if (eventRole == null)
                    throw new Exception("Please provide correct role.");
                if (!eventRole.Event.Active)
                    throw new Exception("This event is finalized. Please make a new event.");
                eventRole.Description = description;
                var s = _database.SaveChangesAsync();
                await ReplyAsync($"Updated event role `{eventRole.Id}` description to `{eventRole.Description}`");
                await s;
                await _events.UpdateEventMessage(eventRole.Event);
            }

            [Command("role update slots")]
            [Summary("Updates role's maximum participants count.")]
            public async Task UpdateEventRoleMaxParticipants(
            [Summary("Role witch to update.")] EventRole eventRole,
            [Summary("New maximum participant count for role.")] int maxParticipants)
            {
                if (eventRole == null)
                    throw new Exception("Please provide correct role.");
                if (!eventRole.Event.Active)
                    throw new Exception("This event is finalized. Please make a new event.");
                eventRole.MaxParticipants = maxParticipants;
                var s = _database.SaveChangesAsync();
                await ReplyAsync($"Updated event role `{eventRole.Id}` maximum participant count to `{eventRole.MaxParticipants}`");
                await s;
                await _events.UpdateEventMessage(eventRole.Event);
            }


            [Command("role update emote")]
            [Summary("Updates role's emote.")]
            public async Task UpdateEventRoleEmote(
            [Summary("Role witch to update.")] EventRole eventRole,
            [Summary("New emote for the role.")] string emote)
            {
                if (eventRole == null)
                    throw new Exception("Please provide correct role.");
                if (eventRole.Event.MessageId != 0)
                    throw new Exception("Role emote can't be edited while event is open for registrasion.");
                if (!eventRole.Event.Active)
                    throw new Exception("This event is finalized. Please make a new event.");
                if (!EmoteHelper.TryParse(emote, out IEmote parsedEmote))
                    throw new ArgumentException("Invalid emote provided.");
                    
                if (eventRole.Event.Roles.Count(r => r.Emote == parsedEmote.ToString()) > 0)
                    throw new ArgumentException("This emote is already used by other role.");
                eventRole.Emote = parsedEmote.ToString();
                var s = _database.SaveChangesAsync();
                await ReplyAsync($"Updated event role `{eventRole.Id}` emote to {eventRole.Emote}");
                await s;
            }

            [Command()]
            [Summary("Get info about event.")]
            public async Task EventInfo(
            [Summary("Event about witch info is wanted.")] Event @event = null)
            {
                if (@event == null)
                    @event = _events.FindEventBy(Context.Guild);
                if (@event == null)
                    throw new Exception("No events were found for this guild.");
                var embed = new EmbedBuilder()
                    .WithTitle(@event.Title)
                    .WithDescription(@event.Description)
                    .WithTimestamp(@event.Opened)
                    .WithFooter($"EventId: {@event.Id}; MessageId: {@event.MessageId}; MessageChannelId: {@event.MessageChannelId}")
                    .AddField("Active", @event.Active ? "Yes" : "No", true)
                    .AddField("Type", @event.Type, true)
                    .AddField("Participants", @event.ParticipantCount, true);
                if (@event.Roles != null)
                    embed.WithFields(@event.Roles.OrderBy(e => e.SortNumber).Select(r => new EmbedFieldBuilder()
                        .WithName($"Id: `{r.Id}` {r.Emote} `{r.Title}` - {r.ParticipantCount} participants; {(r.MaxParticipants < 0 ? "infinite" : r.MaxParticipants.ToString())} max slots.")
                        .WithValue(r.ParticipantCount == 0 ? "There are no participants" : string.Join("\r\n", r.Participants
                            .Select(p => new { Participant = p, User = Context.Guild.GetUser(p.UserId) })
                            .OrderBy(o => o.User.ToString())
                            .Select(o => $"{o.User}{(o.Participant.UserData != null ? $" - `{o.Participant.UserData}`" : "")}")
                            ))
                    ));

                await ReplyAsync(embed: embed.Build());
            }
            [Priority(1)]
            [Command("role")]
            [Summary("Gets role info.")]
            public async Task EventRoleInfo(
            [Summary("Role about witch info is wanted.")] EventRole eventRole)
            {
                if (eventRole == null)
                    throw new Exception("Please provide correct role.");
                var embed = new EmbedBuilder()
                    .WithTitle($"{eventRole.Emote} {eventRole.Title}")
                    .WithDescription($"{eventRole.Description}")
                    .WithFooter($"EventRoleId: {eventRole.Id}, EventId: {eventRole.Event.Id}")
                    .AddField("Max participants", eventRole.MaxParticipants < 0 ? "infinite" : eventRole.MaxParticipants.ToString(), true)
                    .AddField("Participants", eventRole.ParticipantCount, true);

                var msg = await ReplyAsync(embed: embed.Build());

                if (eventRole.Participants != null && eventRole.Participants.Count > 0)
                {
                    embed.AddField("Participants", string.Join("\r\n", eventRole.Participants.Select(p => $"id: `{p.Id}` <@{p.UserId}>{(p.UserData != null ? $" - {p.UserData}" : "")}")));
                    await msg.ModifyAsync(m => m.Embed = embed.Build());
                }
            }

            [Command("open")]
            [Summary("Open registration for event here.")]
            public async Task EventOpen(
            [Summary("Event to open")] Event @event = null)
            {
                if (@event == null)
                    @event = _events.FindEventBy(Context.Guild);
                if (@event == null)
                    throw new Exception("No events were found for this guild.");
                if (!@event.Active)
                    throw new Exception("This event is finalized. Please make a new event.");

                await Context.Message.DeleteAsync();
                var message = await ReplyAsync(embed: _events.GenerateEventEmbed(@event).Build());
                @event.MessageId = message.Id;
                @event.MessageChannelId = message.Channel.Id;
                await _database.SaveChangesAsync();

                switch (@event.Type)
                {
                    case Event.EventParticipactionType.Unspecified:
                        throw new Exception("Event type was unspecified.");
                    case Event.EventParticipactionType.Quick:
                        await message.AddReactionsAsync(@event.Roles.OrderBy(e => e.SortNumber).Select(r => EmoteHelper.Parse(r.Emote)).ToArray());
                        break;
                    case Event.EventParticipactionType.Detailed:
                        break;
                    default:
                        throw new Exception("Event type in not implemented.");
                }
            }
            
            [Command("close")]
            [Summary("Closes event registration.")]
            public async Task EventClose(
            [Summary("Event to close")] Event @event = null)
            {
                if (@event == null)
                    @event = _events.FindEventBy(Context.Guild);
                if (@event == null)
                    throw new Exception("No events were found for this guild.");
                if (!@event.Active)
                    throw new Exception("This event is finalized. Please make a new event.");
                if (@event.MessageId == 0)
                    throw new Exception("This event is already closed for registration.");

                @event.MessageId = 0;
                @event.MessageChannelId = 0;
                await _database.SaveChangesAsync();
                await ReplyAsync($"Event `{@event.Id}` registration has been closed, it's registration message will now be normal message.");
            }

            [Command("finalize")]
            [Summary("Archives event and reverts all role additions. This is irreversable.")]
            public async Task EventFinilize(
            [Summary("Event to finilize")] Event @event = null)
            {
                if (@event == null)
                    @event = _events.FindEventBy(Context.Guild);
                if (@event == null)
                    throw new Exception("No events were found for this guild.");
                if (!@event.Active)
                    throw new Exception("This event is already finalized.");

                @event.Active = false;
                await _database.SaveChangesAsync();
                
                await ReplyAsync($"Event `{@event.Id}` has been finilized. Removing participant roles...");
                if (@event.Guild.ParticipantRoleId != 0)
                    foreach (var participant in @event.Participants)
                    {
                        var user = Context.Guild.GetUser(participant.UserId);
                        await user.RemoveRoleAsync(Context.Guild.GetRole(@event.Guild.ParticipantRoleId));
                    }
                await ReplyAsync($"Everyone's roles have been removed. I hope it was fun!");
            }

            [Command("list")]
            [Summary("Lists all prevous events that took on this server.")]
            public async Task EventArchive()
            {
                var guildEvents = _database.Events.Where(e => e.GuildId == Context.Guild.Id).OrderBy(e => e.Opened).ToList();
                if (guildEvents.Count() == 0)
                    throw new Exception("There are no events that roon on this server.");

                var pagedEvents = guildEvents
                    .Select((e, i) => new { Event = e, Index = i })
                    .GroupBy(o => o.Index / 6)
                    .Select(g => g.Select(o => o.Event));
                var pager = new PaginatedMessage()
                {
                    Title = "List al all prevous events.",
                    Color = Color.Blue,
                    Options = new PaginatedAppearanceOptions()
                    {
                        Timeout = new TimeSpan(0, 3, 0),
                        DisplayInformationIcon = false,
                        JumpDisplayOptions = JumpDisplayOptions.Never
                    }
                };
                pager.Pages = pagedEvents.Select(eg =>
                    string.Join("\r\n", eg.Select(e =>
                        $"`{e.Id}` **{e.Title}** {(e.Active ? "✅" : "❌")}\r\n" +
                        $"Opened at {e.Opened.ToShortDateString()} {e.Opened.ToShortTimeString()}"
                        ))
                );
                await PagedReplyAsync(pager);
            }

            [Command("participant add")]
            [Summary("Add user to event role. Acts like join command.")]
            public async Task EventParticipantAdd(
            [Summary("User id or mention")] IUser user,
            [Summary("Role emote or role id to join.")] string emoteOrId,
            [Summary("Extra information that migth be needed by organizers.")] string extraInformation = null,
            [Summary("Optional event ID for joining event that is not most recent one.")] Event @event = null)
            {
                EventRole er;
                if (!(user is SocketGuildUser guildUser))
                    throw new Exception("This command must be executed inside guild.");
                if (@event == null)
                    @event = _events.FindEventBy(Context.Guild);
                if (@event == null & !(int.TryParse(emoteOrId, out int roleId)))
                    throw new Exception("Unable to locate any events for this guild.");
                else if (@event == null || roleId != 0)
                    er = _database.EventRoles.FirstOrDefault(r => r.Id == roleId);
                else
                    er = @event.Roles.FirstOrDefault(r => r.Emote == emoteOrId);
                if (er == null)
                    throw new ArgumentException("Invalid emote or event id specified");
                if (!er.Event.Active)
                    throw new Exception("This event is finalized. Please make a new event.");

                await _events.TryJoinEvent(guildUser, er, extraInformation, false);
                await Context.Message.DeleteAsync(); // Protect somewhat sensitive data.
            }

            [Command("participant remove")]
            [Summary("Remove participant from event role.")]
            public async Task EventParticipantRemove(
            [Summary("User that is participanting id or mention")] IUser user,
            [Summary("Event to romove participant from")] Event @event = null)
            {
                if (@event == null)
                    @event = _events.FindEventBy(Context.Guild);
                if (@event == null)
                    throw new Exception("No events were found for this guild.");
                if (!@event.Active)
                    throw new Exception("This event is finalized. Please make a new event.");

                if (!(user is IGuildUser guildUser))
                    throw new Exception("This command must be executed inside guild.");

                var participant = @event.Participants.FirstOrDefault(p => p.UserId == guildUser.Id);
                _database.Remove(participant);
                var embed = new EmbedBuilder()
                    .WithTitle($"{user} been removed from event `{@event.Title}`, by {Context.User}")
                    .WithDescription($"They were in `{participant.Role.Title}` role")
                    .WithColor(Color.Red);
                if (participant.UserData != null)
                    embed.AddField("Provided details", $"`{participant.UserData}`");

                await _database.SaveChangesAsync();
                await _events.UpdateEventMessage(@event);
                if (@event.Guild.EventRoleConfirmationChannelId != 0)
                    await (await ((IGuild)Context.Guild).GetTextChannelAsync(@event.Guild.EventRoleConfirmationChannelId)).SendMessageAsync(embed: embed.Build());
                if (@event.Guild.ParticipantRoleId != 0)
                    await guildUser.RemoveRoleAsync(Context.Guild.GetRole(@event.Guild.ParticipantRoleId));
            }
        }
    }
}
