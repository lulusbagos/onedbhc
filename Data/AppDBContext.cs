using Microsoft.EntityFrameworkCore;
using one_db.Models;
using one_db.Models.NewEmployeeModels;
using one_db.Models.Undian;
using System.Collections.Generic;

namespace one_db.Data
{
	public class AppDBContext : DbContext
	{
		public AppDBContext(DbContextOptions<AppDBContext> options)
			: base(options)
		{
			// Menonaktifkan validasi foreign key untuk memungkinkan input manual
			this.ChangeTracker.LazyLoadingEnabled = false;
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Konfigurasi untuk tbl_r_dept
			modelBuilder.Entity<tbl_r_dept>()
				.HasKey(d => d.id);

			modelBuilder.Entity<UndianCoupon>()
				.HasIndex(c => new { c.periode, c.kode_kupon })
				.IsUnique();

			modelBuilder.Entity<UndianResult>()
				.HasIndex(r => new { r.pengundian_id, r.hadiah_id, r.nomor_urut_pemenang })
				.IsUnique();

			modelBuilder.Entity<UndianScanLog>()
				.HasIndex(s => s.no_nik)
				.IsUnique();

			// Menonaktifkan cascade delete
			foreach (var relationship in modelBuilder.Model.GetEntityTypes()
				.SelectMany(e => e.GetForeignKeys()))
			{
				relationship.DeleteBehavior = DeleteBehavior.NoAction;
			}
		}

		// --- DbSet yang Sudah Ada ---
		public DbSet<tbl_r_dept> tbl_r_dept { get; set; }
		public DbSet<tbl_m_user_login> tbl_m_user_login { get; set; }
		public DbSet<tbl_r_kategori_user> tbl_r_kategori_user { get; set; }
		public DbSet<tbl_r_menu> tbl_r_menu { get; set; }
		public DbSet<tbl_r_comp> tbl_r_comp { get; set; }
		public DbSet<tbl_m_setting_aplikasi> tbl_m_setting_aplikasi { get; set; }
		public DbSet<vw_t_user_kategori> vw_t_user_kategori { get; set; }
		public DbSet<tbl_m_karyawan_old> tbl_m_karyawan_old { get; set; }
		public DbSet<vw_m_karyawan_indexim> vw_m_karyawan_indexim { get; set; }
		public DbSet<tbl_t_pengajuan> tbl_t_pengajuan { get; set; }
		public DbSet<tbl_m_dashboard> tbl_m_dashboard { get; set; }
		public DbSet<vw_m_surat_pengajuan> vw_m_surat_pengajuan { get; set; }
		public DbSet<vw_m_karyawan> vw_m_karyawan { get; set; }
		public DbSet<tbl_m_email> tbl_m_email { get; set; }
		public DbSet<tbl_m_roster_keterangan> tbl_m_roster_keterangan { get; set; }
		public DbSet<tbl_m_roster_detail> tbl_m_roster_detail { get; set; }
		public DbSet<tbl_m_setting_menu> tbl_m_setting_menu { get; set; }
		public DbSet<tbl_r_revisi_roster> tbl_r_revisi_roster { get; set; }
		public DbSet<tbl_r_mitra_pengajuan> tbl_r_mitra_pengajuan { get; set; }
		public DbSet<tbl_m_dokumen_kepatuhan> tbl_m_dokumen_kepatuhan { get; set; }
		public DbSet<tbl_r_dokumen_mitra> tbl_r_dokumen_mitra { get; set; }
		public DbSet<tbl_r_section> tbl_r_section { get; set; }
		public DbSet<tbl_r_position> tbl_r_position { get; set; }
		public DbSet<tbl_r_pendidikan> tbl_r_pendidikan { get; set; }

		// --- DbSet Baru dari Modul Karyawan (New Structure) ---
		public DbSet<Employee> Employees { get; set; }
		public DbSet<EmployeeWorkHistory> EmployeeWorkHistories { get; set; }
		public DbSet<EmployeeAddress> EmployeeAddresses { get; set; }
		public DbSet<EmployeeDocument> EmployeeDocuments { get; set; }
		public DbSet<EmployeeSection> EmployeeSections { get; set; }
		public DbSet<EmployeePosition> EmployeePositions { get; set; }
		public DbSet<EmployeeDepartment> EmployeeDepartments { get; set; }
		public DbSet<EmployeeCompany> EmployeeCompanies { get; set; }

		// --- Modul Undian ---
		public DbSet<UndianCoupon> UndianCoupons { get; set; }
		public DbSet<UndianPrize> UndianPrizes { get; set; }
		public DbSet<UndianDraw> UndianDraws { get; set; }
		public DbSet<UndianResult> UndianResults { get; set; }
		public DbSet<UndianScanLog> UndianScanLogs { get; set; }

		// --- Input Karyawan Fleksibel ---
		public DbSet<tbl_r_company_level> tbl_r_company_level { get; set; }
		public DbSet<tbl_m_company> tbl_m_company { get; set; }
		public DbSet<tbl_m_karyawan_profile> tbl_m_karyawan_profile { get; set; }
		public DbSet<tbl_t_karyawan_company> tbl_t_karyawan_company { get; set; }
		public DbSet<tbl_t_karyawan_pekerjaan_history> tbl_t_karyawan_pekerjaan_history { get; set; }
		public DbSet<tbl_t_karyawan_alamat_history> tbl_t_karyawan_alamat_history { get; set; }
		public DbSet<tbl_t_karyawan_bank_history> tbl_t_karyawan_bank_history { get; set; }
		public DbSet<tbl_t_karyawan_emergency_history> tbl_t_karyawan_emergency_history { get; set; }
		public DbSet<tbl_t_karyawan_keluarga_history> tbl_t_karyawan_keluarga_history { get; set; }
		public DbSet<tbl_t_karyawan_pendidikan_history> tbl_t_karyawan_pendidikan_history { get; set; }
		public DbSet<tbl_t_karyawan_sertifikasi_history> tbl_t_karyawan_sertifikasi_history { get; set; }
		public DbSet<tbl_t_karyawan_mcu_history> tbl_t_karyawan_mcu_history { get; set; }
		public DbSet<tbl_t_karyawan_vaksin_history> tbl_t_karyawan_vaksin_history { get; set; }
		public DbSet<tbl_t_karyawan_dokumen_history> tbl_t_karyawan_dokumen_history { get; set; }
		public DbSet<tbl_t_karyawan_invite> tbl_t_karyawan_invite { get; set; }
		public DbSet<tbl_m_travel_authorization> tbl_m_travel_authorization { get; set; }

	}
}
