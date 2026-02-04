using System;
using System.Collections.Generic;

namespace one_db.Dto
{
	public class InputKaryawanWizardPayload
	{
		public ProfilePayload? Profile { get; set; }
		public JobPayload? Job { get; set; }
		public List<AlamatPayload>? Addresses { get; set; }
		public BankPayload? Bank { get; set; }
		public EmergencyContactPayload? EmergencyContact { get; set; }
		public List<KeluargaPayload>? Families { get; set; }
		public List<PendidikanPayload>? Educations { get; set; }
		public List<SertifikasiPayload>? Certifications { get; set; }
		public List<McuPayload>? Mcus { get; set; }
		public List<VaksinPayload>? Vaccines { get; set; }
		public List<DocumentMetaPayload>? Documents { get; set; }
		public string? InviteToken { get; set; }
	}

	public class ProfilePayload
	{
		public string? indexim_id { get; set; }
		public string? kewarganegaraan { get; set; }
		public string? no_identitas { get; set; }
		public string? no_kk { get; set; }
		public string? nama_lengkap { get; set; }
		public string? tempat_lahir { get; set; }
		public DateTime? tanggal_lahir { get; set; }
		public string? jenis_kelamin { get; set; }
		public string? gol_darah { get; set; }
		public string? agama { get; set; }
		public string? status_residence_id { get; set; }
		public bool status_reciden { get; set; }
		public string? no_telp_pribadi { get; set; }
		public string? email_pribadi { get; set; }
		public string? email_perusahaan { get; set; }
		public string? no_bpjskes { get; set; }
		public string? no_bpjstk { get; set; }
		public string? no_npwp { get; set; }
		public string? nama_ibu_kandung { get; set; }
		public string? status_ibu { get; set; }
		public string? nama_ayah_kandung { get; set; }
		public string? status_ayah { get; set; }
	}

	public class JobPayload
	{
		public string? perusahaan_id { get; set; }
		public int? perusahaan_ref_id { get; set; }
		public string? perusahaan_text { get; set; }
		public int? departemen_id { get; set; }
		public int? section_id { get; set; }
		public int? posisi_id { get; set; }
		public string? posisi_text { get; set; }
		public string? job_level { get; set; }
		public string? job_grade { get; set; }
		public string? roster_code { get; set; }
		public string? lokasi_kerja_kode { get; set; }
		public string? lokasi_kerja_text { get; set; }
		public string? lokasi_terima_text { get; set; }
		public string? nik { get; set; }
		public DateTime? doh { get; set; }
		public DateTime? poh { get; set; }
		public DateTime? tanggal_aktif { get; set; }
		public DateTime? tanggal_resign { get; set; }
		public string? status_residence { get; set; }
		public bool status_reciden { get; set; }
	}

	public class AlamatPayload
	{
		public string? jenis_alamat { get; set; }
		public string? alamat { get; set; }
		public string? rt { get; set; }
		public string? rw { get; set; }
		public string? provinsi { get; set; }
		public string? kota { get; set; }
		public string? kecamatan { get; set; }
		public string? kelurahan { get; set; }
		public string? kode_pos { get; set; }
	}

	public class BankPayload
	{
		public string? bank_name { get; set; }
		public string? nama_pemilik_rekening { get; set; }
		public string? nomor_rekening { get; set; }
	}

	public class EmergencyContactPayload
	{
		public string? nama { get; set; }
		public string? relasi { get; set; }
		public string? hp1 { get; set; }
		public string? hp2 { get; set; }
	}

	public class KeluargaPayload
	{
		public string? hubungan { get; set; }
		public string? nama { get; set; }
		public DateTime? tanggal_lahir { get; set; }
		public string? pekerjaan { get; set; }
		public string? nomor_hp { get; set; }
		public bool is_tanggungan { get; set; }
	}

	public class PendidikanPayload
	{
		public Guid? pendidikan_id { get; set; }
		public string? nama_instansi { get; set; }
		public string? jurusan { get; set; }
		public string? tahun_masuk { get; set; }
		public string? tahun_lulus { get; set; }
		public string? keterangan { get; set; }
		public string? file_field { get; set; }
	}

	public class SertifikasiPayload
	{
		public string? nama_sertifikasi { get; set; }
		public string? lembaga { get; set; }
		public string? nomor_sertifikat { get; set; }
		public DateTime? tanggal_terbit { get; set; }
		public DateTime? tanggal_kadaluarsa { get; set; }
		public string? file_field { get; set; }
	}

	public class McuPayload
	{
		public string? hasil { get; set; }
		public string? fasilitas_kesehatan { get; set; }
		public DateTime? tanggal { get; set; }
		public string? file_field { get; set; }
	}

	public class VaksinPayload
	{
		public string? jenis_vaksin { get; set; }
		public int dosis_ke { get; set; }
		public DateTime? tanggal_vaksin { get; set; }
		public string? fasilitas_kesehatan { get; set; }
		public string? catatan { get; set; }
	}

	public class DocumentMetaPayload
	{
		public string? input_name { get; set; }
		public string? kode_dokumen { get; set; }
		public string? nama_dokumen { get; set; }
	}
}
