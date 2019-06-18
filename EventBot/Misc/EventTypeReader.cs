using Discord.Commands;
using EventBot.Entities;
using EventBot.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EventBot.Misc
{
    class EventTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var events = services.GetRequiredService<EventManagementService>();
            if (context.Guild == null)
                return Task.FromResult(TypeReaderResult.FromError(CommandError.UnmetPrecondition, "Events are avaivable only inside guild context."));
            Event ev;
            if (input == null)
                ev = events.FindEventBy(context.Guild, true);
            else if (int.TryParse(input, out int id))
                ev = events.FindEventBy(context.Guild, id, true);
            else
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Event id is not a number."));

            return Task.FromResult(TypeReaderResult.FromSuccess(ev));
        }
    }
}
