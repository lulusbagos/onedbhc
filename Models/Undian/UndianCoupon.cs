using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models.Undian
{
	[Table("tbl_u_kupon")]
	public class UndianCoupon
	{
		[Key]
		[Column("id")]
		public Guid id { get; set; }

		[Column("no_nik")]
		[StringLength(50)]
		public string no_nik { get; set; } = string.Empty;

		[Column("kode_kupon")]
		[StringLength(100)]
		public string kode_kupon { get; set; } = string.Empty;

		[Column("periode")]
		[StringLength(50)]
		public string? periode { get; set; }

		[Column("status")]
		[StringLength(25)]
		public string status { get; set; } = "available";

		[Column("keterangan")]
		[StringLength(255)]
		public string? keterangan { get; set; }

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
