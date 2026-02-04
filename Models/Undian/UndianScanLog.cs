using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models.Undian
{
	[Table("tbl_u_scan_log")]
	public class UndianScanLog
	{
		[Key]
		[Column("id")]
		public Guid id { get; set; }

		[Column("no_nik")]
		[StringLength(50)]
		public string no_nik { get; set; } = string.Empty;

		[Column("scanned_at")]
		public DateTime scanned_at { get; set; } = DateTime.UtcNow;
	}
}
