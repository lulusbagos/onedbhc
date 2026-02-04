using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_t_karyawan_alamat_history")]
	public class tbl_t_karyawan_alamat_history
	{
		[Key]
		public Guid id { get; set; } = Guid.NewGuid();

		[Required]
		public Guid master_karyawan_id { get; set; }

		[Required]
		[StringLength(30)]
		public string jenis_alamat { get; set; } = "KTP";

		[StringLength(200)]
		public string? alamat { get; set; }

		[StringLength(10)]
		public string? rt { get; set; }

		[StringLength(10)]
		public string? rw { get; set; }

		[StringLength(150)]
		public string? provinsi { get; set; }

		[StringLength(150)]
		public string? kota { get; set; }

		[StringLength(150)]
		public string? kecamatan { get; set; }

		[StringLength(150)]
		public string? kelurahan { get; set; }

		[StringLength(10)]
		public string? kode_pos { get; set; }

		public bool is_current { get; set; } = true;

		public int version_no { get; set; } = 1;

		public DateTime created_at { get; set; } = DateTime.UtcNow;

		[StringLength(100)]
		public string? created_by { get; set; }
	}
}
