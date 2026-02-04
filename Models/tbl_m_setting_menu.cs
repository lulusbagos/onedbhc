using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_m_setting_menu")]
	public class tbl_m_setting_menu
	{
		[Key]
		[Column("id")]
		[StringLength(50)]
		[DatabaseGenerated(DatabaseGeneratedOption.None)] // karena kolom id bukan IDENTITY di DB
		public string id { get; set; } = Guid.NewGuid().ToString();

		[Column("awal")]
		public DateTime? awal { get; set; }

		[Column("akhir")]
		public DateTime? akhir { get; set; }

		[Column("insert_by")]
		[StringLength(50)]
		public string? insert_by { get; set; }

		[Column("ip")]
		[StringLength(50)]
		public string? ip { get; set; }

		[Column("created_at")]
		[StringLength(50)] 
		public string? created_at { get; set; }

		[Column("updated_at")]
		[StringLength(50)]
		public string? updated_at { get; set; }
	}
}
