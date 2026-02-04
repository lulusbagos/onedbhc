using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_r_company_level")]
	public class tbl_r_company_level
	{
		[Key]
		public Guid id { get; set; } = Guid.NewGuid();

		[Required]
		[StringLength(30)]
		public string kode_level { get; set; } = string.Empty;

		[Required]
		[StringLength(100)]
		public string nama_level { get; set; } = string.Empty;

		public int urutan { get; set; } = 1;

		[StringLength(200)]
		public string? deskripsi { get; set; }

		public bool is_active { get; set; } = true;

		public DateTime created_at { get; set; } = DateTime.UtcNow;

		[StringLength(100)]
		public string? created_by { get; set; }
	}
}
