using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
	// PERBAIKAN: Model ini sekarang memetakan 1:1 ke SQL View yang baru
	[Table("vw_m_surat_pengajuan")] // Beritahu EF nama view-nya
	public class vw_m_surat_pengajuan
	{
		[Key] // Beritahu EF ini adalah primary key
		[Column("id")]
		public string id { get; set; }

		// 🧍 Informasi Karyawan (Sudah diprioritaskan oleh SQL View)
		[Column("nama_lengkap")]
		public string? nama_lengkap { get; set; }
		[Column("depart")]
		public string? depart { get; set; }
		[Column("posisi")]
		public string? posisi { get; set; }
		[Column("doh")]
		public DateTime? doh { get; set; }
		[Column("level")]
		public int? level { get; set; } // Sesuai SQL k.level

		// 📝 Data Pengajuan
		[Column("nik")]
		public string? nik { get; set; }
		[Column("jenis_pengajuan")]
		public string? jenis_pengajuan { get; set; }
		[Column("nomor")]
		public string? nomor { get; set; }
		[Column("status_karyawan")]
		public string? status_karyawan { get; set; }
		[Column("tanggal_pengajuan")]
		public DateTime? tanggal_pengajuan { get; set; }
		[Column("status")]
		public string? status { get; set; }
		[Column("keperluan")]
		public string? keperluan { get; set; }

		// 💍 Pernikahan
		[Column("nama_pasangan")]
		public string? nama_pasangan { get; set; }
		[Column("pernah_klaim_pernikahan")]
		public string? pernah_klaim_pernikahan { get; set; }
		[Column("tgl_menikah")]
		public DateTime? tgl_menikah { get; set; }

		// 🕊️ Duka
		[Column("nama_almarhum")]
		public string? nama_almarhum { get; set; }
		[Column("hubungan_keluarga")]
		public string? hubungan_keluarga { get; set; }
		[Column("tgl_meninggal")]
		public DateTime? tgl_meninggal { get; set; }
		[Column("status_karyawan_saat_duka")]
		public string? status_karyawan_saat_duka { get; set; }
		[Column("status_hris")]
		public string? status_hris { get; set; }
		[Column("pernah_klaim_duka")]
		public string? pernah_klaim_duka { get; set; }
		[Column("tgl_klaim_duka_sebelumnya")]
		public DateTime? tgl_klaim_duka_sebelumnya { get; set; }

		// 📎 Dokumen
		[Column("file_path_buku_nikah")]
		public string? file_path_buku_nikah { get; set; }
		[Column("file_path_ktp_pernikahan")]
		public string? file_path_ktp_pernikahan { get; set; }
		[Column("file_path_surat_kematian")]
		public string? file_path_surat_kematian { get; set; }
		[Column("file_path_ktp_duka")]
		public string? file_path_ktp_duka { get; set; }
		[Column("file_path_kk_duka")]
		public string? file_path_kk_duka { get; set; }
		[Column("file_path_other_duka")]
		public string? file_path_other_duka { get; set; }
		[Column("file_path")]
		public string? file_path { get; set; }

		// 📌 Meta
		[Column("remarks")]
		public string? remarks { get; set; }
		[Column("insert_by")]
		public string? insert_by { get; set; }
		[Column("ip")]
		public string? ip { get; set; }
		[Column("created_at")]
		public DateTime? created_at { get; set; }
		[Column("updated_at")]
		public DateTime? updated_at { get; set; }
		[Column("notifikasi")]
		public string? notifikasi { get; set; }

		// ✏️ Kolom Editan Mentah (untuk form Admin)
		[Column("nama_lengkap_edit")]
		public string? nama_lengkap_edit { get; set; }
		[Column("posisi_edit")]
		public string? posisi_edit { get; set; }
		[Column("depart_edit")]
		public string? depart_edit { get; set; }
		[Column("doh_edit")]
		public DateTime? doh_edit { get; set; }
	}
}