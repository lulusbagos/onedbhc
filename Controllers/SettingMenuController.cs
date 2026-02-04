using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using one_db.Data;
using one_db.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace one_db.Controllers
{
	[Authorize]
	public class SettingMenuController : Controller
	{
		private readonly AppDBContext _context;
		private readonly ILogger<SettingMenuController> _logger;

		private const string controller_name = "SettingMenu";
		private const string title_name = "Pengaturan Menu";

		public SettingMenuController(AppDBContext context, ILogger<SettingMenuController> logger)
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


		// ✅ READ ALL
		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var data = await _context.tbl_m_setting_menu
					.AsNoTracking()
					.OrderByDescending(x => x.created_at)
					.ToListAsync();

				return Json(new { success = true, data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching data for {controller_name}", controller_name);
				return Json(new { success = false, message = $"Terjadi kesalahan saat mengambil data: {ex.Message}" });
			}
		}

		// ✅ READ BY ID
		[HttpGet]
		public async Task<IActionResult> Get(string id)
		{
			try
			{
				if (string.IsNullOrEmpty(id))
					return Json(new { success = false, message = "ID tidak valid." });

				var result = await _context.tbl_m_setting_menu
					.AsNoTracking()
					.FirstOrDefaultAsync(x => x.id == id);

				if (result == null)
					return Json(new { success = false, message = "Data tidak ditemukan." });

				return Json(new { success = true, data = result });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Terjadi kesalahan saat mengambil data by ID.");
				return Json(new { success = false, message = ex.Message });
			}
		}

		// ✅ CREATE
		[HttpPost]
		public async Task<IActionResult> Insert([FromBody] tbl_m_setting_menu model)
		{
			try
			{
				if (model == null)
					return Json(new { success = false, message = "Data tidak valid." });

				model.id = Guid.NewGuid().ToString();
				model.ip = HttpContext.Connection.RemoteIpAddress?.ToString();
				model.created_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				model.insert_by = HttpContext.Session.GetString("nrp");

				await _context.tbl_m_setting_menu.AddAsync(model);
				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "Data berhasil disimpan." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Terjadi kesalahan saat insert data.");
				var inner = ex.InnerException?.Message ?? "-";
				return Json(new { success = false, message = $"{ex.Message} ({inner})" });
			}
		}

		// ✅ UPDATE
		[HttpPost]
		public async Task<IActionResult> Update([FromBody] tbl_m_setting_menu model)
		{
			try
			{
				if (string.IsNullOrEmpty(model.id))
					return Json(new { success = false, message = "ID tidak valid." });

				var entity = await _context.tbl_m_setting_menu.FirstOrDefaultAsync(x => x.id == model.id);
				if (entity == null)
					return Json(new { success = false, message = "Data tidak ditemukan." });

				entity.awal = model.awal;
				entity.akhir = model.akhir;
				entity.updated_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				entity.insert_by = HttpContext.Session.GetString("nrp");
				entity.ip = HttpContext.Connection.RemoteIpAddress?.ToString();

				_context.tbl_m_setting_menu.Update(entity);
				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "Data berhasil diperbarui." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Terjadi kesalahan saat update data.");
				return Json(new { success = false, message = ex.Message });
			}
		}

		// ✅ DELETE
		[HttpPost]
		public async Task<IActionResult> Delete([FromBody] dynamic body)
		{
			try
			{
				string id = body?.id;
				if (string.IsNullOrEmpty(id))
					return Json(new { success = false, message = "ID tidak valid." });

				var entity = await _context.tbl_m_setting_menu.FirstOrDefaultAsync(x => x.id == id);
				if (entity == null)
					return Json(new { success = false, message = "Data tidak ditemukan." });

				_context.tbl_m_setting_menu.Remove(entity);
				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "Data berhasil dihapus." });
			}
			catch (Exception ex)
			{
				var inner = ex.InnerException?.Message ?? ex.Message;
				if (inner.Contains("REFERENCE constraint"))
					return Json(new { success = false, message = "Data tidak bisa dihapus karena masih digunakan oleh data lain." });

				_logger.LogError(ex, "Error delete data");
				return Json(new { success = false, message = inner });
			}
		}
	}
}
