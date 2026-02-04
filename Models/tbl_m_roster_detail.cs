using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_m_roster_detail")]
	public class tbl_m_roster_detail
	{
		[Key]
		[Column("id")]
		[StringLength(50)]
		public string id { get; set; }

		[Column("nik")]
		[StringLength(50)]
		public string? nik { get; set; }

		[Column("working_date")]
		public DateTime? working_date { get; set; }

		[Column("status")]
		[StringLength(50)]
		public string? status { get; set; }

		[Column("remarks")]
		public string? remarks { get; set; }

		[Column("ip")]
		public string? ip { get; set; }

		[Column("created_at")]
		// [PERBAIKAN]: Atribut [StringLength(50)] dihapus dari sini
		public DateTime? created_at { get; set; }

		[Column("updated_at")]
		// [PERBAIKAN]: Atribut [StringLength(50)] dihapus dari sini
		public DateTime? updated_at { get; set; }
	}
}