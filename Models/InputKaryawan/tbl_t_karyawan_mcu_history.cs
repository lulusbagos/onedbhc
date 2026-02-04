using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_t_karyawan_mcu_history")]
	public class tbl_t_karyawan_mcu_history
	{
		[Key]
		public Guid id { get; set; } = Guid.NewGuid();

		[Required]
		public Guid master_karyawan_id { get; set; }

		[StringLength(150)]
		public string? hasil_mcu { get; set; }

		[StringLength(150)]
		public string? fasilitas_kesehatan { get; set; }

		public DateTime? tanggal_mcu { get; set; }

		[StringLength(250)]
		public string? path_file_mcu { get; set; }

		public int version_no { get; set; } = 1;

		public DateTime created_at { get; set; } = DateTime.UtcNow;

		[StringLength(100)]
		public string? created_by { get; set; }
	}
}
