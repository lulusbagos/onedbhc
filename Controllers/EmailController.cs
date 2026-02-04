using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using one_db.Data;
using one_db.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace one_db.Controllers
{
	[Authorize]
	public class EmailController : Controller
	{
		private readonly AppDBContext _context;
		private readonly ILogger<EmailController> _logger;
		private string controller_name = "Email";
		private string title_name = "Email";
		public EmailController(AppDBContext context, ILogger<EmailController> logger)
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


		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			var data = await _context.tbl_m_email.OrderBy(e => e.departemen).ToListAsync();
			return Json(new { data });
		}

		[HttpGet]
		public async Task<IActionResult> GetById(string id)
		{
			var email = await _context.tbl_m_email.FindAsync(id);
			if (email == null) return NotFound();
			return Json(email);
		}

		// --- ACTION BARU UNTUK MENGAMBIL DAFTAR DEPARTEMEN ---
		[HttpGet]
		public async Task<IActionResult> GetDepartments()
		{
			try
			{
				var departments = await _context.vw_m_karyawan_indexim
					.Where(d => d.depart != null && d.depart != "")
					.Select(k => k.depart)
					.Distinct()
					.OrderBy(d => d)
					.ToListAsync();
				return Json(departments);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching departments.");
				return Json(new { success = false, message = "Gagal memuat daftar departemen." });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([FromBody] tbl_m_email model)
		{
			if (!ModelState.IsValid)
			{
				return Json(new { success = false, message = "Data tidak valid." });
			}

			if (!AreEmailsValid(model.email) || !AreEmailsValid(model.cc, allowEmpty: true))
			{
				return Json(new { success = false, message = "Format email atau CC tidak valid. Pisahkan dengan titik koma (;)." });
			}

			try
			{
				model.id = Guid.NewGuid().ToString();
				model.insert_by = HttpContext.Session.GetString("nrp");
				model.ip = HttpContext.Connection.RemoteIpAddress?.ToString();
				model.created_at = DateTime.Now;
				model.updated_at = model.created_at;

				await _context.tbl_m_email.AddAsync(model);
				await _context.SaveChangesAsync();
				return Json(new { success = true, message = "Data berhasil ditambahkan." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating email config.");
				return Json(new { success = false, message = "Terjadi kesalahan server." });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Update([FromBody] tbl_m_email model)
		{
			if (!ModelState.IsValid)
			{
				return Json(new { success = false, message = "Data tidak valid." });
			}

			if (!AreEmailsValid(model.email) || !AreEmailsValid(model.cc, allowEmpty: true))
			{
				return Json(new { success = false, message = "Format email atau CC tidak valid. Pisahkan dengan titik koma (;)." });
			}

			try
			{
				var existing = await _context.tbl_m_email.FindAsync(model.id);
				if (existing == null) return NotFound();

				existing.departemen = model.departemen;
				existing.email = model.email;
				existing.cc = model.cc;
				existing.updated_at = DateTime.Now;

				_context.tbl_m_email.Update(existing);
				await _context.SaveChangesAsync();
				return Json(new { success = true, message = "Data berhasil diperbarui." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating email config.");
				return Json(new { success = false, message = "Terjadi kesalahan server." });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(string id)
		{
			try
			{
				var email = await _context.tbl_m_email.FindAsync(id);
				if (email == null) return NotFound();

				_context.tbl_m_email.Remove(email);
				await _context.SaveChangesAsync();
				return Json(new { success = true, message = "Data berhasil dihapus." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting email config.");
				return Json(new { success = false, message = "Terjadi kesalahan server." });
			}
		}

		// Helper method to validate multiple emails separated by semicolon
		private bool AreEmailsValid(string emails, bool allowEmpty = false)
		{
			if (string.IsNullOrWhiteSpace(emails))
			{
				return allowEmpty;
			}

			var emailArray = emails.Split(';', StringSplitOptions.RemoveEmptyEntries);
			var emailValidator = new EmailAddressAttribute();

			foreach (var email in emailArray)
			{
				if (!emailValidator.IsValid(email.Trim()))
				{
					return false;
				}
			}
			return true;
		}
	}
}

