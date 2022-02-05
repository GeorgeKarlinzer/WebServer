using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GameServer.Data
{
    public class ServerDbContext : DbContext
    {
        static string dbPath = OperatingSystem.IsWindows() ? $"{Directory.GetCurrentDirectory()}/server.db"
            : "/home/admin/ubuntu.20.04-x64/server.db";

        public ServerDbContext() 
        : this(new DbContextOptionsBuilder<ServerDbContext>().UseSqlite(@$"DataSource={dbPath}").Options)
        {
        }

        public ServerDbContext(DbContextOptions<ServerDbContext> options)
        : base(options)
        {
        }

        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Word> Words { get; set; }
        public DbSet<GameLog> GameLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.HasKey(e => e.Login);
                entity.Property(e => e.Password);
            });

            modelBuilder.Entity<Word>(entity =>
            {
                entity.HasIndex(e => e.ID);
                entity.Property(e => e.Value);
            });

            modelBuilder.Entity<GameLog>(entity =>
            {
                entity.HasIndex(e => e.ID);
                entity.Property(e => e.Log);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
