using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_t_karyawan_company")]
	public class tbl_t_karyawan_company
	{
		[Key]
		public Guid id { get; set; } = Guid.NewGuid();

		[Required]
		public Guid master_karyawan_id { get; set; }

		[Required]
		public Guid company_id { get; set; }

		[Required]
		[StringLength(30)]
		public string role_level { get; set; } = string.Empty;

		public DateTime? start_date { get; set; }

		public DateTime? end_date { get; set; }

		[StringLength(200)]
		public string? keterangan { get; set; }
	}
}
