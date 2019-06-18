using Discord.Commands;
using EventBot.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EventBot.Modules
{
    public class TestingModule: ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        [Summary("Test if bot is working.")]
        [NoHelp]
        public Task SayAsync()
        => ReplyAsync("Pong!");
    }
}
