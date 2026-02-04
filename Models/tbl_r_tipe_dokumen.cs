using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_r_tipe_dokumen")]
	public class tbl_r_tipe_dokumen
	{
		[Key]
		public Guid id { get; set; } = Guid.NewGuid();

		[Required]
		[StringLength(100)]
		public string nama_tipe_dokumen { get; set; }
	}
}

