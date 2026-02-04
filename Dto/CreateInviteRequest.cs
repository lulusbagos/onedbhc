using System;
using System.ComponentModel.DataAnnotations;

namespace one_db.Dto
{
	public class CreateInviteRequest
	{
		[Required]
		public Guid company_id { get; set; }

		[StringLength(30)]
		public string? nik { get; set; }

		[StringLength(150)]
		public string? nama_lengkap { get; set; }

		[StringLength(150)]
		public string? email { get; set; }

		public DateTime? expires_at { get; set; }
	}
}
