using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_t_karyawan_sertifikasi_history")]
	public class tbl_t_karyawan_sertifikasi_history
	{
		[Key]
		public Guid id { get; set; } = Guid.NewGuid();

		[Required]
		public Guid master_karyawan_id { get; set; }

		[StringLength(150)]
		public string? nama_sertifikasi { get; set; }

		[StringLength(150)]
		public string? lembaga_penerbit { get; set; }

		[StringLength(80)]
		public string? nomor_sertifikat { get; set; }

		public DateTime? tanggal_terbit { get; set; }
		public DateTime? tanggal_kadaluarsa { get; set; }

		[StringLength(250)]
		public string? path_file_sertifikat { get; set; }

		public int version_no { get; set; } = 1;

		public DateTime created_at { get; set; } = DateTime.UtcNow;

		[StringLength(100)]
		public string? created_by { get; set; }
	}
}
