using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_r_dokumen_mitra")]
	public class tbl_r_dokumen_mitra
	{
		[Key]
		public int id { get; set; }

		public int id_mitra_pengajuan { get; set; }

		public int id_dokumen_master { get; set; }

		[Required]
		[StringLength(50)]
		public string status_dokumen { get; set; } // 'Wajib Diunggah', 'Sudah Diunggah', 'Disetujui', 'Ditolak'

		public string? file_path { get; set; }

		[StringLength(255)]
		public string? file_name_original { get; set; }

		public string? catatan_review { get; set; }

		public DateTime? uploaded_at { get; set; }

		[StringLength(50)]
		public string? review_by { get; set; }

		public DateTime? review_at { get; set; }

		// Properti Navigasi (Opsional tapi direkomendasikan untuk EF Core)
		[ForeignKey("id_mitra_pengajuan")]
		public virtual tbl_r_mitra_pengajuan? MitraPengajuan { get; set; }

		[ForeignKey("id_dokumen_master")]
		public virtual tbl_m_dokumen_kepatuhan? DokumenMaster { get; set; }
	}
}

