using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Classes
{
    public class AppDbContext : DbContext
    {
        public DbSet<Client> Clients { get; set; }
        public AppDbContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("server=127.0.0.1;database=pr5;port=3307;user=root;password=;", new MySqlServerVersion(new Version(8, 0, 11)));
        }
    }
}
