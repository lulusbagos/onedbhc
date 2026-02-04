using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using one_db.Data;
using one_db.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
// Mengganti EPPlus dengan NPOI
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting; // Diperlukan untuk mengakses wwwroot
									// Menghapus using OfficeOpenXml.Style;

namespace one_db.Controllers
{
	[Authorize]
	public class RosterController : Controller
	{
		private readonly AppDBContext _context;
		private readonly ILogger<RosterController> _logger;
		private const string controller_name = "Roster";
		private const string title_name = "Roster";

		public RosterController(AppDBContext context, ILogger<RosterController> logger)
		{
			_context = context;
			_logger = logger;
		}

		[Authorize]
		public IActionResult Index()
		{
			try
			{
				// ✅ 1️⃣ Ambil data dari Claims
				var kategoriUserId = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
				var compCode = User.FindFirst("comp_code")?.Value;
				var nrp = User.Identity?.Name;
				var dept = User.FindFirst("dept_code")?.Value;
				var nama = User.FindFirst("nama")?.Value;

				// ✅ 2️⃣ Fallback ke Session
				kategoriUserId ??= HttpContext.Session.GetString("kategori_user_id");
				compCode ??= HttpContext.Session.GetString("company");
				nrp ??= HttpContext.Session.GetString("nrp");
				dept ??= HttpContext.Session.GetString("dept");
				nama ??= HttpContext.Session.GetString("nama");

				// 🚨 3️⃣ Cek login valid
				if (string.IsNullOrEmpty(kategoriUserId) || string.IsNullOrEmpty(nrp))
				{
					_logger.LogWarning("User tanpa kategori_user_id mencoba akses AdminKonController.");
					return RedirectToAction("Index", "Login");
				}

				// ✅ 4️⃣ Dapatkan nama controller otomatis
				var controllerName = ControllerContext.ActionDescriptor.ControllerName;

				// 🔍 5️⃣ Cek akses lewat tabel RBAC
				bool punyaAkses = _context.tbl_r_menu.Any(x =>
					x.kategori_user_id == kategoriUserId &&
					(x.comp_code == null || x.comp_code == compCode) &&
					x.link_controller == controllerName);

				if (!punyaAkses)
				{
					_logger.LogWarning($"Akses ditolak: {nrp} ({kategoriUserId}) mencoba akses {controllerName}");
					return RedirectToAction("Index", "Login");
				}

				// 📋 6️⃣ Ambil menu sesuai kategori & company
				var menuList = _context.tbl_r_menu
					.Where(x => x.kategori_user_id == kategoriUserId)
					.Where(x => x.comp_code == null || x.comp_code == compCode)
					.OrderBy(x => x.type)
					.ThenBy(x => x.title)
					.ToList();

				// 📊 7️⃣ Hitung jumlah tiap kategori menu
				ViewBag.MenuMasterCount = menuList.Count(x => x.type == "Master");
				ViewBag.MaOwnerCount = menuList.Count(x => x.type == "MaOwner");
				ViewBag.HROwnerCount = menuList.Count(x => x.type == "HROwner");
				ViewBag.AdminOwnerCount = menuList.Count(x => x.type == "AdminOwner");
				ViewBag.AdminKonCount = menuList.Count(x => x.type == "AdminKon");

				// 📦 8️⃣ Data tambahan ke View
				ViewBag.Title = "Admin Kontraktor";
				ViewBag.Controller = controllerName;
				ViewBag.Setting = _context.tbl_m_setting_aplikasi.FirstOrDefault();
				ViewBag.Menu = menuList;
				ViewBag.insert_by = nrp;
				ViewBag.departemen = dept;
				ViewBag.nama = nama;
				ViewBag.company = compCode;

				_logger.LogInformation($"User {nrp} ({kategoriUserId}) mengakses {controllerName} di {compCode}");
				return View();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat load controller AdminKon");
				return RedirectToAction("Index", "Login");
			}
		}


		[Authorize]
		public IActionResult Download()
		{
			// CATATAN: Aksi 'Download' ini tampaknya identik dengan Index().
			// Tombol "Download Excel" yang baru TIDAK akan menggunakan ini,
			// tetapi akan memanggil 'DownloadExcelReport'
			try
			{
				var kategoriUserId = HttpContext.Session.GetString("kategori_user_id");
				var compCode = HttpContext.Session.GetString("company");

				if (string.IsNullOrEmpty(kategoriUserId))
				{
					return RedirectToAction("Index", "Login");
				}

				var cekAkses = _context.tbl_r_menu
					.Count(x => x.kategori_user_id == kategoriUserId &&
								(x.comp_code == null || x.comp_code == compCode) &&
								x.link_controller == controller_name);

				if (cekAkses == 0)
				{
					return RedirectToAction("Index", "Login");
				}

				var menuList = _context.tbl_r_menu
					.Where(x => x.kategori_user_id == kategoriUserId)
					.Where(x => x.comp_code == null || x.comp_code == compCode)
					.OrderBy(x => x.type)
					.ThenBy(x => x.title)
					.ToList();

				var masterCount = menuList.Count(x => x.type == "Master");
				var MaOwnerCount = menuList.Count(x => x.type == "MaOwner");
				var HROwnerCount = menuList.Count(x => x.type == "HROwner"); // ✅ hitung HROwner
				var AdminOwnerCount = menuList.Count(x => x.type == "AdminOwner");
				ViewBag.Title = title_name;
				ViewBag.Controller = controller_name;
				ViewBag.Setting = _context.tbl_m_setting_aplikasi.FirstOrDefault();
				ViewBag.Menu = menuList;
				ViewBag.MenuMasterCount = masterCount;
				ViewBag.HROwnerCount = HROwnerCount;
				ViewBag.AdminOwnerCount = AdminOwnerCount;
				ViewBag.MaOwnerCount = MaOwnerCount;
				ViewBag.insert_by = HttpContext.Session.GetString("nrp");
				ViewBag.departemen = HttpContext.Session.GetString("dept");
				ViewBag.company = compCode;


				return View();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat load menu");
				return RedirectToAction("Index", "Login");
			}
		}

		// =================================================================================
		// BAGIAN CRUD KETERANGAN (SUDAH ADA)
		// =================================================================================

		[HttpGet]
		[Authorize]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var data = await _context.tbl_m_roster_keterangan.ToListAsync();
				return Json(new { success = true, data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting all Roster Keterangan");
				return Json(new { success = false, message = "Gagal memuat data." });
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetAllRoster()
		{
			try
			{
				var data = await _context.tbl_m_roster_detail.ToListAsync();
				return Json(new { success = true, data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting all Roster Keterangan");
				return Json(new { success = false, message = "Gagal memuat data." });
			}
		}

		[HttpGet]
		[Authorize]
		public async Task<IActionResult> Get(string id)
		{
			try
			{
				var data = await _context.tbl_m_roster_keterangan.FindAsync(id);
				if (data == null)
				{
					return Json(new { success = false, message = "Data tidak ditemukan." });
				}
				return Json(new { success = true, data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting Roster Keterangan by ID");
				return Json(new { success = false, message = "Gagal memuat data." });
			}
		}

		[HttpPost]
		[Authorize]
		public async Task<IActionResult> Insert(tbl_m_roster_keterangan data)
		{
			try
			{

				data.id = Guid.NewGuid().ToString();
				data.created_at = DateTime.Now;
				data.created_by = HttpContext.Session.GetString("nrp");

				_context.tbl_m_roster_keterangan.Add(data);
				await _context.SaveChangesAsync();
				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error inserting Roster Keterangan");
				return Json(new { success = false, message = "Gagal menyimpan data." });
			}
		}

		[HttpPost]
		[Authorize]
		public async Task<IActionResult> Update(tbl_m_roster_keterangan data)
		{
			try
			{
				var existingData = await _context.tbl_m_roster_keterangan.FindAsync(data.id);
				if (existingData == null)
				{
					return Json(new { success = false, message = "Data tidak ditemukan." });
				}

				existingData.kode = data.kode;
				existingData.keterangan = data.keterangan;
				existingData.warna = data.warna;
				existingData.updated_at = DateTime.Now;
				existingData.updated_by = HttpContext.Session.GetString("nrp");

				await _context.SaveChangesAsync();
				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating Roster Keterangan");
				return Json(new { success = false, message = "Gagal memperbarui data." });
			}
		}

		[HttpPost]
		[Authorize]
		public async Task<IActionResult> Delete(string id)
		{
			try
			{
				var data = await _context.tbl_m_roster_keterangan.FindAsync(id);
				if (data == null)
				{
					return Json(new { success = false, message = "Data tidak ditemukan." });
				}

				_context.tbl_m_roster_keterangan.Remove(data);
				await _context.SaveChangesAsync();
				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting Roster Keterangan");
				return Json(new { success = false, message = "Gagal menghapus data." });
			}
		}

		// =================================================================================
		// BAGIAN REVISI ROSTER (SUDAH ADA)
		// =================================================================================

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize]
		public async Task<IActionResult> Create(tbl_r_revisi_roster model)
		{
			if (ModelState.IsValid)
			{
				try
				{
					model.id = Guid.NewGuid().ToString();
					model.status = "DIAJUKAN"; // Status awal
					model.insert_by = HttpContext.Session.GetString("nrp");
					model.ip = HttpContext.Connection.RemoteIpAddress?.ToString();
					var now = DateTime.Now;
					// Kolom waktu yang tersedia di tabel
					model.created_at = now;
					model.updated_at = now;

					await _context.tbl_r_revisi_roster.AddAsync(model);
					await _context.SaveChangesAsync();

					TempData["SuccessMessage"] = "Pengajuan revisi roster berhasil dikirim.";
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Gagal menyimpan pengajuan revisi roster.");
					TempData["ErrorMessage"] = "Gagal menyimpan pengajuan. Silakan coba lagi.";
				}
				return RedirectToAction(nameof(Index));
			}

			TempData["ErrorMessage"] = "Data yang dimasukkan tidak valid.";
			return RedirectToAction(nameof(Index));
		}

		[HttpGet]
		[Authorize]
		public async Task<IActionResult> GetRosterStatus(string nik, DateTime tanggal)
		{
			if (string.IsNullOrEmpty(nik) || tanggal == default)
			{
				return Json(new { status = "Data tidak lengkap" });
			}

			var rosterDetail = await _context.tbl_m_roster_detail
				.FirstOrDefaultAsync(d => d.nik == nik && d.working_date.Value.Date == tanggal.Date);

			if (rosterDetail != null)
			{
				return Json(new { status = rosterDetail.status });
			}

			return Json(new { status = "TIDAK DITEMUKAN" });
		}



		[HttpGet]
		[Authorize]
		public async Task<IActionResult> GetDepartments()
		{
			try
			{
				var data = await _context.vw_m_karyawan_indexim
									.Where(k => k.depart != null && k.kd_depart != null)
									.Select(k => new { k.kd_depart, k.depart })
									.Distinct()
									.OrderBy(k => k.depart)
									.ToListAsync();
				return Json(new { success = true, data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting departments");
				return Json(new { success = false, message = "Gagal memuat departemen." });
			}
		}

		/// <summary>
		/// [BARU] Mengambil daftar karyawan untuk modal revisi.
		/// </summary>
		[HttpGet]
		[Authorize]
		public async Task<IActionResult> GetKaryawanList()
		{
			try
			{
				var raw = await _context.vw_m_karyawan_indexim
									.Where(k => k.tgl_aktif != null && k.no_nik != null) // Hanya karyawan aktif
									.Select(k => new { k.no_nik, k.nama_lengkap, k.kd_depart, k.depart, k.posisi, k.tgl_aktif })
									.ToListAsync();

				var data = raw
					.GroupBy(k => k.no_nik)
					.Select(g => g.OrderByDescending(e => e.tgl_aktif).First())
					.Select(k => new { k.no_nik, k.nama_lengkap, k.kd_depart, k.depart, k.posisi })
					.OrderBy(k => k.nama_lengkap)
					.ToList();
				return Json(new { success = true, data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting karyawan list");
				return Json(new { success = false, message = "Gagal memuat karyawan." });
			}
		}

		// =================================================================================
		// [DIUBAH] FUNGSI DOWNLOAD EXCEL MENGGUNAKAN NPOI
		// =================================================================================

		[HttpGet]
		[Authorize]
		public async Task<IActionResult> DownloadExcelReport(string tglMulai, string tglSelesai, string kdDepart)
		{
			try
			{
				// 1. Set Lisensi EPPlus (Sudah tidak diperlukan untuk NPOI)
				// ExcelPackage.License = LicenseContext.NonCommercial; 

				// 2. Parse Tanggal
				if (!DateTime.TryParse(tglMulai, out DateTime startDate) || !DateTime.TryParse(tglSelesai, out DateTime endDate))
				{
					return BadRequest("Format tanggal tidak valid. Gunakan YYYY-MM-DD.");
				}

				// 3. Ambil Data dari DB
				var rosterQuery = _context.tbl_m_roster_detail
					.Where(r => r.working_date >= startDate && r.working_date <= endDate);

				if (!string.IsNullOrWhiteSpace(kdDepart))
				{
					var nikList = await _context.vw_m_karyawan_indexim
						.Where(k => k.kd_depart == kdDepart)
						.Select(k => k.no_nik)
						.ToListAsync();
					if (nikList.Any())
					{
						rosterQuery = rosterQuery.Where(r => nikList.Contains(r.nik));
					}
					else
					{
						rosterQuery = rosterQuery.Where(r => false); // no entries
					}
				}

				var rosterData = await rosterQuery
					.OrderBy(r => r.nik)
					.ThenBy(r => r.working_date)
					.ToListAsync();

				// 4. Proses Data (Pivot)
				var rosterMap = new Dictionary<string, Dictionary<string, string>>();
				var uniqueNiks = new List<string>();
				var dateHeaders = new List<DateTime>();

				// Buat header tanggal
				for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
				{
					dateHeaders.Add(dt);
				}

				// Isi map roster
				foreach (var item in rosterData)
				{
					if (string.IsNullOrEmpty(item.nik) || item.working_date == null) continue;

					string dateString = item.working_date.Value.ToString("yyyy-MM-dd"); // <-- Kunci disimpan dengan "dd" (benar)
					if (!rosterMap.ContainsKey(item.nik))
					{
						rosterMap[item.nik] = new Dictionary<string, string>();
						uniqueNiks.Add(item.nik); // NIK sudah terurut dari query DB
					}
					rosterMap[item.nik][dateString] = item.status;
				}

				// 5. Hasilkan File Excel (Menggunakan NPOI)
				IWorkbook workbook = new XSSFWorkbook();
				ISheet worksheet = workbook.CreateSheet("Roster");

				// --- Buat Style Header ---
				ICellStyle boldStyle = workbook.CreateCellStyle();
				IFont boldFont = workbook.CreateFont();
				boldFont.IsBold = true;
				boldStyle.SetFont(boldFont);

				// --- Header Row (Baris 0) ---
				IRow headerRow = worksheet.CreateRow(0);

				// Cell A1: "EmployeeCode"
				ICell cell = headerRow.CreateCell(0);
				cell.SetCellValue("EmployeeCode");
				cell.CellStyle = boldStyle;

				// Header Tanggal (B1, C1, dst) - Format YYYY-MM-DD
				for (int i = 0; i < dateHeaders.Count; i++)
				{
					cell = headerRow.CreateCell(i + 1);
					cell.SetCellValue(dateHeaders[i].ToString("yyyy-MM-dd")); // DIPERBAIKI: "DD" -> "dd"
					cell.CellStyle = boldStyle;
				}

				// --- Baris Data (Mulai dari baris 1) ---
				for (int r = 0; r < uniqueNiks.Count; r++)
				{
					string nik = uniqueNiks[r];
					int currentRowIndex = r + 1; // NPOI 0-indexed, jadi baris data mulai dari 1
					IRow dataRow = worksheet.CreateRow(currentRowIndex);

					// Kolom A (Employee Code)
					dataRow.CreateCell(0).SetCellValue(nik);

					var userRoster = rosterMap[nik];

					for (int c = 0; c < dateHeaders.Count; c++)
					{
						string dateString = dateHeaders[c].ToString("yyyy-MM-dd"); // DIPERBAIKI: "DD" -> "dd"
						if (userRoster.TryGetValue(dateString, out string status))
						{
							dataRow.CreateCell(c + 1).SetCellValue(status);
						}
						// Jika tidak ada status, NPOI akan membiarkannya kosong (null)
					}
				}

				// --- Styling (AutoFit) ---
				for (int i = 0; i <= dateHeaders.Count; i++)
				{
					worksheet.AutoSizeColumn(i);
				}

				// 6. Kembalikan File
				var stream = new MemoryStream();
				workbook.Write(stream, true); // 'true' untuk biarkan stream terbuka
				stream.Position = 0;

				string excelName = $"RosterReport_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";
				return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating Excel report");
				// Mengembalikan error sebagai content agar bisa dilihat jika ada masalah
				return Content($"Gagal membuat laporan: {ex.Message}\n{ex.StackTrace}", "text/plain");
			}
		}


	}
}


