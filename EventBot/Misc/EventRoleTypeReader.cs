using Discord.Commands;
using EventBot.Entities;
using EventBot.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventBot.Misc
{
    class EventRoleTypeReader : TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            var database = services.GetRequiredService<DatabaseService>();
            if (context.Guild == null)
                return Task.FromResult(TypeReaderResult.FromError(CommandError.UnmetPrecondition, "Event roles are available only inside a discord server."));
            EventRole er = null;
            if (int.TryParse(input, out int id))
                er = database.EventRoles.FirstOrDefault(r => r.Id == id);
            else
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Event role ID is not a valid number."));
            if(er == null)
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ObjectNotFound, "Specified event role was not found."));
            if(er.Event?.GuildId != context.Guild.Id)
                return Task.FromResult(TypeReaderResult.FromError(CommandError.Exception, "Cross server event role access is denied."));
            return Task.FromResult(TypeReaderResult.FromSuccess(er));
        }
    }
}
