using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using one_db.Data;
using one_db.Models;
using Microsoft.AspNetCore.Http;       // <-- Tambahkan ini
using Microsoft.AspNetCore.Hosting;    // <-- Tambahkan ini
using System.IO;                       // <-- Tambahkan ini
using System.Security.Claims;          // <-- Tambahkan ini
using System.Threading.Tasks;          // <-- Tambahkan ini (Meskipun saya tidak membuat async, ini best practice)
									   // --- BARU ---
using Microsoft.EntityFrameworkCore; // <-- Tambahkan ini

namespace one_db.Controllers
{
	// --- BARU ---
	// Model DTO (Data Transfer Object) untuk menerima data dari modal ganti password
	public class ChangePasswordRequest
	{
		public string OldPassword { get; set; }
		public string NewPassword { get; set; }
		public string ConfirmPassword { get; set; }
	}

	// Model DTO untuk menerima gambar base64 dari cropper
	public class ProfilePictureRequest
	{
		public string ImageData { get; set; } // Ini akan berisi base64 string
	}
	// --- SELESAI ---


	[Authorize]
	public class UserLoginController : Controller
	{
		private readonly AppDBContext _context;
		private readonly ILogger<UserLoginController> _logger;
		private readonly IWebHostEnvironment _hostingEnvironment; // <-- Tambahan
		private readonly IHttpContextAccessor _httpContextAccessor; // <-- Tambahan
		private string controller_name = "UserLogin";
		private string title_name = "UserLogin";

		// --- MODIFIKASI: Konstruktor ---
		public UserLoginController(AppDBContext context,
								   ILogger<UserLoginController> logger,
								   IWebHostEnvironment hostingEnvironment,    // <-- Tambahan
								   IHttpContextAccessor httpContextAccessor) // <-- Tambahan
		{
			_context = context;
			_logger = logger;
			_hostingEnvironment = hostingEnvironment;    // <-- Tambahan
			_httpContextAccessor = httpContextAccessor; // <-- Tambahan
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

				// --- BARU ---
				// ✅ 6.5️⃣ Ambil foto profil dan simpan ke Session
				var user = _context.tbl_m_user_login.FirstOrDefault(u => u.nrp == nrp);
				string profilePicPath = (user?.profile_picture_path) ?? "~/user.png";
				HttpContext.Session.SetString("profile_picture_path", profilePicPath);
				// --- SELESAI ---

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

		// ... (GetAll, Get, Insert, Update, Delete Anda tetap sama) ...
		// ... Pastikan method Get, Insert, Update, Delete Anda ada di sini ...

		public IActionResult GetAll()
		{
			try
			{
				var data = _context.tbl_m_user_login.OrderBy(x => x.created_at).ToList();
				return Json(new { success = true, data = data });
			}
			catch (Exception ex)
			{
				var innerExceptionMessage = ex.InnerException?.Message ?? ex.Message;
				_logger.LogError(ex, "Error fetching data.");
				return Json(new { success = false, message = $"Terjadi kesalahan saat mengambil data: {innerExceptionMessage}." });
			}
		}

		public IActionResult Get(int id)
		{
			try
			{
				var result = _context.tbl_m_user_login.Where(x => x.id == id).FirstOrDefault();

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
		public async Task<IActionResult> Insert(tbl_m_user_login a, IFormFile? profilePictureFile) // <-- PERUBAHAN
		{
			try
			{
				// Model binder akan mengisi a.comp_code dan a.dept_code dengan NAMA LENGKAP
				// dari form, sesuai permintaan Anda.

				a.ip = System.Environment.MachineName;
				a.created_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // <-- BARU: Set created_at

				// --- BARU: Logic Simpan File ---
				if (profilePictureFile != null && profilePictureFile.Length > 0)
				{
					// Pastikan NRP ada untuk nama file
					if (string.IsNullOrEmpty(a.nrp))
					{
						return Json(new { success = false, message = "NRP (NIK) wajib diisi saat mengunggah foto." });
					}

					var folderPath = Path.Combine(_hostingEnvironment.WebRootPath, "img", "profiles");
					if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

					var fileName = $"{a.nrp.Replace(" ", "_")}_{Guid.NewGuid().ToString().Substring(0, 8)}{Path.GetExtension(profilePictureFile.FileName)}";
					var filePath = Path.Combine(folderPath, fileName);

					await using (var stream = new FileStream(filePath, FileMode.Create))
					{
						await profilePictureFile.CopyToAsync(stream);
					}
					a.profile_picture_path = $"~/img/profiles/{fileName}";
				}
				else
				{
					a.profile_picture_path = "~/user.png"; // Default jika tidak upload
				}
				// --- SELESAI BARU ---


				_context.tbl_m_user_login.Add(a);
				await _context.SaveChangesAsync(); // <-- PERUBAHAN: async
				return Json(new { success = true, message = "Data berhasil disimpan." });
			}
			catch (Exception ex)
			{
				var innerExceptionMessage = ex.InnerException?.Message ?? ex.Message;
				_logger.LogError(ex, $"Terjadi kesalahan saat menambahkan data. Detail: {innerExceptionMessage}");
				return Json(new { success = false, message = $"Terjadi kesalahan saat menambahkan data: {innerExceptionMessage}" });
			}
		}

		[Authorize]

		[HttpPost]
		public async Task<ActionResult> Update(tbl_m_user_login a, IFormFile? profilePictureFile) // <-- PERUBAHAN
		{
			try
			{
				var tbl_ = await _context.tbl_m_user_login.FirstOrDefaultAsync(f => f.id == a.id); // <-- PERUBAHAN: async
				if (tbl_ != null)
				{
					tbl_.kategori_user_id = a.kategori_user_id;
					tbl_.nrp = a.nrp;
					tbl_.password = a.password;
					tbl_.nama = a.nama;
					tbl_.dept_code = a.dept_code; // Ini akan berisi NAMA LENGKAP dari form
					tbl_.comp_code = a.comp_code; // Ini akan berisi NAMA LENGKAP dari form
					tbl_.insert_by = a.insert_by;
					tbl_.ip = System.Environment.MachineName; // <-- PERUBAHAN: set ip di tbl_

					// --- BARU: Logic Update File ---
					if (profilePictureFile != null && profilePictureFile.Length > 0)
					{
						// 1. Hapus file lama jika ada
						if (!string.IsNullOrEmpty(tbl_.profile_picture_path) && tbl_.profile_picture_path != "~/user.png")
						{
							try
							{
								string oldFilePathServer = tbl_.profile_picture_path.Replace("~/", _hostingEnvironment.WebRootPath + Path.DirectorySeparatorChar).Replace("/", Path.DirectorySeparatorChar.ToString());
								if (System.IO.File.Exists(oldFilePathServer))
								{
									System.IO.File.Delete(oldFilePathServer);
								}
							}
							catch (Exception ex)
							{
								_logger.LogWarning(ex, "Gagal menghapus foto profil lama: {Path}", tbl_.profile_picture_path);
								// Jangan hentikan proses update hanya karena gagal hapus file lama
							}
						}

						// 2. Simpan file baru
						var folderPath = Path.Combine(_hostingEnvironment.WebRootPath, "img", "profiles");
						if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

						var fileName = $"{tbl_.nrp.Replace(" ", "_")}_{Guid.NewGuid().ToString().Substring(0, 8)}{Path.GetExtension(profilePictureFile.FileName)}";
						var filePath = Path.Combine(folderPath, fileName);

						await using (var stream = new FileStream(filePath, FileMode.Create))
						{
							await profilePictureFile.CopyToAsync(stream);
						}

						// 3. Update path di database
						tbl_.profile_picture_path = $"~/img/profiles/{fileName}";
					}
					// Jika tidak ada file baru diupload, biarkan profile_picture_path yang lama.
					// --- SELESAI BARU ---

					tbl_.updated_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // <-- PERUBAHAN: Set updated_at
					await _context.SaveChangesAsync(); // <-- PERUBAHAN: async
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
		public async Task<IActionResult> Delete(int id) // <-- PERUBAHAN: async
		{
			try
			{
				var tbl_ = await _context.tbl_m_user_login.FirstOrDefaultAsync(f => f.id == id); // <-- PERUBAHAN: async
				if (tbl_ != null)
				{
					// --- BARU (Opsional) ---
					// Hapus file foto profil jika ada saat user didelete
					if (!string.IsNullOrEmpty(tbl_.profile_picture_path) && tbl_.profile_picture_path != "~/user.png")
					{
						string oldFilePathServer = tbl_.profile_picture_path.Replace("~/", _hostingEnvironment.WebRootPath + Path.DirectorySeparatorChar).Replace("/", Path.DirectorySeparatorChar.ToString());
						if (System.IO.File.Exists(oldFilePathServer))
						{
							System.IO.File.Delete(oldFilePathServer);
						}
					}
					// --- SELESAI ---

					_context.tbl_m_user_login.Remove(tbl_);
					await _context.SaveChangesAsync(); // <-- PERUBAHAN: async
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



		// --- PERBAIKAN DI SINI ---
		[HttpPost]
		[Authorize]
		[IgnoreAntiforgeryToken] // <-- TAMBAHKAN INI
		public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model) // <-- PERUBAHAN: async
		{
			try
			{
				// 1. Validasi model
				if (model.NewPassword != model.ConfirmPassword)
				{
					return Json(new { success = false, message = "Password baru dan konfirmasi tidak cocok." });
				}

				if (string.IsNullOrEmpty(model.OldPassword) || string.IsNullOrEmpty(model.NewPassword))
				{
					return Json(new { success = false, message = "Password tidak boleh kosong." });
				}

				// 2. Dapatkan NRP user yang sedang login
				var nrp = _httpContextAccessor.HttpContext.User.Identity.Name;
				if (string.IsNullOrEmpty(nrp))
				{
					nrp = _httpContextAccessor.HttpContext.Session.GetString("nrp");
				}

				if (string.IsNullOrEmpty(nrp))
				{
					return Json(new { success = false, message = "Sesi tidak valid, silahkan login ulang." });
				}

				// 3. Cari user di database
				var user = await _context.tbl_m_user_login.FirstOrDefaultAsync(u => u.nrp == nrp); // <-- PERUBAHAN: async
				if (user == null)
				{
					return Json(new { success = false, message = "User tidak ditemukan." });
				}


				// 4. Cek password lama (Versi Plain Text)
				if (user.password != model.OldPassword)
				{
					return Json(new { success = false, message = "Password lama salah." });
				}

				// 5. Update password baru (Versi Plain Text)
				user.password = model.NewPassword;
				user.updated_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // Sesuaikan format

				await _context.SaveChangesAsync(); // <-- PERUBAHAN: async

				return Json(new { success = true, message = "Password berhasil diubah." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat ganti password.");
				return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
			}
		}


		// --- PERBAIKAN DI SINI ---
		[HttpPost]
		[Authorize]
		[IgnoreAntiforgeryToken] // <-- TAMBAHKAN INI
		public async Task<IActionResult> UpdateProfilePicture([FromBody] ProfilePictureRequest model) // <-- PERUBAHAN: async
		{
			try
			{
				// 1. Dapatkan NRP user
				var nrp = _httpContextAccessor.HttpContext.User.Identity.Name;
				if (string.IsNullOrEmpty(nrp))
				{
					nrp = _httpContextAccessor.HttpContext.Session.GetString("nrp");
				}

				if (string.IsNullOrEmpty(nrp))
				{
					return Json(new { success = false, message = "Sesi tidak valid." });
				}

				// 2. Cari user
				var user = await _context.tbl_m_user_login.FirstOrDefaultAsync(u => u.nrp == nrp); // <-- PERUBAHAN: async
				if (user == null)
				{
					return Json(new { success = false, message = "User tidak ditemukan." });
				}

				// 3. Proses gambar Base64
				if (string.IsNullOrEmpty(model.ImageData) || !model.ImageData.Contains(","))
				{
					return Json(new { success = false, message = "Data gambar tidak valid." });
				}

				var base64Data = model.ImageData.Split(',')[1];
				var bytes = Convert.FromBase64String(base64Data);

				// 4. Tentukan path penyimpanan (di dalam wwwroot)
				var folderPath = Path.Combine(_hostingEnvironment.WebRootPath, "img", "profiles");
				if (!Directory.Exists(folderPath))
				{
					Directory.CreateDirectory(folderPath); // Buat folder jika belum ada
				}

				// 5. Buat nama file unik & simpan file
				var fileName = $"{nrp.Replace(" ", "_")}_{Guid.NewGuid().ToString().Substring(0, 8)}.png";
				var filePath = Path.Combine(folderPath, fileName);

				await System.IO.File.WriteAllBytesAsync(filePath, bytes); // <-- PERUBAHAN: async

				// 6. Buat path relatif untuk disimpan ke DB
				var relativePath = $"~/img/profiles/{fileName}";

				// Hapus foto lama jika ada
				if (!string.IsNullOrEmpty(user.profile_picture_path) && user.profile_picture_path != "~/user.png")
				{
					// Ubah path relatif (~/img/...) menjadi path fisik server (C:\...)
					string oldFilePathServer = user.profile_picture_path.Replace("~/", _hostingEnvironment.WebRootPath + Path.DirectorySeparatorChar).Replace("/", Path.DirectorySeparatorChar.ToString());
					if (System.IO.File.Exists(oldFilePathServer))
					{
						System.IO.File.Delete(oldFilePathServer);
					}
				}

				// 7. Simpan path baru ke database
				user.profile_picture_path = relativePath;
				user.updated_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
				await _context.SaveChangesAsync(); // <-- PERUBAHAN: async

				// 8. Update sesi agar gambar di topbar langsung berubah
				_httpContextAccessor.HttpContext.Session.SetString("profile_picture_path", relativePath);

				// 9. Kirim kembali path baru (untuk di-load JS)
				return Json(new { success = true, message = "Foto profil berhasil diperbarui.", newImagePath = Url.Content(relativePath) });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat update foto profil.");
				return Json(new { success = false, message = $"Terjadi kesalahan: {ex.Message}" });
			}
		}
	}
}
