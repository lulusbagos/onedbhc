using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_t_karyawan_emergency_history")]
	public class tbl_t_karyawan_emergency_history
	{
		[Key]
		public Guid id { get; set; } = Guid.NewGuid();

		[Required]
		public Guid master_karyawan_id { get; set; }

		[StringLength(150)]
		public string? nama_kontak_darurat { get; set; }

		[StringLength(100)]
		public string? relasi_kontak_darurat { get; set; }

		[StringLength(30)]
		public string? hp_kontak_darurat_1 { get; set; }

		[StringLength(30)]
		public string? hp_kontak_darurat_2 { get; set; }

		public int version_no { get; set; } = 1;

		public DateTime created_at { get; set; } = DateTime.UtcNow;

		[StringLength(100)]
		public string? created_by { get; set; }
	}
}
