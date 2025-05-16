using System;
using System.ComponentModel.DataAnnotations.Schema;
using RV2.Data.Postgres.Base.Model;

namespace RV2.Data.Postgres.Base.Tests.Models
{
    public class TestEntity : BaseData
    {
        [Column("name")]
        public string Name { get; set; }

        [Column("description")]
        public string Description { get; set; }
    }
}
