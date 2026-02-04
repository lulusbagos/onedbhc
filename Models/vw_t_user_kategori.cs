using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("vw_t_user_kategori")]
	public class vw_t_user_kategori
	{
		[Key]
		[Column("nrp")]
		[StringLength(50)]
		public string Nrp { get; set; } = null!;

		[Column("nama")]
		[StringLength(100)]
		public string? nama { get; set; }

		[Column("kategori_user_id")]
		public string? kategori_user_id { get; set; }

		[Column("dept_code")]
		[StringLength(50)]
		public string? dept_code { get; set; }

		[Column("comp_code")]
		[StringLength(50)]
		public string? comp_code { get; set; }

		[Column("login_controller")]
		[StringLength(100)]
		public string? login_controller { get; set; }

		[Column("login_function")]
		[StringLength(100)]
		public string? login_function { get; set; }
	}
}
