using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models.Undian
{
	[Table("tbl_u_hadiah")]
	public class UndianPrize
	{
		[Key]
		[Column("id")]
		public Guid id { get; set; }

		[Column("nama_hadiah")]
		[StringLength(200)]
		public string nama_hadiah { get; set; } = string.Empty;

		[Column("deskripsi")]
		[StringLength(500)]
		public string? deskripsi { get; set; }

		[Column("kategori")]
		[StringLength(50)]
		public string kategori { get; set; } = "doorprize";

		[Column("urutan")]
		public int urutan { get; set; } = 1;

		[Column("jumlah_unit")]
		public int jumlah_unit { get; set; } = 1;

		[Column("is_active")]
		public bool is_active { get; set; } = true;

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
