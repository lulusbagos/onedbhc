using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_t_karyawan_dokumen_history")]
	public class tbl_t_karyawan_dokumen_history
	{
		[Key]
		public Guid id { get; set; } = Guid.NewGuid();

		[Required]
		public Guid master_karyawan_id { get; set; }

		[StringLength(80)]
		public string? kode_dokumen { get; set; }

		[StringLength(200)]
		public string? nama_dokumen { get; set; }

		[StringLength(250)]
		public string? path_file { get; set; }

		[StringLength(200)]
		public string? keterangan { get; set; }

		public DateTime created_at { get; set; } = DateTime.UtcNow;

		[StringLength(100)]
		public string? created_by { get; set; }
	}
}
