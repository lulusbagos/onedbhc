using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_r_pendidikan")]
	public class tbl_r_pendidikan
	{
		[Key]
		public Guid id { get; set; } = Guid.NewGuid();

		[Required]
		[StringLength(100)]
		public string nama_pendidikan { get; set; } = string.Empty;

		public int urutan { get; set; } = 1;

		public bool is_active { get; set; } = true;
	}
}
