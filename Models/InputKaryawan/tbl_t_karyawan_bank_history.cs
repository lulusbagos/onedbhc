using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_t_karyawan_bank_history")]
	public class tbl_t_karyawan_bank_history
	{
		[Key]
		public Guid id { get; set; } = Guid.NewGuid();

		[Required]
		public Guid master_karyawan_id { get; set; }

		[StringLength(150)]
		public string? nama_bank { get; set; }

		[StringLength(150)]
		public string? nama_pemilik_rekening { get; set; }

		[StringLength(50)]
		public string? nomor_rekening { get; set; }

		public bool is_active { get; set; } = true;

		public int version_no { get; set; } = 1;

		public DateTime created_at { get; set; } = DateTime.UtcNow;

		[StringLength(100)]
		public string? created_by { get; set; }
	}
}
