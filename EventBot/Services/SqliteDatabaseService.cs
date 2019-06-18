using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventBot.Services
{
    public class SqliteDatabaseService : DatabaseService
    {
        public SqliteDatabaseService(IServiceProvider services, DbContextOptions options) : base(services, options) { }
        public SqliteDatabaseService(IServiceProvider services) : base(services) {}
        public SqliteDatabaseService() : base() { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlite("Data Source=data.db");
        }
    }
}
