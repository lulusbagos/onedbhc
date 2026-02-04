using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	[Table("vw_m_karyawan")]
	public class vw_m_karyawan
	{
		[Key]
		[Column("id")]
		public string id { get; set; }

		[Column("id_personal")]
		public string? id_personal { get; set; }

		[Column("no_ktp")]
		[StringLength(50)]
		public string? no_ktp { get; set; }

		[Column("nama_lengkap")]
		[StringLength(200)]
		public string? nama_lengkap { get; set; }

		[Column("tmp_lahir")]
		[StringLength(100)]
		public string? tmp_lahir { get; set; }

		[Column("tgl_lahir")]
		public DateTime? tgl_lahir { get; set; }

		[Column("alamat_ktp")]
		public string? alamat_ktp { get; set; }

		[Column("rt_ktp")]
		[StringLength(10)]
		public string? rt_ktp { get; set; }

		[Column("rw_ktp")]
		[StringLength(10)]
		public string? rw_ktp { get; set; }

		[Column("agama")]
		[StringLength(50)]
		public string? agama { get; set; }

		[Column("jk")]
		[StringLength(10)]
		public string? jk { get; set; }

		[Column("stat_nikah")]
		[StringLength(50)]
		public string? stat_nikah { get; set; }

		[Column("warga_negara")]
		[StringLength(50)]
		public string? warga_negara { get; set; }

		[Column("no_kk")]
		[StringLength(50)]
		public string? no_kk { get; set; }

		[Column("no_npwp")]
		[StringLength(50)]
		public string? no_npwp { get; set; }

		[Column("no_bpjstk")]
		[StringLength(50)]
		public string? no_bpjstk { get; set; }

		[Column("no_bpjskes")]
		[StringLength(50)]
		public string? no_bpjskes { get; set; }

		[Column("hp_1")]
		[StringLength(50)]
		public string? hp_1 { get; set; }

		[Column("hp_2")]
		[StringLength(50)]
		public string? hp_2 { get; set; }

		[Column("email_pribadi")]
		[StringLength(100)]
		public string? email_pribadi { get; set; }

		[Column("nama_ibu")]
		[StringLength(200)]
		public string? nama_ibu { get; set; }

		[Column("stat_ibu")]
		[StringLength(50)]
		public string? stat_ibu { get; set; }

		[Column("nama_ayah")]
		[StringLength(200)]
		public string? nama_ayah { get; set; }

		[Column("stat_ayah")]
		[StringLength(50)]
		public string? stat_ayah { get; set; }

		[Column("no_equity")]
		[StringLength(50)]
		public string? no_equity { get; set; }

		[Column("pendidikan")]
		[StringLength(100)]
		public string? pendidikan { get; set; }

		[Column("nama_sekolah")]
		[StringLength(200)]
		public string? nama_sekolah { get; set; }

		[Column("fakultas")]
		[StringLength(150)]
		public string? fakultas { get; set; }

		[Column("jurusan")]
		[StringLength(150)]
		public string? jurusan { get; set; }

		[Column("nama_pemilik")]
		[StringLength(200)]
		public string? nama_pemilik { get; set; }

		[Column("bank")]
		[StringLength(100)]
		public string? bank { get; set; }

		[Column("no_rek")]
		[StringLength(100)]
		public string? no_rek { get; set; }

		[Column("nama_ec")]
		[StringLength(200)]
		public string? nama_ec { get; set; }

		[Column("relasi_ec")]
		[StringLength(100)]
		public string? relasi_ec { get; set; }

		[Column("hp_ec")]
		[StringLength(50)]
		public string? hp_ec { get; set; }

		[Column("hp_ec_2")]
		[StringLength(50)]
		public string? hp_ec_2 { get; set; }

		[Column("url_pendukung")]
		public string? url_pendukung { get; set; }

		[Column("no_nik")]
		[StringLength(50)]
		public string? no_nik { get; set; }

		[Column("depart")]
		[StringLength(100)]
		public string? depart { get; set; }

		[Column("kd_depart")]
		[StringLength(50)]
		public string? kd_depart { get; set; }

		[Column("posisi")]
		[StringLength(150)]
		public string? posisi { get; set; }

		[Column("section")]
		[StringLength(150)]
		public string? section { get; set; }

		[Column("kd_section")]
		[StringLength(50)]
		public string? kd_section { get; set; }

		[Column("grade")]
		[StringLength(50)]
		public string? grade { get; set; }

		[Column("klasifikasi")]
		[StringLength(100)]
		public string? klasifikasi { get; set; }

		[Column("insentif")]
		[StringLength(100)]
		public string? insentif { get; set; }

		[Column("level")]
		[StringLength(50)]
		public string? level { get; set; }

		[Column("kd_level")]
		[StringLength(50)]
		public string? kd_level { get; set; }

		[Column("roster")]
		[StringLength(50)]
		public string? roster { get; set; }

		[Column("poh")]
		[StringLength(50)]
		public string? poh { get; set; }

		[Column("lokker")]
		[StringLength(100)]
		public string? lokker { get; set; }

		[Column("lokterima")]
		[StringLength(100)]
		public string? lokterima { get; set; }

		[Column("kode_perusahaan")]
		[StringLength(50)]
		public string? kode_perusahaan { get; set; }

		[Column("nama_perusahaan")]
		[StringLength(200)]
		public string? nama_perusahaan { get; set; }

		[Column("jenis_perusahaan")]
		[StringLength(100)]
		public string? jenis_perusahaan { get; set; }

		[Column("id_parent")]
		[StringLength(50)]
		public string? id_parent { get; set; }

		[Column("parent")]
		[StringLength(200)]
		public string? parent { get; set; }

		[Column("kategori_perusahaan")]
		public string? kategori_perusahaan { get; set; }

		[Column("hirarki_perusahaan")]
		public string? hirarki_perusahaan { get; set; }

		[Column("level_hirarki")]
		public string? level_hirarki { get; set; }

		[Column("email_kantor")]
		[StringLength(100)]
		public string? email_kantor { get; set; }

		[Column("stat_tinggal")]
		[StringLength(50)]
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
		[StringLength(255)]
		public string? alasan_nonaktif { get; set; }

		[Column("url_berkas_nonaktif")]
		public string? url_berkas_nonaktif { get; set; }

		[Column("info_pelanggaran")]
		public string? info_pelanggaran { get; set; }

		[Column("tgl_replikasi")]
		public DateTime? tgl_replikasi { get; set; }
	}
}
