using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("vw_m_karyawan_indexim")]
	public class vw_m_karyawan_indexim
	{
		[Key]
		[Column("id")]
		public string id { get; set; }

		[Column("id_personal")]
		public string? id_personal { get; set; }

		[Column("no_nik")]
		public string? no_nik { get; set; }

		[Column("nama_lengkap")]
		public string? nama_lengkap { get; set; }

		[Column("alamat_ktp")]
		public string? alamat_ktp { get; set; }

		[Column("depart")]
		public string? depart { get; set; }

		[Column("kd_depart")]
		public string? kd_depart { get; set; }

		[Column("posisi")]
		public string? posisi { get; set; }

		[Column("section")]
		public string? section { get; set; }

		[Column("level")]
		public string? level { get; set; }

		[Column("nama_perusahaan")]
		public string? nama_perusahaan { get; set; }

		[Column("email_kantor")]
		public string? email_kantor { get; set; }

		[Column("doh")]
		public DateTime? doh { get; set; }
		[Column("poh")]
		public string? poh { get; set; }

		[Column("lokterima")]
		public string? lokterima { get; set; }

		[Column("nominal")]
		public string? nominal { get; set; }
		
		[Column("tgl_buat")]
		public DateTime? tgl_buat { get; set; }

		[Column("url_foto")]
		public string? url_foto { get; set; }

		[Column("tgl_aktif")]
		public DateTime? tgl_aktif { get; set; }
	}
}
