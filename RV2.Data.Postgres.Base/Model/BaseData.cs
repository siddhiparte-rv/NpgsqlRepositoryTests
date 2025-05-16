using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RV2.Data.Postgres.Base.Model
{
    public class BaseData
    {
        public virtual long Id { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; }
        
        [Column("updated_date")]
        public DateTime UpdatedDate { get; set; }

        [Column("updated_by")]
        public string UpdatedBy { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }
    }
}
