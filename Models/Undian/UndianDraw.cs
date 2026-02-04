using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models.Undian
{
	[Table("tbl_u_pengundian")]
	public class UndianDraw
	{
		[Key]
		[Column("id")]
		public Guid id { get; set; }

		[Column("nama_event")]
		[StringLength(200)]
		public string nama_event { get; set; } = string.Empty;

		[Column("tanggal_event")]
		public DateTime? tanggal_event { get; set; }

		[Column("keterangan")]
		[StringLength(500)]
		public string? keterangan { get; set; }

		[Column("status")]
		[StringLength(25)]
		public string status { get; set; } = "draft";

		[Column("total_peserta")]
		public int total_peserta { get; set; }

		[Column("total_kupon")]
		public int total_kupon { get; set; }

		[Column("total_pemenang")]
		public int total_pemenang { get; set; }

		[Column("created_by")]
		[StringLength(128)]
		public string? created_by { get; set; }

		[Column("created_at")]
		public DateTime created_at { get; set; } = DateTime.UtcNow;

		[Column("created_ip")]
		[StringLength(64)]
		public string? created_ip { get; set; }

		[Column("updated_by")]
		[StringLength(128)]
		public string? updated_by { get; set; }

		[Column("updated_at")]
		public DateTime? updated_at { get; set; }

		[Column("updated_ip")]
		[StringLength(64)]
		public string? updated_ip { get; set; }
	}
}
