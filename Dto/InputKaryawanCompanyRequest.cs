using System;
using System.ComponentModel.DataAnnotations;

namespace one_db.Dto
{
	public class InputKaryawanCompanyRequest
	{
		[StringLength(30)]
		public string? kode_company { get; set; }

		[Required]
		[StringLength(150)]
		public string nama_company { get; set; } = string.Empty;

		[Required]
		[StringLength(30)]
		public string kode_level { get; set; } = string.Empty;

		public Guid? parent_company_id { get; set; }

		[StringLength(200)]
		public string? address { get; set; }

		[StringLength(50)]
		public string? contact_person { get; set; }

		[StringLength(30)]
		public string? contact_phone { get; set; }

		[StringLength(200)]
		public string? notes { get; set; }
	}
}
