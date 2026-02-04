using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_m_dokumen_kepatuhan")]
	public class tbl_m_dokumen_kepatuhan
	{
		[Key]
		public int id { get; set; }

		[Required]
		[StringLength(100)]
		public string grup { get; set; }

		[Required]
		[StringLength(255)]
		public string nama_dokumen { get; set; }

		[StringLength(500)]
		public string? deskripsi { get; set; }

		public bool is_active { get; set; }
	}
}
