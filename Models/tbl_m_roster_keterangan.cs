using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_m_roster_keterangan")]
	public class tbl_m_roster_keterangan
	{
		[Key]
		[Column("id")]
		[StringLength(50)]
		public string? id { get; set; }

		[Column("kode")]
		[StringLength(50)]
		public string? kode { get; set; }

		[Column("keterangan")]
		[StringLength(50)]
		public string? keterangan { get; set; }

		[Column("warna")]
		[StringLength(50)]
		public string? warna { get; set; }

		[Column("created_at")]
		public DateTime? created_at { get; set; }

		[Column("created_by")]
		[StringLength(50)]
		public string? created_by { get; set; }

		[Column("updated_at")]
		public DateTime? updated_at { get; set; }

		[Column("updated_by")]
		[StringLength(50)]
		public string? updated_by { get; set; }

		[Column("ip")]
		[StringLength(50)]
		public string? ip { get; set; }
	}
}
