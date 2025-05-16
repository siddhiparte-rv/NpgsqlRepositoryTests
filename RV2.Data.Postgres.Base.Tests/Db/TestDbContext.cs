using Microsoft.EntityFrameworkCore;
using RV2.Data.Postgres.Base.Repository;
using RV2.Data.Postgres.Base.Tests.Models;
using System;
namespace RV2.Data.Postgres.Base.Tests.Db
{
    public class TestDbContext : NpgsqlContext
    {
        public DbSet<TestEntity> TestEntities { get; set; }

        public TestDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TestEntity>(entity =>
            {
                entity.ToTable("test_entities");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Description).HasColumnName("description");
            });
        }
    }
}
