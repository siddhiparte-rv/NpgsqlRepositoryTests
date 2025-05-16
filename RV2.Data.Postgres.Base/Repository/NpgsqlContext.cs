using Microsoft.EntityFrameworkCore;
using RV2.Data.Postgres.Base.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RV2.Data.Postgres.Base.Repository
{
    public class NpgsqlContext : DbContext
    {
        protected readonly string _connectionString;

        public NpgsqlContext(string connectionString)
          : base()
        {
            _connectionString = connectionString;
        }

        public NpgsqlContext(DbContextOptions dbContextOptions)
          : base(dbContextOptions)
        {

        }




        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!string.IsNullOrEmpty(_connectionString))
            {
                optionsBuilder.UseNpgsql(_connectionString);
            }
        }

    }
}
