using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using one_db.Data;
using one_db.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace one_db.Controllers
{
	[Authorize]
	public class RosterKaryawanController : Controller
	{
		private readonly AppDBContext _context;
		private readonly ILogger<RosterKaryawanController> _logger;
		private const string controller_name = "RosterKaryawan"; // Konstanta yang akan kita gunakan
		private const string title_name = "RosterKaryawan";

		public RosterKaryawanController(AppDBContext context, ILogger<RosterKaryawanController> logger)
		{
			_context = context;
			_logger = logger;
		}

		[Authorize]
		public async Task<IActionResult> Index()
		{
			try
			{
				// 🚨 0️⃣ Cek sinkronisasi Cookie vs Session
				if (User?.Identity?.IsAuthenticated == true &&
					string.IsNullOrEmpty(HttpContext.Session.GetString("kategori_user_id")))
				{
					await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
					HttpContext.Session.Clear();
					return RedirectToAction("Index", "Login");
				}

				// ✅ 1️⃣ Ambil data dari Claims
				var kategoriUserId = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
				var compCode = User.FindFirst("comp_code")?.Value;
				var nrp = User.Identity?.Name;
				var dept = User.FindFirst("dept_code")?.Value; // Dept pengguna (string)
				var nama = User.FindFirst("nama")?.Value;

				// ✅ 2️⃣ Fallback ke Session
				kategoriUserId ??= HttpContext.Session.GetString("kategori_user_id");
				compCode ??= HttpContext.Session.GetString("company");
				nrp ??= HttpContext.Session.GetString("nrp");
				dept ??= HttpContext.Session.GetString("dept"); // Fallback dept pengguna (string)
				nama ??= HttpContext.Session.GetString("nama");

				// 🚨 3️⃣ Cek login valid
				if (string.IsNullOrEmpty(kategoriUserId) || string.IsNullOrEmpty(nrp))
				{
					_logger.LogWarning("User tanpa kategori_user_id mencoba akses RosterKaryawanController.");
					return RedirectToAction("Index", "Login");
				}

				// ✅ 4️⃣ Cek akses lewat tabel RBAC (Menggunakan konstanta controller_name)
				bool punyaAkses = _context.tbl_r_menu.Any(x =>
					x.kategori_user_id == kategoriUserId &&
					(x.comp_code == null || x.comp_code == compCode) &&
					x.link_controller == controller_name);

				if (!punyaAkses)
				{
					_logger.LogWarning($"Akses ditolak: {nrp} ({kategoriUserId}) mencoba akses {controller_name}");
					return RedirectToAction("Index", "Login");
				}

				// 📋 5️⃣ Ambil menu sesuai kategori & company
				var menuList = _context.tbl_r_menu
					.Where(x => x.kategori_user_id == kategoriUserId)
					.Where(x => x.comp_code == null || x.comp_code == compCode)
					.OrderBy(x => x.type)
					.ThenBy(x => x.title)
					.ToList();

				// --- PERBAIKAN: Hapus pengecekan currentMenuId ---
				// var currentMenuId = menuList.FirstOrDefault(m => m.link_controller == controller_name)?.id;

				// 📊 6️⃣ Hitung jumlah tiap kategori menu
				ViewBag.MenuMasterCount = menuList.Count(x => x.type == "Master");
				ViewBag.MaOwnerCount = menuList.Count(x => x.type == "MaOwner");
				ViewBag.HROwnerCount = menuList.Count(x => x.type == "HROwner");
				ViewBag.AdminOwnerCount = menuList.Count(x => x.type == "AdminOwner");
				ViewBag.AdminKonCount = menuList.Count(x => x.type == "AdminKon");

				// 📦 7️⃣ Data tambahan ke View (YANG SUDAH ADA)
				ViewBag.Title = title_name; // Menggunakan konstanta
				ViewBag.Controller = controller_name; // Menggunakan konstanta
				ViewBag.Setting = _context.tbl_m_setting_aplikasi.FirstOrDefault();
				ViewBag.Menu = menuList;
				ViewBag.insert_by = nrp;
				ViewBag.departemen = dept; // 'dept' adalah departemen pengguna yang login
				ViewBag.nama = nama;
				ViewBag.company = compCode;

				// -------------------------------------------------------------------
				// --- MULAI PERBAIKAN: Tambahkan data yang hilang untuk View ---
				// -------------------------------------------------------------------

				// (A) Ambil daftar departemen untuk dropdown filter
				var departments = await _context.vw_m_karyawan_indexim
										.Select(k => k.depart)
										.Distinct()
										.OrderBy(d => d)
										.ToListAsync();
				ViewBag.Departments = departments;

				// (B) Tentukan apakah pengguna adalah Admin
				bool isAdmin = (kategoriUserId == "ADMINISTRATOR");
				ViewBag.IsAdmin = isAdmin;

				// (C) Simpan departemen pengguna untuk default filter JavaScript
				ViewBag.UserDept = dept;

				// --- PERBAIKAN LOGIKA EDIT ---
				// (D) Ambil setting roster. Kita asumsikan baris pertama adalah yang benar.
				tbl_m_setting_menu rosterSetting = await _context.tbl_m_setting_menu
															   .FirstOrDefaultAsync();

				bool isEditingAllowed = false;
				if (isAdmin)
				{
					// Jika pengguna adalah Admin, selalu izinkan edit (bypass tanggal)
					isEditingAllowed = true;
				}
				else if (rosterSetting != null && rosterSetting.awal.HasValue && rosterSetting.akhir.HasValue)
				{
					// Jika bukan Admin, cek berdasarkan rentang tanggal
					var today = DateTime.Today;
					isEditingAllowed = (today >= rosterSetting.awal.Value.Date && today <= rosterSetting.akhir.Value.Date);
				}
				// --- SELESAI PERBAIKAN LOGIKA EDIT ---

				// (E) Kirim data periode & status edit ke View
				ViewBag.RosterSetting = rosterSetting;
				ViewBag.IsEditingAllowed = isEditingAllowed;

				// -------------------------------------------------------------------
				// --- SELESAI PERBAIKAN ---
				// -------------------------------------------------------------------

				_logger.LogInformation($"User {nrp} ({kategoriUserId}) mengakses {controller_name} di {compCode}");
				return View();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat load controller RosterKaryawan");
				return RedirectToAction("Index", "Login");
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetKaryawan(string term, string departemen)
		{
			try
			{
				var kategoriUserId = HttpContext.Session.GetString("kategori_user_id");
				bool isAdmin = kategoriUserId == "ADMINISTRATOR";
				// --- PERBAIKAN: userDeptString sudah benar sebagai string ---
				string userDeptString = HttpContext.Session.GetString("dept");

				var query = _context.vw_m_karyawan_indexim.AsQueryable();

				if (!isAdmin)
				{
					// --- PERBAIKAN: Bandingkan string dengan string ---
					query = query.Where(k => k.depart == userDeptString);
				}
				else if (departemen != "ALL" && !string.IsNullOrEmpty(departemen))
				{
					// --- PERBAIKAN: Gunakan parameter 'departemen' (string) secara langsung ---
					query = query.Where(k => k.depart == departemen);
				}

				if (!string.IsNullOrEmpty(term))
				{
					query = query.Where(k => k.nama_lengkap.Contains(term) || k.no_nik.Contains(term));
				}

				// --- FIX START ---
				// Project to an anonymous type to avoid the Guid casting error on the 'id' property.
				// This selects only the necessary fields before materializing the list.
				var karyawanList = await query
					.Take(20)
					.Select(k => new
					{
						k.no_nik,
						k.nama_lengkap,
						k.depart,
						k.posisi,
						k.section,
						k.level
					})
					.ToListAsync();
				// --- FIX END ---

				var result = new List<object>();
				var today = DateTime.Today;
				var nextWeek = today.AddDays(7);

				var niks = karyawanList
					.Where(k => !string.IsNullOrEmpty(k.no_nik))
					.Select(k => k.no_nik)
					.ToList();

				var rosterCounts = new Dictionary<string, int>();
				if (niks.Any())
				{
					rosterCounts = await _context.tbl_m_roster_detail
					 .Where(r => niks.Contains(r.nik) && r.working_date >= today && r.working_date < nextWeek)
					 .GroupBy(r => r.nik)
					 .Select(g => new { Nik = g.Key, Count = g.Count() })
					 .ToDictionaryAsync(x => x.Nik, x => x.Count);
				}

				foreach (var k in karyawanList)
				{
					rosterCounts.TryGetValue(k.no_nik ?? "", out var count); // Use null-coalescing for safety
					result.Add(new
					{
						label = $"{k.no_nik} - {k.nama_lengkap}",
						value = k.nama_lengkap,
						nik = k.no_nik,
						nama = k.nama_lengkap,
						dept = k.depart,
						posisi = k.posisi,
						section = k.section,
						level = k.level,
						rosterStatus = count == 7 ? "complete" : "incomplete"
					});
				}

				return Json(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting karyawan for term '{term}' and department '{dept}'", term, departemen);
				return Json(new { success = false, message = $"Terjadi kesalahan server: {ex.Message}" });
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetRosterKeterangan()
		{
			try
			{
				var data = await _context.tbl_m_roster_keterangan.OrderBy(r => r.kode).ToListAsync();
				return Json(new { success = true, data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting roster keterangan");
				return Json(new { success = false, message = "Error." });
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetJadwal(string nik)
		{
			if (string.IsNullOrEmpty(nik))
			{
				return Json(new List<object>());
			}

			var jadwal = await _context.tbl_m_roster_detail
				.Where(j => j.nik == nik)
				.Join(_context.tbl_m_roster_keterangan,
					detail => detail.status,
					ket => ket.kode,
					(detail, ket) => new
					{
						title = detail.status,
						start = detail.working_date.Value.ToString("yyyy-MM-dd"),
						backgroundColor = ket.warna,
						borderColor = ket.warna,
						keterangan = ket.keterangan,
						id = detail.id
					})
				.ToListAsync();

			return Json(jadwal);
		}

		[HttpPost]
		public async Task<IActionResult> UpdateJadwal([FromBody] tbl_m_roster_detail data)
		{
			if (data == null || string.IsNullOrEmpty(data.nik) || data.working_date == null)
			{
				return Json(new { success = false, message = "Data tidak valid." });
			}

			try
			{
				var existing = await _context.tbl_m_roster_detail
					.FirstOrDefaultAsync(j => j.nik == data.nik && j.working_date.Value.Date == data.working_date.Value.Date);

				if (existing != null)
				{
					existing.status = data.status;
					existing.updated_at = DateTime.Now;
				}
				else
				{
					data.id = Guid.NewGuid().ToString();
					data.created_at = DateTime.Now;
					_context.tbl_m_roster_detail.Add(data);
				}

				await _context.SaveChangesAsync();
				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating schedule for NIK {nik}", data.nik);
				return Json(new { success = false, message = "Gagal menyimpan jadwal." });
			}
		}

		[HttpPost]
		public async Task<IActionResult> UpdateJadwalBulk([FromBody] JsonElement data)
		{
			string nik = string.Empty;
			var userIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
			try
			{
				nik = data.GetProperty("nik").GetString();
				string status = data.GetProperty("status").GetString();
				DateTime startDate = data.GetProperty("startDate").GetDateTime();
				DateTime endDate = data.GetProperty("endDate").GetDateTime();

				if (string.IsNullOrEmpty(nik) || string.IsNullOrEmpty(status) || startDate == default || endDate == default)
				{
					return Json(new { success = false, message = "Data tidak lengkap." });
				}

				var datesToUpdate = new List<DateTime>();
				for (var dt = startDate; dt < endDate; dt = dt.AddDays(1))
				{
					datesToUpdate.Add(dt);
				}

				var existingJadwalsDict = await _context.tbl_m_roster_detail
					.Where(j => j.nik == nik && j.working_date >= startDate && j.working_date < endDate)
					.ToDictionaryAsync(j => j.working_date.Value.Date);

				foreach (var date in datesToUpdate)
				{
					if (existingJadwalsDict.TryGetValue(date.Date, out var existingJadwal))
					{
						existingJadwal.status = status;
						existingJadwal.updated_at = DateTime.Now;
					}
					else
					{
						_context.tbl_m_roster_detail.Add(new tbl_m_roster_detail
						{
							id = Guid.NewGuid().ToString(),
							nik = nik,
							working_date = date,
							status = status,
							created_at = DateTime.Now,
							ip = userIpAddress,
						});
					}
				}
				await _context.SaveChangesAsync();
				return Json(new { success = true, message = "Jadwal berhasil diperbarui." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during bulk schedule update for NIK {nik}", nik);
				return Json(new { success = false, message = "Terjadi kesalahan." });
			}
		}

		[HttpPost]
		public async Task<IActionResult> RemoveJadwalRange([FromBody] JsonElement data)
		{
			string nik = string.Empty;
			try
			{
				nik = data.GetProperty("nik").GetString();
				DateTime startDate = data.GetProperty("startDate").GetDateTime();
				DateTime endDate = data.GetProperty("endDate").GetDateTime();

				var jadwalsToRemove = await _context.tbl_m_roster_detail
					.Where(j => j.nik == nik && j.working_date >= startDate && j.working_date < endDate)
					.ToListAsync();

				if (jadwalsToRemove.Any())
				{
					_context.tbl_m_roster_detail.RemoveRange(jadwalsToRemove);
					await _context.SaveChangesAsync();
				}

				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error removing schedule range for NIK {nik}", nik);
				return Json(new { success = false, message = "Gagal menghapus jadwal." });
			}
		}

		[HttpPost]
		public async Task<IActionResult> RemoveJadwal([FromBody] JsonElement data)
		{
			try
			{
				string id = data.GetProperty("id").GetString();
				var jadwal = await _context.tbl_m_roster_detail.FindAsync(id);
				if (jadwal == null)
				{
					return Json(new { success = false, message = "Jadwal tidak ditemukan." });
				}
				_context.tbl_m_roster_detail.Remove(jadwal);
				await _context.SaveChangesAsync();
				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error removing single schedule entry.");
				return Json(new { success = false, message = "Gagal menghapus jadwal." });
			}
		}

		[HttpPost]
		public async Task<IActionResult> CopyJadwal([FromBody] JsonElement data)
		{
			try
			{
				string sourceNik = data.GetProperty("sourceNik").GetString();
				var targetNiks = data.GetProperty("targetNiks").EnumerateArray().Select(e => e.GetString()).ToList();
				int month = data.GetProperty("month").GetInt32();
				int year = data.GetProperty("year").GetInt32();

				var startDate = new DateTime(year, month, 1);
				var endDate = startDate.AddMonths(1);

				var sourceJadwal = await _context.tbl_m_roster_detail
					.Where(j => j.nik == sourceNik && j.working_date >= startDate && j.working_date < endDate)
					.ToListAsync();

				if (!sourceJadwal.Any())
				{
					return Json(new { success = false, message = "Karyawan sumber tidak memiliki jadwal di bulan ini." });
				}

				// Hapus jadwal lama target
				var existingJadwalTarget = await _context.tbl_m_roster_detail
					.Where(j => targetNiks.Contains(j.nik) && j.working_date >= startDate && j.working_date < endDate)
					.ToListAsync();

				if (existingJadwalTarget.Any())
				{
					_context.tbl_m_roster_detail.RemoveRange(existingJadwalTarget);
				}

				// Tambah jadwal baru
				var newJadwals = new List<tbl_m_roster_detail>();
				foreach (var nik in targetNiks)
				{
					foreach (var jadwal in sourceJadwal)
					{
						newJadwals.Add(new tbl_m_roster_detail
						{
							id = Guid.NewGuid().ToString(),
							nik = nik,
							working_date = jadwal.working_date,
							status = jadwal.status,
							created_at = DateTime.Now
						});
					}
				}

				await _context.tbl_m_roster_detail.AddRangeAsync(newJadwals);
				await _context.SaveChangesAsync();

				return Json(new { success = true, message = $"Jadwal berhasil disalin ke {targetNiks.Count} karyawan." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error copying schedule.");
				return Json(new { success = false, message = "Terjadi kesalahan saat menyalin jadwal." });
			}
		}
	}
}

