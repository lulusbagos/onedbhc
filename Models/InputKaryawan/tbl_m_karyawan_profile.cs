using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_m_karyawan_profile")]
	public class tbl_m_karyawan_profile
	{
		[Key]
		public Guid id { get; set; } = Guid.NewGuid();

		[Required]
		[StringLength(30)]
		public string indexim_id { get; set; } = string.Empty;

		[Required]
		[StringLength(30)]
		public string nrp { get; set; } = string.Empty;

		[StringLength(30)]
		public string? nik { get; set; }

		[StringLength(30)]
		public string? kewarganegaraan { get; set; }

		[StringLength(30)]
		public string? no_identitas { get; set; }

		[StringLength(16)]
		public string? no_kk { get; set; }

		[Required]
		[StringLength(150)]
		public string nama_lengkap { get; set; } = string.Empty;

		[StringLength(150)]
		public string? tempat_lahir { get; set; }

		public DateTime? tanggal_lahir { get; set; }

		[StringLength(5)]
		public string? jenis_kelamin { get; set; }

		[StringLength(5)]
		public string? gol_darah { get; set; }

		[StringLength(50)]
		public string? agama { get; set; }

		[StringLength(150)]
		public string? email { get; set; }

		[StringLength(30)]
		public string? nomor_hp { get; set; }

		[StringLength(100)]
		public string? jabatan { get; set; }

		public Guid? owner_company_id { get; set; }
		public Guid? main_contractor_company_id { get; set; }
		public Guid? sub_contractor_company_id { get; set; }
		public Guid? vendor_company_id { get; set; }
		public Guid? submitted_company_id { get; set; }
		public Guid? invite_id { get; set; }

		[StringLength(30)]
		public string? status_karyawan { get; set; }

		public DateTime? tanggal_masuk { get; set; }
		public DateTime? tanggal_selesai { get; set; }

		[StringLength(30)]
		public string? status_residence_id { get; set; }

		public bool status_reciden { get; set; }

		[StringLength(100)]
		public string? nama_ibu_kandung { get; set; }

		[StringLength(20)]
		public string? status_ibu { get; set; }

		[StringLength(100)]
		public string? nama_ayah_kandung { get; set; }

		[StringLength(20)]
		public string? status_ayah { get; set; }

		[StringLength(50)]
		public string? no_bpjskes { get; set; }

		[StringLength(50)]
		public string? no_bpjstk { get; set; }

		[StringLength(30)]
		public string? no_npwp { get; set; }

		[StringLength(30)]
		public string? no_telp_pribadi { get; set; }

		[StringLength(150)]
		public string? email_perusahaan { get; set; }

		[StringLength(150)]
		public string? email_pribadi { get; set; }

		[StringLength(30)]
		public string approval_status { get; set; } = "PendingCompanyApproval";

		public DateTime? company_approved_at { get; set; }

		[StringLength(100)]
		public string? company_approved_by { get; set; }

		public DateTime? main_approved_at { get; set; }

		[StringLength(100)]
		public string? main_approved_by { get; set; }

		public DateTime? finalized_at { get; set; }

		[StringLength(200)]
		public string? catatan { get; set; }

		public bool is_active { get; set; } = true;

		public DateTime created_at { get; set; } = DateTime.UtcNow;

		[StringLength(100)]
		public string? created_by { get; set; }
	}
}
