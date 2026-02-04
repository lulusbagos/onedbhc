using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_m_dashboard")]
	public class tbl_m_dashboard
	{
		[Key]
		[Column("id")]
		public Guid id { get; set; }

		[Column("link")]
		public string? link { get; set; }

		[Column("judul")]
		public string? judul { get; set; }

		[Column("durasi")]
		public string? durasi { get; set; }

		[Column("created_at")]
		public DateOnly? created_at { get; set; }
	}
}
