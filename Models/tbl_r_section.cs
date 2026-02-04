using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_r_section")]
	public class tbl_r_section
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int id { get; set; }

		[Required]
		public int dept_id { get; set; }

		[StringLength(150)]
		public string? nama_section { get; set; }

		public bool is_active { get; set; } = true;
	}
}
