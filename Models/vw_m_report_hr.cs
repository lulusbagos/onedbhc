using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("vw_m_report_hr")]
	public class vw_m_report_hr
	{
		[Key]
		[Column("id_personal")]
		public int id_personal { get; set; }

		[Column("no_ktp")]
		public string? no_ktp { get; set; }

		[Column("nama_lengkap")]
		public string? nama_lengkap { get; set; }

		[Column("tmp_lahir")]
		public string? tmp_lahir { get; set; }

		[Column("tgl_lahir")]
		public DateTime? tgl_lahir { get; set; }

		[Column("alamat_ktp")]
		public string? alamat_ktp { get; set; }

		[Column("rt_ktp")]
		public string? rt_ktp { get; set; }

		[Column("rw_ktp")]
		public string? rw_ktp { get; set; }

		[Column("agama")]
		public string? agama { get; set; }

		[Column("jk")]
		public string? jk { get; set; }

		[Column("stat_nikah")]
		public string? stat_nikah { get; set; }

		[Column("warga_negara")]
		public string? warga_negara { get; set; }

		[Column("no_kk")]
		public string? no_kk { get; set; }

		[Column("no_npwp")]
		public string? no_npwp { get; set; }

		[Column("no_bpjstk")]
		public string? no_bpjstk { get; set; }

		[Column("no_bpjskes")]
		public string? no_bpjskes { get; set; }

		[Column("hp_1")]
		public string? hp_1 { get; set; }

		[Column("hp_2")]
		public string? hp_2 { get; set; }

		[Column("email_pribadi")]
		public string? email_pribadi { get; set; }

		[Column("nama_ibu")]
		public string? nama_ibu { get; set; }

		[Column("stat_ibu")]
		public string? stat_ibu { get; set; }

		[Column("nama_ayah")]
		public string? nama_ayah { get; set; }

		[Column("stat_ayah")]
		public string? stat_ayah { get; set; }

		[Column("no_equity")]
		public string? no_equity { get; set; }

		[Column("pendidikan")]
		public string? pendidikan { get; set; }

		[Column("nama_sekolah")]
		public string? nama_sekolah { get; set; }

		[Column("fakultas")]
		public string? fakultas { get; set; }

		[Column("jurusan")]
		public string? jurusan { get; set; }

		[Column("nama_pemilik")]
		public string? nama_pemilik { get; set; }

		[Column("bank")]
		public string? bank { get; set; }

		[Column("no_rek")]
		public string? no_rek { get; set; }

		[Column("nama_ec")]
		public string? nama_ec { get; set; }

		[Column("relasi_ec")]
		public string? relasi_ec { get; set; }

		[Column("hp_ec")]
		public string? hp_ec { get; set; }

		[Column("hp_ec_2")]
		public string? hp_ec_2 { get; set; }

		[Column("url_pendukung")]
		public string? url_pendukung { get; set; }

		[Column("no_nik")]
		public string? no_nik { get; set; }

		[Column("depart")]
		public string? depart { get; set; }

		[Column("kd_depart")]
		public string? kd_depart { get; set; }

		[Column("posisi")]
		public string? posisi { get; set; }

		[Column("section")]
		public string? section { get; set; }

		[Column("kd_section")]
		public string? kd_section { get; set; }

		[Column("grade")]
		public string? grade { get; set; }

		[Column("klasifikasi")]
		public string? klasifikasi { get; set; }

		[Column("insentif")]
		public string? insentif { get; set; }

		[Column("level")]
		public string? level { get; set; }

		[Column("kd_level")]
		public string? kd_level { get; set; }

		[Column("roster")]
		public string? roster { get; set; }

		[Column("poh")]
		public string? poh { get; set; }

		[Column("lokker")]
		public string? lokker { get; set; }

		[Column("lokterima")]
		public string? lokterima { get; set; }

		[Column("kode_perusahaan")]
		public string? kode_perusahaan { get; set; }

		[Column("nama_perusahaan")]
		public string? nama_perusahaan { get; set; }

		[Column("email_kantor")]
		public string? email_kantor { get; set; }

		[Column("stat_tinggal")]
		public string? stat_tinggal { get; set; }

		[Column("doh")]
		public DateTime? doh { get; set; }

		[Column("tgl_buat")]
		public DateTime? tgl_buat { get; set; }

		[Column("url_foto")]
		public string? url_foto { get; set; }

		[Column("tgl_aktif")]
		public DateTime? tgl_aktif { get; set; }

		[Column("tgl_nonaktif")]
		public DateTime? tgl_nonaktif { get; set; }

		[Column("ket_nonaktif")]
		public string? ket_nonaktif { get; set; }

		[Column("alasan_nonaktif")]
		public string? alasan_nonaktif { get; set; }

		[Column("url_berkas_nonaktif")]
		public string? url_berkas_nonaktif { get; set; }

		[Column("tgl_langgar")]
		public DateTime? tgl_langgar { get; set; }

		[Column("tgl_punishment")]
		public DateTime? tgl_punishment { get; set; }

		[Column("ket_langgar")]
		public string? ket_langgar { get; set; }

		[Column("tgl_akhir_langgar")]
		public DateTime? tgl_akhir_langgar { get; set; }

		[Column("langgar_jenis")]
		public string? langgar_jenis { get; set; }

		[Column("url_langgar")]
		public string? url_langgar { get; set; }
	}
}
