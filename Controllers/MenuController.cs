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
// --- BARU: Tambahkan using ini ---
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace one_db.Controllers
{
	[Authorize]
	public class MenuController : Controller
	{
		private readonly AppDBContext _context;
		private readonly ILogger<MenuController> _logger;
		private string controller_name = "Menu";
		private string title_name = "Menu";

		public MenuController(AppDBContext context, ILogger<MenuController> logger)
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
				// --- PERBAIKAN: Gunakan async ---
				bool punyaAkses = await _context.tbl_r_menu.AnyAsync(x =>
					x.kategori_user_id == kategoriUserId &&
					(x.comp_code == null || x.comp_code == compCode) &&
					x.link_controller == controllerName);

				if (!punyaAkses)
				{
					_logger.LogWarning($"Akses ditolak: {nrp} ({kategoriUserId}) mencoba akses {controllerName}");
					return RedirectToAction("Index", "Login");
				}

				// 📋 6️⃣ Ambil menu sesuai kategori & company
				// --- PERBAIKAN: Gunakan async ---
				var menuList = await _context.tbl_r_menu
					.Where(x => x.kategori_user_id == kategoriUserId)
					.Where(x => x.comp_code == null || x.comp_code == compCode)
					.OrderBy(x => x.type)
					.ThenBy(x => x.title)
					.ToListAsync();

				// 📊 7️⃣ Hitung jumlah tiap kategori menu
				ViewBag.MenuMasterCount = menuList.Count(x => x.type == "Master");
				ViewBag.MaOwnerCount = menuList.Count(x => x.type == "MaOwner");
				ViewBag.HROwnerCount = menuList.Count(x => x.type == "HROwner");
				ViewBag.AdminOwnerCount = menuList.Count(x => x.type == "AdminOwner");
				ViewBag.AdminKonCount = menuList.Count(x => x.type == "AdminKon");

				// 📦 8️⃣ Data tambahan ke View
				ViewBag.Title = "Admin Kontraktor"; // Anda mungkin ingin mengganti ini ke "Pengaturan Menu"
				ViewBag.Controller = controllerName;
				// --- PERBAIKAN: Gunakan async ---
				ViewBag.Setting = await _context.tbl_m_setting_aplikasi.FirstOrDefaultAsync();
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
		// --- PERBAIKAN: Gunakan async ---
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var data = await _context.tbl_r_menu.OrderBy(x => x.type).ToListAsync();
				return Json(new { data = data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching data.");
				return Json(new { success = false, message = $"Terjadi kesalahan saat mengambil data ${ex.Message}." });
			}
		}

		[Authorize]
		// --- PERBAIKAN: Gunakan async ---
		public async Task<ActionResult> GetAllByKategoriUser()
		{
			try
			{
				var kategoriId = HttpContext.Session.GetInt32("kategori_id");
				if (kategoriId.HasValue)
				{
					var results = await _context.tbl_r_menu
						.Where(x => x.kategori_user_id == kategoriId.Value.ToString())
						.OrderBy(x => x.type)
						.ToListAsync();
					return Json(new { success = true, data = results });
				}
				else
				{
					return Json(new { success = false, message = "ID kategori pengguna tidak ditemukan dalam sesi." });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching data.");
				return Json(new { success = false, message = $"Terjadi kesalahan saat mengambil data {ex.Message}." });
			}
		}

		[Authorize]
		// --- PERBAIKAN: Gunakan async ---
		public async Task<IActionResult> Get(int id)
		{
			try
			{
				var result = await _context.tbl_r_menu.Where(x => x.id == id).FirstOrDefaultAsync();

				if (result == null)
				{
					return Json(new { success = false, message = "Data tidak ditemukan." });
				}

				return Json(new { success = true, data = result });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Terjadi kesalahan saat mengambil data karyawan.");
				return Json(new { success = false, message = $"Terjadi kesalahan saat mengambil data: {ex.Message}" });
			}
		}

		[HttpPost]
		// --- PERBAIKAN: Gunakan async ---
		public async Task<IActionResult> Insert(tbl_r_menu a)
		{
			try
			{
				// --- Logika Anda untuk menyimpan NAMA company ---
				if (string.IsNullOrEmpty(a.comp_code))
				{
					a.comp_code = null;
				}
				// --- Selesai ---

				a.created_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // Set tanggal sebagai string
				await _context.tbl_r_menu.AddAsync(a);
				await _context.SaveChangesAsync();
				return Json(new { success = true, message = "Data berhasil ditambahkan." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Terjadi kesalahan saat menambahkan data.");

				var innerExceptionMessage = ex.InnerException != null ? ex.InnerException.Message : "Tidak ada detail tambahan";
				return Json(new { success = false, message = $"Terjadi kesalahan saat menambahkan data: {ex.Message}. Detail: {innerExceptionMessage}" });
			}

		}

		[Authorize]
		[HttpPost]
		// --- PERBAIKAN: Gunakan async ---
		public async Task<ActionResult> Update(tbl_r_menu a)
		{
			try
			{
				var tbl_ = await _context.tbl_r_menu.FirstOrDefaultAsync(f => f.id == a.id);
				if (tbl_ != null)
				{
					tbl_.kategori_user_id = a.kategori_user_id;
					tbl_.type = a.type;
					tbl_.title = a.title;
					tbl_.link_controller = a.link_controller;

					// --- Logika Anda untuk menyimpan NAMA company ---
					if (string.IsNullOrEmpty(a.comp_code))
					{
						tbl_.comp_code = null;
					}
					else
					{
						tbl_.comp_code = a.comp_code;
					}
					// --- Selesai ---

					tbl_.link_function = a.link_function;
					tbl_.hidden = a.hidden;
					tbl_.new_tab = a.new_tab;
					tbl_.insert_by = a.insert_by;
					a.ip = System.Environment.MachineName;
					tbl_.updated_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // Set tanggal sebagai string
					await _context.SaveChangesAsync();
					return Json(new { success = true, message = "Data berhasil diubah." });
				}
				else
				{
					return Json(new { success = false, message = "Data tidak ditemukan." });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Terjadi kesalahan saat update data.");
				return Json(new { success = false, message = $"Terjadi kesalahan saat update data: {ex.Message}" });
			}
		}

		[Authorize]
		[HttpPost]
		// --- PERBAIKAN: Gunakan async ---
		public async Task<IActionResult> Delete(int id)
		{
			try
			{
				var tbl_ = await _context.tbl_r_menu.FirstOrDefaultAsync(f => f.id == id);
				if (tbl_ != null)
				{
					_context.tbl_r_menu.Remove(tbl_);
					await _context.SaveChangesAsync();
					return Json(new { success = true, message = "Data berhasil dihapus." });
				}
				else
				{
					return Json(new { success = false, message = "Data tidak ditemukan." });
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Terjadi kesalahan saat menghapus data.");
				return Json(new { success = false, message = $"Terjadi kesalahan saat menghapus data: {ex.Message}" });
			}
		}
	}
}
