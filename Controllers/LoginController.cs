using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using one_db.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace one_db.Controllers
{
	public class LoginController : Controller
	{
		private readonly AppDBContext _context;
		private readonly ILogger<LoginController> _logger;

		public LoginController(AppDBContext context, ILogger<LoginController> logger)
		{
			_context = context;
			_logger = logger;
		}
		[AllowAnonymous]
		public async Task<IActionResult> Index()
		{

			if (User?.Identity?.IsAuthenticated == true &&
				string.IsNullOrEmpty(HttpContext.Session.GetString("kategori_user_id")))
			{
				await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
				HttpContext.Session.Clear();
			}
			if (User?.Identity?.IsAuthenticated == true &&
				HttpContext.Session.GetString("kategori_user_id") != null)
			{
				return RedirectToAction("Index", "MenuUtama");
			}

			return View();
		}



		[HttpPost]
		public async Task<IActionResult> ProsesLogin(string nik, string password, string ip)
		{
			try
			{
				var user = await _context.tbl_m_user_login
					.FirstOrDefaultAsync(x => x.nrp == nik && x.password == password);

				if (user == null)
				{
					return Json(new
					{
						status = false,
						remarks = "Login gagal. Username/password salah."
					});
				}

				// 🧠 Ambil semua kategori user yang dimiliki user ini
				var kategoriUserList = await _context.vw_t_user_kategori
					.Where(x => x.Nrp == user.nrp)
					.Select(x => new
					{
						x.kategori_user_id,
						x.login_controller,
						x.login_function
					})
					.ToListAsync();

				if (kategoriUserList.Count == 0)
				{
					return Json(new { status = false, remarks = "Kategori user tidak ditemukan." });
				}

				// ✅ Simpan sementara data ke Session (optional, untuk ViewBag)
				HttpContext.Session.SetString("is_login", "true");
				HttpContext.Session.SetString("nrp", user.nrp);
				HttpContext.Session.SetString("nama", user.nama ?? "");
				HttpContext.Session.SetString("dept", user.dept_code ?? "");
				HttpContext.Session.SetString("company", user.comp_code ?? "");
				HttpContext.Session.SetString("ip", ip ?? "");

				return Json(new
				{
					status = true,
					remarks = "Login Sukses",
					data = kategoriUserList
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat proses login");
				return Json(new
				{
					status = false,
					remarks = "Terjadi kesalahan.",
					data = ex.Message
				});
			}
		}

		[HttpGet]
		public async Task<IActionResult> CekKategoriUser(string kategori_user_id)
		{
			try
			{
				var kategori = _context.tbl_r_kategori_user.FirstOrDefault(x => x.kategori == kategori_user_id);
				var nrp = HttpContext.Session.GetString("nrp");
				var nama = HttpContext.Session.GetString("nama");
				var dept = HttpContext.Session.GetString("dept");
				var comp = HttpContext.Session.GetString("company");

				if (kategori == null || string.IsNullOrEmpty(nrp))
				{
					return Json(new { status = false, remarks = "Data tidak valid" });
				}

				// ✅ Buat Claims principal
				var claims = new List<Claim>
				{
					new Claim(ClaimTypes.Name, nrp),
					new Claim("nama", nama ?? ""),
					new Claim("dept_code", dept ?? ""),
					new Claim("comp_code", comp ?? ""),
					new Claim(ClaimTypes.Role, kategori_user_id) // role = kategori_user_id
                };

				var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

				await HttpContext.SignInAsync(
					CookieAuthenticationDefaults.AuthenticationScheme,
					new ClaimsPrincipal(claimsIdentity),
					new AuthenticationProperties
					{
						IsPersistent = true,
						ExpiresUtc = DateTime.UtcNow.AddHours(4)
					});

				HttpContext.Session.SetString("kategori_user_id", kategori_user_id);

				return Json(new { status = true, remarks = "Sukses", data = kategori });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat cek kategori user");
				return Json(new { status = false, remarks = "Gagal", data = ex.Message });
			}
		}

		public async Task<IActionResult> Logout()
		{
			HttpContext.Session.Clear();
			await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
			return RedirectToAction("Index");
		}
	}
}
