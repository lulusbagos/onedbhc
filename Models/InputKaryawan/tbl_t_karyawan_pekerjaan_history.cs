using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_t_karyawan_pekerjaan_history")]
	public class tbl_t_karyawan_pekerjaan_history
	{
		[Key]
		public Guid id { get; set; } = Guid.NewGuid();

		[Required]
		public Guid master_karyawan_id { get; set; }

		public Guid? perusahaan_id { get; set; }
		public int? departemen_id { get; set; }
		public int? section_id { get; set; }
		public int? posisi_id { get; set; }

		[StringLength(50)]
		public string? job_level { get; set; }

		[StringLength(50)]
		public string? job_grade { get; set; }

		[StringLength(50)]
		public string? roster_code { get; set; }

		[StringLength(30)]
		public string? nik { get; set; }

		public DateTime? doh { get; set; }
		public DateTime? poh { get; set; }
		public DateTime? tanggal_aktif { get; set; }
		public DateTime? tanggal_resign { get; set; }

		[StringLength(30)]
		public string? lokasi_kerja_kode { get; set; }

		[StringLength(150)]
		public string? lokasi_kerja_text { get; set; }

		[StringLength(150)]
		public string? lokasi_terima_text { get; set; }

		[StringLength(50)]
		public string? status_residence { get; set; }

		public bool status_reciden { get; set; }

		public DateTime created_at { get; set; } = DateTime.UtcNow;

		[StringLength(100)]
		public string? created_by { get; set; }
	}
}
