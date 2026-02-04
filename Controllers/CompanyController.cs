using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using one_db.Data;
using one_db.Models;

namespace one_db.Controllers
{
	[Authorize]
	public class CompanyController : Controller
	{
		private readonly AppDBContext _context;
		private readonly ILogger<CompanyController> _logger;
		private string controller_name = "Company";
		private string title_name = "Company";


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


		public CompanyController(AppDBContext context, ILogger<CompanyController> logger)
		{
			_context = context;
			_logger = logger;
		}
		public IActionResult GetAll()
		{
			try
			{
				var data = _context.tbl_r_comp.OrderBy(x => x.created_at).ToList();
				return Json(new { data = data });
			}
			catch (Exception ex)
			{
				var innerExceptionMessage = ex.InnerException?.Message ?? ex.Message;
				_logger.LogError(ex, "Error fetching data.");
				return Json(new { success = false, message = $"Terjadi kesalahan saat mengambil data ${ex.Message}." });
			}
		}

		public IActionResult Get(int id)
		{
			try
			{
				var result = _context.tbl_r_comp.Where(x => x.id == id).FirstOrDefault();

				if (result == null)
				{
					return Json(new { success = false, message = "Data tidak ditemukan." });
				}

				return Json(new { success = true, data = result });
			}
			catch (Exception ex)
			{
				var innerExceptionMessage = ex.InnerException?.Message ?? ex.Message;
				_logger.LogError(ex, "Terjadi kesalahan saat mengambil data karyawan.");
				return Json(new { success = false, message = $"Terjadi kesalahan saat mengambil data: {ex.Message}" });
			}
		}
		[Authorize]
		[HttpPost]
		public IActionResult Insert(tbl_r_comp a)
		{
			try
			{
				a.ip = System.Environment.MachineName;
				//a.created_at = DateTime.Now;
				_context.tbl_r_comp.Add(a);
				_context.SaveChanges();
				return Json(new { success = true, message = "Data berhasil disimpan." });
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
		public ActionResult Update(tbl_r_comp a)
		{
			try
			{
				var tbl_ = _context.tbl_r_comp.FirstOrDefault(f => f.id == a.id);
				if (tbl_ != null)
				{
					tbl_.id = a.id;
					tbl_.comp_code = a.comp_code;
					tbl_.company = a.company;
					tbl_.insert_by = a.insert_by;
					a.ip = System.Environment.MachineName;
					//tbl_.updated_at = DateTime.Now;
					_context.SaveChanges();
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
		public IActionResult Delete(int id)
		{
			try
			{
				var tbl_ = _context.tbl_r_comp.FirstOrDefault(f => f.id == id);
				if (tbl_ != null)
				{
					_context.tbl_r_comp.Remove(tbl_);
					_context.SaveChanges();
					return Json(new { success = true, message = "Data berhasil dihapus." });
				}
				else
				{
					return Json(new { success = false, message = "Data tidak ditemukan." });
				}
			}
			catch (Exception ex)
			{
				var innerExceptionMessage = ex.InnerException?.Message ?? ex.Message;
				_logger.LogError(ex, "Terjadi kesalahan saat menghapus data.");
				return Json(new { success = false, message = $"Terjadi kesalahan saat menghapus data: {ex.Message}" });
			}
		}
	}
}
