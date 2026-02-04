using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_t_karyawan_keluarga_history")]
	public class tbl_t_karyawan_keluarga_history
	{
		[Key]
		public Guid id { get; set; } = Guid.NewGuid();

		[Required]
		public Guid master_karyawan_id { get; set; }

		[StringLength(100)]
		public string? hubungan { get; set; }

		[StringLength(150)]
		public string? nama_lengkap { get; set; }

		public DateTime? tanggal_lahir { get; set; }

		[StringLength(100)]
		public string? pekerjaan { get; set; }

		[StringLength(30)]
		public string? nomor_hp { get; set; }

		public bool is_tanggungan { get; set; }

		public int version_no { get; set; } = 1;

		public DateTime created_at { get; set; } = DateTime.UtcNow;

		[StringLength(100)]
		public string? created_by { get; set; }
	}
}
