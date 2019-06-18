using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventBot.Services
{
    public class MySqlDatabaseService : DatabaseService
    {
        public MySqlDatabaseService(IServiceProvider services, DbContextOptions options) : base(services, options) { }
        public MySqlDatabaseService(IServiceProvider services) : base(services) { }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseMySql(Environment.GetEnvironmentVariable("dbconnection"));
        }
    }
}
