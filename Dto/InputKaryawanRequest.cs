using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace one_db.Dto
{
	public class InputKaryawanRequest
	{
		public Guid? id { get; set; }

		[Required]
		[StringLength(30)]
		public string indexim_id { get; set; } = string.Empty;

		[Required]
		[StringLength(30)]
		public string nrp { get; set; } = string.Empty;

		[StringLength(30)]
		public string? nik { get; set; }

		[Required]
		[StringLength(150)]
		public string nama_lengkap { get; set; } = string.Empty;

		[StringLength(150)]
		public string? email { get; set; }

		[StringLength(30)]
		public string? nomor_hp { get; set; }

		[StringLength(100)]
		public string? jabatan { get; set; }

		[StringLength(30)]
		public string? status_karyawan { get; set; }

		public DateTime? tanggal_masuk { get; set; }

		public DateTime? tanggal_selesai { get; set; }

		public Guid? owner_company_id { get; set; }
		public Guid? main_contractor_company_id { get; set; }
		public Guid? sub_contractor_company_id { get; set; }
		public Guid? vendor_company_id { get; set; }

		public List<Guid>? additional_company_ids { get; set; }

		[StringLength(200)]
		public string? catatan { get; set; }
	}
}
