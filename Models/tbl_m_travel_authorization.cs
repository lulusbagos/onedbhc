using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_m_travel_authorization")]
	public class tbl_m_travel_authorization
	{
		[Key]
		[Column("id")]
		[StringLength(50)]
		public string id { get; set; } = Guid.NewGuid().ToString();

		[Column("nik")]
		[StringLength(50)]
		[Required]
		public string nik { get; set; } = string.Empty;

		[Column("out_site")]
		[DataType(DataType.Date)]
		public DateTime? out_site { get; set; }

		[Column("on_site")]
		[DataType(DataType.Date)]
		public DateTime? on_site { get; set; }

		[Column("wilayah")]
		[StringLength(50)]
		public string? wilayah { get; set; }

		[Column("poh")]
		[StringLength(50)]
		public string? poh { get; set; }

		[Column("nominal")]
		[StringLength(50)]
		public string? nominal { get; set; }

		[Column("nomor_ta")]
		[StringLength(50)]
		public string? nomor_ta { get; set; }

		[Column("created_at")]
		public DateTime? created_at { get; set; } = DateTime.Now;

		[Column("created_by")]
		[StringLength(50)]
		public string? created_by { get; set; }

		[Column("update_by")]
		[StringLength(50)]
		public string? update_by { get; set; }

		[Column("update_at")]
		public DateTime? update_at { get; set; }
	}
}
