using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_r_mitra_pengajuan")]
	public class tbl_r_mitra_pengajuan
	{
		[Key]
		public int id { get; set; }

		[Required]
		[StringLength(255)]
		public string nama_pt { get; set; }

		[StringLength(80)]
		public string? upload_token { get; set; }

		[StringLength(150)]
		public string? pt_owner { get; set; } // Perusahaan induk / kategori

		[StringLength(150)]
		public string? email_pt { get; set; }

		[StringLength(500)]
		public string? review_singkat { get; set; }

		[StringLength(100)]
		public string? provinsi { get; set; }

		[StringLength(100)]
		public string? kabupaten { get; set; }

		[StringLength(100)]
		public string? kecamatan { get; set; }

		[StringLength(100)]
		public string? kelurahan { get; set; }

		[Required]
		[StringLength(50)]
		public string status_pengajuan { get; set; } // 'Menunggu Review', 'Disetujui', 'Ditolak'

		public string? catatan_admin { get; set; }

		public string? remark { get; set; }

		[StringLength(50)]
		public string? insert_by { get; set; }

		public DateTime created_at { get; set; }

		[StringLength(50)]
		public string? review_by { get; set; }

		public DateTime? review_at { get; set; }
	}

}
