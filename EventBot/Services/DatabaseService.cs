using Discord.WebSocket;
using EventBot.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace EventBot.Services
{
    public abstract class DatabaseService: DbContext
    {
        private readonly IServiceProvider _services;
        private readonly DiscordSocketClient _discord;


        public DbSet<GuildConfig> GuildConfigs { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventRole> EventRoles { get; set; }
        public DbSet<EventParticipant> EventParticipants { get; set; }

        public DatabaseService(IServiceProvider services, DbContextOptions options) : base(options)
        {
            _services = services;

            _discord = services.GetRequiredService<DiscordSocketClient>();
            _discord.GuildAvailable += OnGuildAvaivable;
        }
        public DatabaseService(IServiceProvider services) : base()
        {
            _services = services;

            _discord = services.GetRequiredService<DiscordSocketClient>();
            _discord.GuildAvailable += OnGuildAvaivable;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseLazyLoadingProxies();
        }

        public async Task InitializeAsync()
        {
            await Database.MigrateAsync();
        }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Event>().Property(e => e.Type)
                .HasConversion(new EnumToNumberConverter<Event.EventParticipactionType, int>());
        }

        protected async Task OnGuildAvaivable(SocketGuild guild)
        {
            GuildConfig config = default;
            if(await GuildConfigs.CountAsync() != 0)
                config = await GuildConfigs.FirstAsync(g => g.GuildId == guild.Id);
            if(config == null)
            {
                config = new GuildConfig()
                {
                    GuildId = guild.Id
                };
                Add(config);
                await SaveChangesAsync();
            }
        }
    }
}
