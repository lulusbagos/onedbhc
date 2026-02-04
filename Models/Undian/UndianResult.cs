using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models.Undian
{
	[Table("tbl_u_hasil_pengundian")]
	public class UndianResult
	{
		[Key]
		[Column("id")]
		public Guid id { get; set; }

		[Column("pengundian_id")]
		public Guid pengundian_id { get; set; }

		[Column("hadiah_id")]
		public Guid hadiah_id { get; set; }

		[Column("no_nik")]
		[StringLength(50)]
		public string no_nik { get; set; } = string.Empty;

		[Column("kupon_id")]
		public Guid kupon_id { get; set; }

		[Column("nomor_urut_pemenang")]
		public int nomor_urut_pemenang { get; set; }

		[Column("is_valid")]
		public bool is_valid { get; set; } = true;

		[Column("catatan")]
		[StringLength(500)]
		public string? catatan { get; set; }

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
