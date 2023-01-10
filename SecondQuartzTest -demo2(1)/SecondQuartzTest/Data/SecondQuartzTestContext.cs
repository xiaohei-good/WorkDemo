using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SecondQuartzTest.Models;

namespace SecondQuartzTest.Data
{
    public class SecondQuartzTestContext : DbContext
    {
        public const string TableNamePrefix = "secondquartztest_";
        public const string ConnectionString = nameof(ConnectionString);
        private readonly IConfiguration _configuration;
        //public SecondQuartzTestContext (DbContextOptions<SecondQuartzTestContext> options)
        //    : base(options)
        //{
        //}

        public SecondQuartzTestContext(DbContextOptions<SecondQuartzTestContext> context,
           IConfiguration configuration) : base(context)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(_configuration.GetConnectionString(ConnectionString),
                ServerVersion.AutoDetect(_configuration.GetConnectionString(ConnectionString)));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            string GetTableName<T>() where T : class
            {
                return $"{TableNamePrefix}{typeof(T).Name.ToLower()}";
            }

            base.OnModelCreating(modelBuilder);

            //Schedule
            modelBuilder.Entity<Models.Test>()
                .ToTable(GetTableName<Models.Test>())
                .HasKey(i => i.Id);
           
        }

        public DbSet<Test> Test { get; set; } = default!;
    }
}
