using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[NotMapped] // Karena ini bukan tabel tetap, hanya hasil query custom
	public class CompanyFilter
	{
		[Column("maincon")]
		public string? maincon { get; set; }

		[Column("subcont")]
		public string? subcont { get; set; }
	}
}
