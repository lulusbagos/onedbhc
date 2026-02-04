using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_r_status_pernikahan")]
	public class tbl_r_status_pernikahan
	{
		[Key]
		public Guid id { get; set; } = Guid.NewGuid();

		[Required]
		[StringLength(10)]
		public string kode_status { get; set; }

		[Required]
		[StringLength(100)]
		public string nama_status { get; set; }
	}
}

