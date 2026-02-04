using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_t_karyawan_pendidikan_history")]
	public class tbl_t_karyawan_pendidikan_history
	{
		[Key]
		public Guid id { get; set; } = Guid.NewGuid();

		[Required]
		public Guid master_karyawan_id { get; set; }

		public Guid? pendidikan_id { get; set; }

		[StringLength(150)]
		public string? nama_instansi { get; set; }

		[StringLength(150)]
		public string? jurusan { get; set; }

		[StringLength(10)]
		public string? tahun_masuk { get; set; }

		[StringLength(10)]
		public string? tahun_lulus { get; set; }

		[StringLength(250)]
		public string? keterangan { get; set; }

		[StringLength(250)]
		public string? path_dokumen { get; set; }

		public int version_no { get; set; } = 1;

		public DateTime created_at { get; set; } = DateTime.UtcNow;

		[StringLength(100)]
		public string? created_by { get; set; }
	}
}
