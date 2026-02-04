using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using one_db.Data;
using one_db.Models;
using System;
using System.Linq;

// ⬇️ TAMBAHKAN USING INI ⬇️
using Microsoft.EntityFrameworkCore;
using one_db.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace one_db.Controllers
{
	[Authorize]
	public class SummaryRosterController : Controller
	{
		private readonly AppDBContext _context;
		private readonly ILogger<SummaryRosterController> _logger;

		public SummaryRosterController(AppDBContext context, ILogger<SummaryRosterController> logger)
		{
			_context = context;
			_logger = logger;
		}

		// ===================================================================
		// 1. METODE INDEX ANDA (Diperbarui untuk Filter Admin)
		// ===================================================================
		[Authorize]
		public IActionResult Index()
		{
			try
			{
				// 🚨 0️⃣ Cek sinkronisasi Cookie vs Session
				// ... (Logika SignOutAsync Anda di sini, jika diperlukan) ...
				if (User?.Identity?.IsAuthenticated == true &&
					string.IsNullOrEmpty(HttpContext.Session.GetString("kategori_user_id")))
				{
					Task.Run(() => HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme));
					HttpContext.Session.Clear();
					return RedirectToAction("Index", "Login");
				}

				// ✅ 1️⃣ Ambil data dari Claims
				var kategoriUserId = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
				var compCode = User.FindFirst("comp_code")?.Value;
				var nrp = User.Identity?.Name;
				var dept = User.FindFirst("dept_code")?.Value;
				var nama = User.FindFirst("nama")?.Value;

				// Nonaktifkan tracking untuk query
				_context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

				// ✅ 2️⃣ Fallback ke Session
				kategoriUserId ??= HttpContext.Session.GetString("kategori_user_id");
				compCode ??= HttpContext.Session.GetString("company");
				nrp ??= HttpContext.Session.GetString("nrp");
				dept ??= HttpContext.Session.GetString("dept");
				nama ??= HttpContext.Session.GetString("nama");

				// 🚨 3️⃣ Cek login valid
				if (string.IsNullOrEmpty(kategoriUserId) || string.IsNullOrEmpty(nrp))
				{
					_logger.LogWarning("User tanpa kategori_user_id mencoba akses SummaryRoster.");
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
				// ... (Hitungan menu lainnya) ...
				ViewBag.AdminKonCount = menuList.Count(x => x.type == "AdminKon");

				// 📦 8️⃣ Data tambahan ke View
				ViewBag.Title = "Summary Roster"; // Ganti judul
				ViewBag.Controller = controllerName;
				ViewBag.Setting = _context.tbl_m_setting_aplikasi.FirstOrDefault();
				ViewBag.Menu = menuList;
				ViewBag.insert_by = nrp;
				ViewBag.departemen = dept;
				ViewBag.nama = nama;
				ViewBag.company = compCode;

				// 🚀 PERUBAHAN: Kirim Kategori User & Daftar Dept untuk Filter
				ViewBag.kategoriUserId = kategoriUserId;
				if (kategoriUserId == "ADMINISTRATOR")
				{
					// Ambil daftar departemen unik untuk filter
					ViewBag.DeptList = _context.vw_m_karyawan_indexim
						.Select(k => k.depart)
						.Distinct()
						.OrderBy(d => d)
						.ToList();
				}
				// 🚀 AKHIR PERUBAHAN

				_logger.LogInformation($"User {nrp} ({kategoriUserId}) mengakses {controllerName} di {compCode}");

				// Hanya me-render view "shell"
				// View ini TIDAK menggunakan @model, sehingga error C# Anda tidak akan terjadi.
				return View();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat load controller SummaryRoster");
				return RedirectToAction("Index", "Login");
			}
		}


		// ===================================================================
		// 2. METODE GET DATA (Diperbarui untuk Menerima Filter)
		// ===================================================================
		[Authorize]
		[HttpGet]
		public async Task<IActionResult> GetRosterData(int? year, int? month, string filterDept) // 🚀 Tambah parameter filterDept
		{
			try
			{
				// 1. Ambil info user
				var kategoriUserId = HttpContext.Session.GetString("kategori_user_id") ?? User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
				var dept = HttpContext.Session.GetString("dept") ?? User.FindFirst("dept_code")?.Value;

				if (string.IsNullOrEmpty(kategoriUserId))
				{
					return Unauthorized(new { message = "Sesi berakhir. Silakan login kembali." });
				}

				// 2. Tentukan Rentang Tanggal
				DateTime today = DateTime.Today;
				int currentYear = year ?? today.Year;
				int currentMonth = month ?? today.Month;
				DateTime startDate = new DateTime(currentYear, currentMonth, 1);
				DateTime endDate = startDate.AddMonths(1).AddDays(-1);

				var dateRange = Enumerable.Range(0, 1 + endDate.Subtract(startDate).Days)
										  .Select(offset => startDate.AddDays(offset))
										  .ToList();

				// 3. 🚀 LOGIKA FILTER KARYAWAN (Diperbarui)
				var employeeQuery = _context.vw_m_karyawan_indexim.AsNoTracking();

				if (kategoriUserId != "ADMINISTRATOR")
				{
					// User biasa: hanya lihat departemen sendiri
					employeeQuery = employeeQuery.Where(k => k.depart == dept);
				}
				else if (!string.IsNullOrEmpty(filterDept))
				{
					// Admin & memilih filter departemen
					employeeQuery = employeeQuery.Where(k => k.depart == filterDept);
				}
				// Jika Admin & filterDept kosong, maka tampilkan semua (tanpa .Where)

				var employees = await employeeQuery
					.OrderBy(k => k.nama_lengkap)
					.Select(k => new { k.no_nik, k.nama_lengkap, k.depart, k.posisi })
					.ToListAsync();

				var niks = employees.Select(e => e.no_nik).ToList();

				// 4. Ambil Legenda
				var rosterKeterangan = await _context.tbl_m_roster_keterangan.AsNoTracking().ToListAsync();
				var statusColors = rosterKeterangan.ToDictionary(k => k.kode, k => k.warna);

				// 5. Ambil Data Roster Detail
				var rosterDetails = await _context.tbl_m_roster_detail.AsNoTracking()
					.Where(r => niks.Contains(r.nik) &&
								r.working_date.HasValue &&
								r.working_date.Value.Date >= startDate &&
								r.working_date.Value.Date <= endDate)
					.Select(r => new { r.nik, r.working_date, r.status })
					.ToListAsync();

				var rosterGroups = rosterDetails.ToLookup(r => r.nik);

				// 6. Siapkan ViewModel (akan dikirim sebagai JSON)
				var viewModel = new SummaryRosterViewModel
				{
					DateHeaders = dateRange,
					StatusColors = statusColors,
					Legend = rosterKeterangan.OrderBy(k => k.kode).ToList(),
					CurrentMonthYear = startDate,
					EmployeeRows = new List<EmployeeRosterRow>()
				};

				// 7. Isi data baris per karyawan
				foreach (var emp in employees)
				{
					var row = new EmployeeRosterRow
					{
						NIK = emp.no_nik,
						Nama = emp.nama_lengkap,
						Departemen = emp.depart,
						Posisi = emp.posisi,
						RosterCells = new Dictionary<DateTime, string>()
					};

					var empRosterData = rosterGroups[emp.no_nik]
						.ToDictionary(r => r.working_date.Value.Date, r => r.status);

					foreach (var date in dateRange)
					{
						if (empRosterData.TryGetValue(date, out var status))
						{
							row.RosterCells[date] = status;
						}
						else
						{
							row.RosterCells[date] = null;
						}
					}
					viewModel.EmployeeRows.Add(row);
				}

				// 8. Kembalikan sebagai JSON
				return Json(viewModel);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat GetRosterData");
				return StatusCode(500, new { message = "Terjadi kesalahan server: " + ex.Message });
			}
		}
	}
}