using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using EventBot.Services;
using Discord.Commands;
using Discord.Addons.Interactive;
using Microsoft.EntityFrameworkCore;

namespace EventBot
{
    class Program
    {
        static void Main(string[] args)
           => new Program().MainAsync().GetAwaiter().GetResult();


        public async Task MainAsync()
        {
            using (var services = ConfigureServices())
            {
                var client = services.GetRequiredService<DiscordSocketClient>();
                client.Log += LogAsync;
                services.GetRequiredService<CommandService>().Log += LogAsync;
                services.GetRequiredService<CommandHandlingService>().Log += LogAsync;

                await services.GetRequiredService<DatabaseService>().InitializeAsync();
                // Tokens should be considered secret data and never hard-coded.
                // We can read from the environment variable to avoid hardcoding.

                await client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("token"));
                await client.StartAsync();

                // Here we initialize the logic required to register our commands.
                await services.GetRequiredService<CommandHandlingService>().InitializeAsync();

                await Task.Delay(-1);
            }
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());

            return Task.CompletedTask;
        }

        private ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(s => new DiscordSocketClient(new DiscordSocketConfig() {
                    LogLevel = LogSeverity.Debug,
                    MessageCacheSize = 1500
                }))
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<EventManagementService>()
                .AddSingleton<DatabaseService>(sp =>
                {
                    if (Environment.GetEnvironmentVariable("dbconnection") != null)
                        return new MySqlDatabaseService(sp);
                    return new SqliteDatabaseService(sp);
                })
                .AddSingleton<InteractiveService>()
                //.AddSingleton<HttpClient>()
                //.AddSingleton<PictureService>()
                .BuildServiceProvider();
        }
    }
}
