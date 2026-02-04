using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_m_company")]
	public class tbl_m_company
	{
		[Key]
		public Guid id { get; set; } = Guid.NewGuid();

		[StringLength(30)]
		public string? kode_company { get; set; }

		[Required]
		[StringLength(150)]
		public string nama_company { get; set; } = string.Empty;

		[Required]
		[StringLength(30)]
		public string kode_level { get; set; } = "OWNER";

		public Guid? parent_company_id { get; set; }

		[StringLength(200)]
		public string? tree_path { get; set; }

		[StringLength(200)]
		public string? address { get; set; }

		[StringLength(50)]
		public string? contact_person { get; set; }

		[StringLength(30)]
		public string? contact_phone { get; set; }

		[StringLength(100)]
		public string? external_reference { get; set; }

		[StringLength(200)]
		public string? notes { get; set; }

		public bool is_active { get; set; } = true;

		public DateTime created_at { get; set; } = DateTime.UtcNow;

		[StringLength(100)]
		public string? created_by { get; set; }
	}
}
