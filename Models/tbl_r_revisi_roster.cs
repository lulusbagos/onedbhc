using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("tbl_r_revisi_roster")]
	public class tbl_r_revisi_roster
	{
		[Key]
		[Column("id")]
		[StringLength(50)]
		[DatabaseGenerated(DatabaseGeneratedOption.None)]
		public string id { get; set; } = Guid.NewGuid().ToString();

		[Column("nik")]
		[StringLength(50)]
		public string? nik { get; set; }

		[Column("tanggal_roster")]
		public DateTime? tanggal_roster { get; set; }

		[Column("tanggal_awal")]
		public DateTime? tanggal_awal { get; set; }

		[Column("tanggal_akhir")]
		public DateTime? tanggal_akhir { get; set; }

		[Column("status_awal")]
		[StringLength(50)]
		public string? status_awal { get; set; }

		[Column("status_baru")]
		[StringLength(50)]
		public string? status_baru { get; set; }

		[Column("segments", TypeName = "nvarchar(max)")]
		public string? segments { get; set; }

		[Column("status")]
		[StringLength(50)]
		public string? status { get; set; }

		[Column("remarks")]
		public string? remarks { get; set; }

		[Column("file_path")]
		public string? file_path { get; set; }

		[Column("insert_by")]
		[StringLength(50)]
		public string? insert_by { get; set; }

		[Column("ip")]
		[StringLength(50)]
		public string? ip { get; set; }

		[Column("created_at")]
		public DateTime? created_at { get; set; } // Ubah ke DateTime?

		[Column("updated_at")]

		public DateTime? updated_at { get; set; } // Ubah ke DateTime?
	}
}
