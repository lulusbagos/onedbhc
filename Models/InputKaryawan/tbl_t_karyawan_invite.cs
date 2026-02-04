using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_t_karyawan_invite")]
	public class tbl_t_karyawan_invite
	{
		[Key]
		public Guid id { get; set; } = Guid.NewGuid();

		[Required]
		[StringLength(120)]
		public string invite_token { get; set; } = Guid.NewGuid().ToString("N");

		public Guid company_id { get; set; }

		[StringLength(30)]
		public string? company_level { get; set; }

		[StringLength(150)]
		public string? recipient_email { get; set; }

		[StringLength(30)]
		public string? nik { get; set; }

		[StringLength(150)]
		public string? nama_lengkap { get; set; }

		[Required]
		[StringLength(30)]
		public string status { get; set; } = "Pending";

		public DateTime created_at { get; set; } = DateTime.UtcNow;

		public DateTime? expires_at { get; set; }

		[StringLength(100)]
		public string? created_by { get; set; }

		public Guid? master_karyawan_id { get; set; }

		public DateTime? completed_at { get; set; }
	}
}
