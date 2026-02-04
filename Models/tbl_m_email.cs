using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_m_email")]
	public class tbl_m_email
	{
		[Key]
		[Column("id")]
		[StringLength(50)]
		[DatabaseGenerated(DatabaseGeneratedOption.None)] // Karena tidak ada IDENTITY di tabel
		public string? id { get; set; } = Guid.NewGuid().ToString();

		[Column("departemen")]
		[StringLength(50)]
		public string? departemen { get; set; }

		[Column("email")]
		public string? email { get; set; } // varchar(max) -> string
		[Column("cc")]
		public string? cc { get; set; } 

		[Column("insert_by")]
		[StringLength(10)]
		public string? insert_by { get; set; }

		[Column("ip")]
		[StringLength(10)]
		public string? ip { get; set; }

		[Column("created_at")]
		[StringLength(10)] // nchar(10) — masih string karena bukan datetime
		public DateTime? created_at { get; set; }

		[Column("updated_at")]
		[StringLength(10)]
		public DateTime? updated_at { get; set; }
	}
}
