using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using one_db.Data;
using one_db.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting; // Diperlukan untuk file upload
using System.IO; // Diperlukan untuk file upload
using Microsoft.AspNetCore.Http; // Diperlukan untuk IFormFile dan Session
using System.Net.Http;
using System.Text.Json;

namespace one_db.Controllers
{
	[Authorize] // Autentikasi diperlukan untuk semua action di controller ini
	public class MitraKepatuhanController : Controller
	{
		private readonly AppDBContext _context;
		private readonly ILogger<MitraKepatuhanController> _logger;
		private readonly IWebHostEnvironment _env; // Untuk menangani file upload
		private static readonly HttpClient _http = new HttpClient();

		public MitraKepatuhanController(AppDBContext context, ILogger<MitraKepatuhanController> logger, IWebHostEnvironment env)
		{
			_context = context;
			_logger = logger;
			_env = env; // Injeksi IWebHostEnvironment
		}

		// =================================================================
		// HALAMAN UNTUK MITRA (VENDOR)
		// =================================================================

		/// <summary>
		/// Halaman utama untuk Mitra.
		/// Menampilkan form pengajuan PT baru dan riwayat pengajuan mereka.
		/// </summary>
		public IActionResult Index()
		{
			try
			{
				// ✅ 1️⃣ Ambil data dari Claims & Session (Digabung)
				var kategoriUserId = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? HttpContext.Session.GetString("kategori_user_id");
				var compCode = User.FindFirst("comp_code")?.Value ?? HttpContext.Session.GetString("company");
				var nrp = User.Identity?.Name ?? HttpContext.Session.GetString("nrp");
				var dept = User.FindFirst("dept_code")?.Value ?? HttpContext.Session.GetString("dept");
				var nama = User.FindFirst("nama")?.Value ?? HttpContext.Session.GetString("nama");

				// 🚨 2️⃣ Cek login valid
				if (string.IsNullOrEmpty(kategoriUserId) || string.IsNullOrEmpty(nrp))
				{
					_logger.LogWarning("User tanpa sesi valid mencoba akses MitraKepatuhanController.Index.");
					return RedirectToAction("Index", "Login");
				}

				// ✅ 3️⃣ Dapatkan nama controller otomatis
				var controllerName = ControllerContext.ActionDescriptor.ControllerName;

				// 🔍 4️⃣ Cek akses lewat tabel RBAC
				bool punyaAkses = _context.tbl_r_menu.Any(x =>
					x.kategori_user_id == kategoriUserId &&
					(x.comp_code == null || x.comp_code == compCode) &&
					x.link_controller == controllerName);

				if (!punyaAkses)
				{
					_logger.LogWarning($"Akses ditolak: {nrp} ({kategoriUserId}) mencoba akses {controllerName}");
					// Arahkan ke halaman 'tidak punya akses' atau login lagi
					TempData["ErrorMessage"] = "Anda tidak memiliki hak akses ke menu ini.";
					return RedirectToAction("Index", "Dashboard"); // Atau controller default lain
				}

				// 📋 5️⃣ Ambil menu sesuai kategori & company (jika diperlukan untuk layout)
				var menuList = _context.tbl_r_menu
					.Where(x => x.kategori_user_id == kategoriUserId)
					.Where(x => x.comp_code == null || x.comp_code == compCode)
					.OrderBy(x => x.type)
					.ThenBy(x => x.title)
					.ToList();

				// 📊 6️⃣ Hitung jumlah menu (jika diperlukan untuk layout)
				ViewBag.MenuMasterCount = menuList.Count(x => x.type == "Master");
				// ... (Tambahkan hitungan lain jika perlu)

				// 📦 7️⃣ Data tambahan ke View
				ViewBag.Title = "Pengajuan Kepatuhan Mitra"; // Judul spesifik halaman
				ViewBag.Controller = controllerName;
				ViewBag.Setting = _context.tbl_m_setting_aplikasi.FirstOrDefault();
				ViewBag.Menu = menuList; // Untuk layout _Layout.cshtml
				ViewBag.insert_by = nrp;
				ViewBag.departemen = dept;
				ViewBag.nama = nama;
				ViewBag.company = compCode;

				_logger.LogInformation($"User {nrp} ({kategoriUserId}) mengakses {controllerName} di {compCode}");
				return View();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat load MitraKepatuhanController.Index");
				TempData["ErrorMessage"] = $"Terjadi kesalahan: {ex.Message}";
				return RedirectToAction("Index", "Login");
			}
		}


		/// <summary>
		/// [AJAX] Mengambil riwayat pengajuan HANYA untuk mitra yang sedang login.
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> GetMitraHistory()
		{
			try
			{
				// Hanya cek login dasar untuk AJAX
				var nrp = User.Identity?.Name ?? HttpContext.Session.GetString("nrp");
				if (string.IsNullOrEmpty(nrp))
				{
					return Json(new { success = false, message = "User tidak terautentikasi." });
				}

				var data = await _context.tbl_r_mitra_pengajuan
									.Where(p => p.insert_by == nrp)
									.OrderByDescending(p => p.created_at)
									.ToListAsync();

				return Json(new { success = true, data = data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error GetMitraHistory");
				return Json(new { success = false, message = ex.Message });
			}
		}

		/// <summary>
		/// [AJAX] Menerima data pengajuan PT baru dari Mitra.
		/// </summary>
		[HttpPost]
		public async Task<IActionResult> SubmitPengajuan([FromBody] tbl_r_mitra_pengajuan model)
		{
			if (model == null || string.IsNullOrWhiteSpace(model.nama_pt))
			{
				return Json(new { success = false, message = "Nama PT tidak boleh kosong." });
			}

			try
			{
				// Hanya cek login dasar untuk AJAX
				var nrp = User.Identity?.Name ?? HttpContext.Session.GetString("nrp");
				if (string.IsNullOrEmpty(nrp))
				{
					return Json(new { success = false, message = "User tidak terautentikasi." });
				}

				model.insert_by = nrp;
				model.created_at = DateTime.Now;
				model.status_pengajuan = "Menunggu Review"; // Status awal
				model.pt_owner = model.pt_owner?.Trim();
				model.email_pt = model.email_pt?.Trim();
				model.review_singkat = model.review_singkat?.Trim();
				model.upload_token = Guid.NewGuid().ToString("N");

				_context.tbl_r_mitra_pengajuan.Add(model);
				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "Pengajuan PT berhasil dikirim dan sedang menunggu review OHS." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error SubmitPengajuan");
				return Json(new { success = false, message = ex.Message });
			}
		}

		[HttpGet]
		public async Task<IActionResult> Upload(int id)
		{
			try
			{
				// --- Start: Auth & RBAC Check ---
				var kategoriUserId = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
					?? HttpContext.Session.GetString("kategori_user_id");
				var compCode = User.FindFirst("comp_code")?.Value
					?? HttpContext.Session.GetString("company");
				var nrp = User.Identity?.Name
					?? HttpContext.Session.GetString("nrp");
				var dept = User.FindFirst("dept_code")?.Value
					?? HttpContext.Session.GetString("dept");
				var nama = User.FindFirst("nama")?.Value
					?? HttpContext.Session.GetString("nama");

				if (string.IsNullOrEmpty(kategoriUserId) || string.IsNullOrEmpty(nrp))
				{
					_logger.LogWarning("User tanpa sesi valid mencoba akses MitraKepatuhanController.Upload.");
					return RedirectToAction("Index", "Login");
				}

				var controllerName = ControllerContext.ActionDescriptor.ControllerName;

				bool punyaAkses = await _context.tbl_r_menu.AnyAsync(x =>
					x.kategori_user_id == kategoriUserId &&
					(x.comp_code == null || x.comp_code == compCode) &&
					x.link_controller == controllerName);

				if (!punyaAkses)
				{
					_logger.LogWarning($"Akses ditolak: {nrp} ({kategoriUserId}) mencoba akses {controllerName}/Upload");
					TempData["ErrorMessage"] = "Anda tidak memiliki hak akses ke menu ini.";
					return RedirectToAction("Index", "Dashboard");
				}
				// --- End: Auth & RBAC Check ---

				// --- Start: Ambil data pengajuan ---
				var pengajuan = await _context.tbl_r_mitra_pengajuan.FindAsync(id);
				if (pengajuan == null)
				{
					_logger.LogWarning($"Upload gagal: pengajuan id={id} tidak ditemukan untuk {nrp}");
					TempData["ErrorMessage"] = "Data pengajuan tidak ditemukan.";
					return RedirectToAction("Index");
				}

				// --- Start: Security Check ---
				if (!string.Equals(pengajuan.insert_by, nrp, StringComparison.OrdinalIgnoreCase))
				{
					_logger.LogWarning($"Security violation: {nrp} mencoba akses upload milik {pengajuan.insert_by}");
					return Unauthorized("Anda tidak memiliki akses ke halaman ini.");
				}

				if (pengajuan.status_pengajuan != "Disetujui" && pengajuan.status_pengajuan != "Menunggu Upload Dokumen" && pengajuan.status_pengajuan != "Menunggu Review Dokumen")
				{
					_logger.LogWarning($"{nrp} mencoba upload, tapi status PT {pengajuan.status_pengajuan}");
					TempData["ErrorMessage"] = "Perusahaan Anda belum masuk tahap upload dokumen.";
					return RedirectToAction("Index");
				}
				// --- End: Security Check ---

				// --- Start: Ambil dokumen yang perlu di-upload ---
				var documentsToUpload = await _context.tbl_r_dokumen_mitra
					.Where(r => r.id_mitra_pengajuan == id)
					.Include(r => r.DokumenMaster)
					.Where(r => r.DokumenMaster != null)
					.OrderBy(r => r.DokumenMaster.grup)
					.ThenBy(r => r.DokumenMaster.id)
					.ToListAsync();

				if (documentsToUpload == null || !documentsToUpload.Any())
				{
					_logger.LogInformation($"Tidak ada dokumen upload untuk id pengajuan={id}, nrp={nrp}");
					TempData["InfoMessage"] = "Belum ada dokumen yang harus diunggah.";
				}
				// --- End: Ambil dokumen yang perlu di-upload ---

				// --- Start: Populate ViewBag ---
				var menuList = await _context.tbl_r_menu
					.Where(x => x.kategori_user_id == kategoriUserId &&
								(x.comp_code == null || x.comp_code == compCode))
					.OrderBy(x => x.type)
					.ThenBy(x => x.title)
					.ToListAsync();

				ViewBag.Title = $"Upload Dokumen: {pengajuan.nama_pt}";
				ViewBag.Controller = controllerName;
				ViewBag.Setting = await _context.tbl_m_setting_aplikasi.FirstOrDefaultAsync();
				ViewBag.Menu = menuList;
				ViewBag.insert_by = nrp;
				ViewBag.departemen = dept;
				ViewBag.nama = nama;
				ViewBag.company = compCode;
				ViewBag.Pengajuan = pengajuan;
				ViewBag.UploadToken = pengajuan.upload_token;
				ViewBag.IsPublic = false;
				// --- End: Populate ViewBag ---

				return View(documentsToUpload);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error GET Upload page untuk id={id}");
				TempData["ErrorMessage"] = $"Terjadi kesalahan sistem: {ex.Message}";
				return RedirectToAction("Index");
			}
		}

		/// <summary>
		/// Halaman Upload untuk link publik tanpa login (token-based).
		/// </summary>
		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> UploadPublic(int id, string token)
		{
			var pengajuan = await _context.tbl_r_mitra_pengajuan.FindAsync(id);
			if (pengajuan == null || string.IsNullOrWhiteSpace(token) || !string.Equals(pengajuan.upload_token, token, StringComparison.Ordinal))
			{
				return Unauthorized("Link tidak valid atau sudah kadaluarsa.");
			}

			var documentsToUpload = await _context.tbl_r_dokumen_mitra
				.Where(r => r.id_mitra_pengajuan == id)
				.Include(r => r.DokumenMaster)
				.Where(r => r.DokumenMaster != null)
				.OrderBy(r => r.DokumenMaster.grup)
				.ThenBy(r => r.DokumenMaster.id)
				.ToListAsync();

			ViewBag.Title = $"Upload Dokumen: {pengajuan.nama_pt}";
			ViewBag.Controller = "MitraKepatuhan";
			ViewBag.Setting = await _context.tbl_m_setting_aplikasi.FirstOrDefaultAsync();
			ViewBag.Menu = null;
			ViewBag.Pengajuan = pengajuan;
			ViewBag.UploadToken = token;
			ViewBag.IsPublic = true;

			return View("Upload", documentsToUpload);
		}

		/// <summary>
		/// [AJAX] Menerima file yang diunggah oleh Mitra.
		/// ID di sini adalah ID DOKUMEN (id dari tbl_r_dokumen_mitra)
		/// </summary>
		[HttpPost]
		public async Task<IActionResult> SubmitFileUpload(int id, IFormFile file)
		{
			// Cek login dasar
			var nrp = User.Identity?.Name ?? HttpContext.Session.GetString("nrp");
			var token = Request.Form["upload_token"].FirstOrDefault();

			if (file == null || file.Length == 0)
			{
				return Json(new { success = false, message = "File tidak ditemukan." });
			}

			var allowedExtensions = new[] { ".pdf", ".xls", ".xlsx" };
			var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!allowedExtensions.Contains(extension))
			{
				return Json(new { success = false, message = "Format file harus PDF atau Excel." });
			}

			if (file.Length > (10 * 1024 * 1024)) // 10MB
			{
				return Json(new { success = false, message = "Ukuran file maksimal 10MB." });
			}

			try
			{
				var docItem = await _context.tbl_r_dokumen_mitra
								.Include(d => d.MitraPengajuan)
								.FirstOrDefaultAsync(d => d.id == id);

				if (docItem == null) return Json(new { success = false, message = "Data dokumen tidak ditemukan." });

				// Security check
				var isOwner = !string.IsNullOrEmpty(nrp) && docItem.MitraPengajuan.insert_by == nrp;
				var tokenMatch = !string.IsNullOrEmpty(token) && token == docItem.MitraPengajuan.upload_token;
				if (!isOwner && !tokenMatch)
				{
					_logger.LogWarning($"Security violation: upload file id {id} tanpa hak akses");
					return Json(new { success = false, message = "Akses tidak sah. Token atau sesi tidak valid." });
				}

				// Hapus file lama jika ada
				if (!string.IsNullOrEmpty(docItem.file_path))
				{
					string oldFilePath = Path.Combine(_env.WebRootPath, docItem.file_path.TrimStart('/'));
					if (System.IO.File.Exists(oldFilePath))
					{
						try { System.IO.File.Delete(oldFilePath); }
						catch (IOException ioEx) { _logger.LogWarning(ioEx, $"Gagal menghapus file lama: {oldFilePath}"); }
					}
				}

				// Simpan file baru
				string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}"; // Gunakan Path.GetFileName
				string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "mitra_docs");
				Directory.CreateDirectory(uploadsFolder); // Buat folder jika belum ada
				string filePath = Path.Combine(uploadsFolder, uniqueFileName);

				await using (var fileStream = new FileStream(filePath, FileMode.Create))
				{
					await file.CopyToAsync(fileStream);
				}

				// Update database
				docItem.status_dokumen = "Sudah Diunggah";
				docItem.file_path = $"/uploads/mitra_docs/{uniqueFileName}";
				docItem.file_name_original = file.FileName;
				docItem.uploaded_at = DateTime.Now;
				docItem.catatan_review = null;
				docItem.review_by = null;
				docItem.review_at = null;

				if (docItem.MitraPengajuan != null)
				{
					docItem.MitraPengajuan.status_pengajuan = "Menunggu Review Dokumen";
				}

				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "File berhasil diunggah.", fileName = file.FileName, path = docItem.file_path });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error SubmitFileUpload");
				return Json(new { success = false, message = $"Error server: {ex.Message}" });
			}
		}


		// =================================================================
		// HALAMAN UNTUK ADMIN OHS
		// =================================================================

		/// <summary>
		/// Halaman Dashboard utama untuk Admin OHS.
		/// Menampilkan daftar semua pengajuan PT.
		/// </summary>
		[Authorize] // Asumsi role, sesuaikan dengan RBAC Anda
		public IActionResult Approval()
		{
			try
			{
				// ✅ 1️⃣ Ambil data dari Claims & Session (Digabung)
				var kategoriUserId = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? HttpContext.Session.GetString("kategori_user_id");
				var compCode = User.FindFirst("comp_code")?.Value ?? HttpContext.Session.GetString("company");
				var nrp = User.Identity?.Name ?? HttpContext.Session.GetString("nrp");
				var dept = User.FindFirst("dept_code")?.Value ?? HttpContext.Session.GetString("dept");
				var nama = User.FindFirst("nama")?.Value ?? HttpContext.Session.GetString("nama");

				// 🚨 2️⃣ Cek login valid
				if (string.IsNullOrEmpty(kategoriUserId) || string.IsNullOrEmpty(nrp))
				{
					_logger.LogWarning("User tanpa sesi valid mencoba akses MitraKepatuhanController.Index.");
					return RedirectToAction("Index", "Login");
				}

				// ✅ 3️⃣ Dapatkan nama controller otomatis
				var controllerName = ControllerContext.ActionDescriptor.ControllerName;

				// 🔍 4️⃣ Cek akses lewat tabel RBAC
				bool punyaAkses = _context.tbl_r_menu.Any(x =>
					x.kategori_user_id == kategoriUserId &&
					(x.comp_code == null || x.comp_code == compCode) &&
					x.link_controller == controllerName);

				if (!punyaAkses)
				{
					_logger.LogWarning($"Akses ditolak: {nrp} ({kategoriUserId}) mencoba akses {controllerName}");
					// Arahkan ke halaman 'tidak punya akses' atau login lagi
					TempData["ErrorMessage"] = "Anda tidak memiliki hak akses ke menu ini.";
					return RedirectToAction("Index", "Dashboard"); // Atau controller default lain
				}

				// 📋 5️⃣ Ambil menu sesuai kategori & company (jika diperlukan untuk layout)
				var menuList = _context.tbl_r_menu
					.Where(x => x.kategori_user_id == kategoriUserId)
					.Where(x => x.comp_code == null || x.comp_code == compCode)
					.OrderBy(x => x.type)
					.ThenBy(x => x.title)
					.ToList();

				// 📊 6️⃣ Hitung jumlah menu (jika diperlukan untuk layout)
				ViewBag.MenuMasterCount = menuList.Count(x => x.type == "Master");
				// ... (Tambahkan hitungan lain jika perlu)

				// 📦 7️⃣ Data tambahan ke View
				ViewBag.Title = "Pengajuan Kepatuhan Mitra"; // Judul spesifik halaman
				ViewBag.Controller = controllerName;
				ViewBag.Setting = _context.tbl_m_setting_aplikasi.FirstOrDefault();
				ViewBag.Menu = menuList; // Untuk layout _Layout.cshtml
				ViewBag.insert_by = nrp;
				ViewBag.departemen = dept;
				ViewBag.nama = nama;
				ViewBag.company = compCode;

				_logger.LogInformation($"User {nrp} ({kategoriUserId}) mengakses {controllerName} di {compCode}");
				return View();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat load MitraKepatuhanController.Index");
				TempData["ErrorMessage"] = $"Terjadi kesalahan: {ex.Message}";
				return RedirectToAction("Index", "Login");
			}
		}

		// =========================== WILAYAH PROXY (hindari CORS) ===========================
		[HttpGet]
		public async Task<IActionResult> WilayahProvinces()
		{
			var data = await FetchWilayahAsync("https://wilayah.id/api/provinces.json");
			return Json(new { data });
		}

		[HttpGet]
		public async Task<IActionResult> WilayahRegencies(string code)
		{
			if (string.IsNullOrWhiteSpace(code))
				return Json(new { data = new List<object>() });
			var data = await FetchWilayahAsync($"https://wilayah.id/api/regencies/{code}.json");
			return Json(new { data });
		}

		[HttpGet]
		public async Task<IActionResult> WilayahDistricts(string code)
		{
			if (string.IsNullOrWhiteSpace(code))
				return Json(new { data = new List<object>() });
			var data = await FetchWilayahAsync($"https://wilayah.id/api/districts/{code}.json");
			return Json(new { data });
		}

		[HttpGet]
		public async Task<IActionResult> WilayahVillages(string code)
		{
			if (string.IsNullOrWhiteSpace(code))
				return Json(new { data = new List<object>() });
			var data = await FetchWilayahAsync($"https://wilayah.id/api/villages/{code}.json");
			return Json(new { data });
		}

		private async Task<List<object>> FetchWilayahAsync(string url)
		{
			var result = new List<object>();
			try
			{
				using var resp = await _http.GetAsync(url);
				resp.EnsureSuccessStatusCode();
				await using var stream = await resp.Content.ReadAsStreamAsync();
				using var doc = await JsonDocument.ParseAsync(stream);
				if (doc.RootElement.ValueKind == JsonValueKind.Array)
				{
					foreach (var item in doc.RootElement.EnumerateArray())
					{
						var code = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
						var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
						if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
							continue;
						result.Add(new { code, name });
					}
				}
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Gagal memanggil API wilayah {Url}", url);
			}
			return result;
		}

		/// <summary>
		/// [AJAX] Mengambil semua data pengajuan untuk dashboard Admin OHS.
		/// </summary>
		[HttpGet]
		[Authorize]
		public async Task<IActionResult> GetAdminDashboard()
		{
			try
			{
				// Hanya cek login dasar untuk AJAX
				var nrp = User.Identity?.Name ?? HttpContext.Session.GetString("nrp");
				if (string.IsNullOrEmpty(nrp))
				{
					return Json(new { success = false, message = "User tidak terautentikasi." });
				}

				var data = await _context.tbl_r_mitra_pengajuan
									.OrderByDescending(p => p.created_at)
									.ToListAsync();

				return Json(new { success = true, data = data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error GetAdminDashboard");
				return Json(new { success = false, message = ex.Message });
			}
		}


		/// <summary>
		/// Halaman Detail untuk OHS mereview PT dan memilih dokumen wajib.
		/// ID di sini adalah ID PENGajuan (id_mitra_pengajuan).
		/// </summary>
		[HttpGet]
		[Authorize]
		public async Task<IActionResult> Review(int id)
		{
			try
			{
				// --- Start: Auth & RBAC Check ---
				var kategoriUserId = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? HttpContext.Session.GetString("kategori_user_id");
				var compCode = User.FindFirst("comp_code")?.Value ?? HttpContext.Session.GetString("company");
				var nrp = User.Identity?.Name ?? HttpContext.Session.GetString("nrp");
				var dept = User.FindFirst("dept_code")?.Value ?? HttpContext.Session.GetString("dept");
				var nama = User.FindFirst("nama")?.Value ?? HttpContext.Session.GetString("nama");

				if (string.IsNullOrEmpty(kategoriUserId) || string.IsNullOrEmpty(nrp))
				{
					_logger.LogWarning("User tanpa sesi valid mencoba akses MitraKepatuhanController.Review.");
					return RedirectToAction("Index", "Login");
				}
				var controllerName = ControllerContext.ActionDescriptor.ControllerName;
				bool punyaAkses = _context.tbl_r_menu.Any(x =>
					x.kategori_user_id == kategoriUserId &&
					(x.comp_code == null || x.comp_code == compCode) &&
					x.link_controller == controllerName);
				if (!punyaAkses)
				{
					_logger.LogWarning($"Akses ditolak: {nrp} ({kategoriUserId}) mencoba akses {controllerName}/Review/{id}");
					TempData["ErrorMessage"] = "Anda tidak memiliki hak akses ke menu ini.";
					return RedirectToAction("Index", "Dashboard");
				}
				// --- End: Auth & RBAC Check ---

				var pengajuan = await _context.tbl_r_mitra_pengajuan.FindAsync(id);
				if (pengajuan == null) return NotFound();

				var allDocuments = await _context.tbl_m_dokumen_kepatuhan
											.Where(d => d.is_active)
											.OrderBy(d => d.grup).ThenBy(d => d.id)
											.ToListAsync();

				var existingRequirements = await _context.tbl_r_dokumen_mitra
													.Where(r => r.id_mitra_pengajuan == id)
													.Select(r => r.id_dokumen_master)
													.ToListAsync();

				var viewModel = new MitraReviewViewModel
				{
					Pengajuan = pengajuan,
					AllMasterDocuments = allDocuments,
					RequiredDocumentIds = existingRequirements
				};

				// --- Start: Populate ViewBag ---
				var menuList = await _context.tbl_r_menu
					.Where(x => x.kategori_user_id == kategoriUserId && (x.comp_code == null || x.comp_code == compCode))
					.OrderBy(x => x.type).ThenBy(x => x.title).ToListAsync();

				ViewBag.Title = $"Review Pengajuan: {pengajuan.nama_pt}";
				ViewBag.Controller = controllerName;
				ViewBag.Setting = await _context.tbl_m_setting_aplikasi.FirstOrDefaultAsync();
				ViewBag.Menu = menuList;
				ViewBag.insert_by = nrp;
				ViewBag.departemen = dept;
				ViewBag.nama = nama;
				ViewBag.company = compCode;
				// --- End: Populate ViewBag ---

				return View(viewModel);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error GET Review page");
				TempData["ErrorMessage"] = $"Terjadi kesalahan: {ex.Message}";
				return RedirectToAction("Approval");
			}
		}


		/// <summary>
		/// [POST] Menyimpan hasil review OHS (Approve/Reject PT dan daftar dokumen wajib).
		/// </summary>
		[HttpPost]
		[Authorize]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SubmitReview(MitraReviewViewModel model)
		{
			// Cek login dasar
			var adminNrp = User.Identity?.Name ?? HttpContext.Session.GetString("nrp");
			if (string.IsNullOrEmpty(adminNrp))
			{
				TempData["ErrorMessage"] = "Sesi Anda telah berakhir.";
				return RedirectToAction("Index", "Login");
			}

			if (model == null || model.Pengajuan == null)
			{
				return RedirectToAction("Approval");
			}

			try
			{
				int idPengajuan = model.Pengajuan.id;
				var pengajuanInDb = await _context.tbl_r_mitra_pengajuan.FindAsync(idPengajuan);
				if (pengajuanInDb == null) return NotFound();

				var allowIncomplete = string.Equals(Request.Form["approve_incomplete"], "true", StringComparison.OrdinalIgnoreCase);
				var incompleteReason = Request.Form["approve_incomplete_reason"].ToString();
				var proofPath = Request.Form["approve_incomplete_proof"].ToString();

				var requestedStatus = model.Pengajuan.status_pengajuan;
				if (requestedStatus == "Disetujui" || requestedStatus == "Menunggu Upload Dokumen")
				{
					pengajuanInDb.status_pengajuan = "Menunggu Upload Dokumen";
				}
				else
				{
					pengajuanInDb.status_pengajuan = requestedStatus;
				}
				pengajuanInDb.catatan_admin = model.Pengajuan.catatan_admin;
				pengajuanInDb.review_by = adminNrp;
				pengajuanInDb.review_at = DateTime.Now;
				pengajuanInDb.pt_owner = model.Pengajuan.pt_owner ?? pengajuanInDb.pt_owner;
				pengajuanInDb.remark = model.Pengajuan.remark;

				var existingDocs = await _context.tbl_r_dokumen_mitra
					.Where(r => r.id_mitra_pengajuan == idPengajuan)
					.ToListAsync();
				var hasIncompleteDocs = existingDocs.Any(d => d.status_dokumen != "Disetujui");

				if ((requestedStatus == "Disetujui" || requestedStatus == "Menunggu Upload Dokumen") && hasIncompleteDocs && !allowIncomplete)
				{
					TempData["ErrorMessage"] = "Masih ada dokumen wajib yang belum disetujui. Aktifkan opsi 'Setujui meski belum lengkap' dengan bukti dan alasan.";
					return RedirectToAction("Review", new { id = idPengajuan });
				}

				if (allowIncomplete)
				{
					if (string.IsNullOrWhiteSpace(incompleteReason) || string.IsNullOrWhiteSpace(proofPath))
					{
						TempData["ErrorMessage"] = "Alasan dan bukti wajib diisi saat menyetujui dengan dokumen belum lengkap.";
						return RedirectToAction("Review", new { id = idPengajuan });
					}
					var appendedNote = $"[APPROVED W/ MISSING DOCS] {incompleteReason} | Bukti: {proofPath}";
					pengajuanInDb.catatan_admin = string.IsNullOrWhiteSpace(pengajuanInDb.catatan_admin)
						? appendedNote
						: $"{pengajuanInDb.catatan_admin} || {appendedNote}";
				}

				if (requestedStatus == "Disetujui" || requestedStatus == "Menunggu Upload Dokumen")
				{
					var selectedDocIds = model.RequiredDocumentIds ?? new List<int>();

					var docsToRemove = existingDocs.Where(e => !selectedDocIds.Contains(e.id_dokumen_master));
					_context.tbl_r_dokumen_mitra.RemoveRange(docsToRemove);

					var existingDocIdsInDb = existingDocs.Select(e => e.id_dokumen_master).ToList();
					var docsToAddIds = selectedDocIds.Where(selectedId => !existingDocIdsInDb.Contains(selectedId));

					foreach (var docId in docsToAddIds)
					{
						var newDoc = new tbl_r_dokumen_mitra
						{
							id_mitra_pengajuan = idPengajuan,
							id_dokumen_master = docId,
							status_dokumen = "Wajib Diunggah"
						};
						_context.tbl_r_dokumen_mitra.Add(newDoc);
					}
				}
				else
				{
					// Jika status bukan disetujui, bersihkan daftar dokumen wajib
					var existingDocsToClear = await _context.tbl_r_dokumen_mitra
						.Where(r => r.id_mitra_pengajuan == idPengajuan)
						.ToListAsync();
					if (existingDocsToClear.Any())
						_context.tbl_r_dokumen_mitra.RemoveRange(existingDocsToClear);
				}

				await _context.SaveChangesAsync();
				_logger.LogInformation($"Admin {adminNrp} mereview {pengajuanInDb.nama_pt} dengan status {pengajuanInDb.status_pengajuan}");

				TempData["SuccessMessage"] = $"Status pengajuan untuk {pengajuanInDb.nama_pt} berhasil diubah.";
				return RedirectToAction("Approval");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error SubmitReview");
				TempData["ErrorMessage"] = $"Terjadi kesalahan saat menyimpan review: {ex.Message}";
				return RedirectToAction("Review", new { id = model.Pengajuan.id });
			}
		}


		/// <summary>
		/// Halaman Detail untuk OHS mereview file-file yang sudah diupload Mitra.
		/// ID di sini adalah ID PENGajuan (id_mitra_pengajuan).
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> ReviewDocuments(int id)
		{
			try
			{
				// --- Start: Auth & RBAC Check ---
				var kategoriUserId = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
					?? HttpContext.Session.GetString("kategori_user_id");
				var compCode = User.FindFirst("comp_code")?.Value
					?? HttpContext.Session.GetString("company");
				var nrp = User.Identity?.Name
					?? HttpContext.Session.GetString("nrp");
				var dept = User.FindFirst("dept_code")?.Value
					?? HttpContext.Session.GetString("dept");
				var nama = User.FindFirst("nama")?.Value
					?? HttpContext.Session.GetString("nama");

				// 🚨 Cek validitas sesi
				if (string.IsNullOrEmpty(kategoriUserId) || string.IsNullOrEmpty(nrp))
				{
					_logger.LogWarning("User tanpa sesi valid mencoba akses MitraKepatuhanController.ReviewDocuments.");
					return RedirectToAction("Index", "Login");
				}

				var controllerName = ControllerContext.ActionDescriptor.ControllerName;

				bool punyaAkses = await _context.tbl_r_menu.AnyAsync(x =>
					x.kategori_user_id == kategoriUserId &&
					(x.comp_code == null || x.comp_code == compCode) &&
					x.link_controller == controllerName);

				if (!punyaAkses)
				{
					_logger.LogWarning($"Akses ditolak: {nrp} ({kategoriUserId}) mencoba akses {controllerName}/ReviewDocuments/{id}");
					TempData["ErrorMessage"] = "Anda tidak memiliki hak akses ke menu ini.";
					return RedirectToAction("Index", "Dashboard");
				}
				// --- End: Auth & RBAC Check ---

				// --- Start: Ambil data pengajuan ---
				var pengajuan = await _context.tbl_r_mitra_pengajuan.FindAsync(id);
				if (pengajuan == null)
				{
					_logger.LogWarning($"ReviewDocuments gagal: pengajuan id={id} tidak ditemukan untuk {nrp}");
					TempData["ErrorMessage"] = "Data pengajuan tidak ditemukan.";
					return RedirectToAction("Approval");
				}
				// --- End: Ambil data pengajuan ---

				// --- Start: Ambil dokumen untuk direview ---
				var documentsToReview = await _context.tbl_r_dokumen_mitra
					.Where(r => r.id_mitra_pengajuan == id)
					.Include(r => r.DokumenMaster)
					.Where(r => r.DokumenMaster != null)
					.OrderBy(r => r.DokumenMaster.grup)
					.ThenBy(r => r.DokumenMaster.id)
					.ToListAsync();

				if (documentsToReview == null || !documentsToReview.Any())
				{
					_logger.LogInformation($"Tidak ada dokumen untuk direview pada pengajuan id={id}, nrp={nrp}");
					TempData["InfoMessage"] = "Belum ada dokumen yang dapat direview untuk pengajuan ini.";
				}
				// --- End: Ambil dokumen untuk direview ---

				// --- Start: Populate ViewBag ---
				var menuList = await _context.tbl_r_menu
					.Where(x => x.kategori_user_id == kategoriUserId &&
								(x.comp_code == null || x.comp_code == compCode))
					.OrderBy(x => x.type)
					.ThenBy(x => x.title)
					.ToListAsync();

				ViewBag.Title = $"Review Dokumen: {pengajuan.nama_pt}";
				ViewBag.Controller = controllerName;
				ViewBag.Setting = await _context.tbl_m_setting_aplikasi.FirstOrDefaultAsync();
				ViewBag.Menu = menuList;
				ViewBag.insert_by = nrp;
				ViewBag.departemen = dept;
				ViewBag.nama = nama;
				ViewBag.company = compCode;
				ViewBag.Pengajuan = pengajuan;
				// --- End: Populate ViewBag ---

				_logger.LogInformation($"User {nrp} membuka halaman ReviewDocuments untuk id pengajuan={id} ({pengajuan.nama_pt})");

				return View(documentsToReview);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error GET ReviewDocuments page untuk id={id}");
				TempData["ErrorMessage"] = $"Terjadi kesalahan sistem: {ex.Message}";
				return RedirectToAction("Approval");
			}
		}


		/// <summary>
		/// Upload bukti ketika menyetujui meski dokumen belum lengkap.
		/// </summary>
		[HttpPost]
		[Authorize]
		public async Task<IActionResult> UploadApprovalProof(int pengajuanId, IFormFile proof)
		{
			var adminNrp = User.Identity?.Name ?? HttpContext.Session.GetString("nrp");
			if (string.IsNullOrEmpty(adminNrp))
				return Json(new { success = false, message = "Sesi Anda telah berakhir." });

			if (proof == null || proof.Length == 0)
				return Json(new { success = false, message = "File bukti tidak ditemukan." });

			var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
			var ext = Path.GetExtension(proof.FileName).ToLowerInvariant();
			if (!allowedExtensions.Contains(ext))
				return Json(new { success = false, message = "Format bukti harus JPG, PNG, atau PDF." });

			if (proof.Length > (10 * 1024 * 1024))
				return Json(new { success = false, message = "Ukuran bukti maksimal 10MB." });

			try
			{
				var pengajuan = await _context.tbl_r_mitra_pengajuan.FindAsync(pengajuanId);
				if (pengajuan == null)
					return Json(new { success = false, message = "Pengajuan tidak ditemukan." });

				var folder = Path.Combine(_env.WebRootPath, "uploads", "mitra_docs", "approval_proof");
				Directory.CreateDirectory(folder);
				var fileName = $"proof_{pengajuanId}_{Guid.NewGuid():N}{ext}";
				var fullPath = Path.Combine(folder, fileName);

				await using (var stream = new FileStream(fullPath, FileMode.Create))
				{
					await proof.CopyToAsync(stream);
				}

				var relativePath = $"/uploads/mitra_docs/approval_proof/{fileName}";
				return Json(new { success = true, path = relativePath, fileName = proof.FileName });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Gagal upload bukti approval tidak lengkap");
				return Json(new { success = false, message = $"Gagal mengunggah bukti: {ex.Message}" });
			}
		}

		/// <summary>
		/// [AJAX] Menyimpan hasil review OHS per file (Approve/Reject FILE)
		/// ID di sini adalah ID DOKUMEN (id dari tbl_r_dokumen_mitra)
		/// </summary>
		[HttpPost]
		[Authorize]
		public async Task<IActionResult> SubmitFileReview(int id, string status, string catatan)
		{
			// Cek login dasar
			var adminNrp = User.Identity?.Name ?? HttpContext.Session.GetString("nrp");
			if (string.IsNullOrEmpty(adminNrp))
			{
				return Json(new { success = false, message = "Sesi Anda telah berakhir." });
			}

			if (string.IsNullOrEmpty(status) || (status == "Ditolak" && string.IsNullOrEmpty(catatan)))
			{
				return Json(new { success = false, message = "Status dan catatan (jika ditolak) wajib diisi." });
			}

			try
			{
				var docItem = await _context.tbl_r_dokumen_mitra.FindAsync(id);
				if (docItem == null) return Json(new { success = false, message = "Data dokumen tidak ditemukan." });

				docItem.status_dokumen = status; // "Disetujui" atau "Ditolak"
				docItem.catatan_review = catatan;
				docItem.review_by = adminNrp;
				docItem.review_at = DateTime.Now;

				await _context.SaveChangesAsync();

				// Update status pengajuan berdasarkan koleksi dokumen
				var parent = await _context.tbl_r_mitra_pengajuan.FirstOrDefaultAsync(p => p.id == docItem.id_mitra_pengajuan);
				if (parent != null)
				{
					var docs = await _context.tbl_r_dokumen_mitra.Where(d => d.id_mitra_pengajuan == parent.id).ToListAsync();
					if (docs.All(d => d.status_dokumen == "Disetujui"))
					{
						parent.status_pengajuan = "Disetujui";
					}
					else if (docs.Any(d => d.status_dokumen == "Ditolak"))
					{
						parent.status_pengajuan = "Perlu Perbaikan Upload";
					}
					else
					{
						parent.status_pengajuan = "Menunggu Review Dokumen";
					}
					await _context.SaveChangesAsync();
				}

				return Json(new { success = true, message = "Status file berhasil diperbarui." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error SubmitFileReview");
				return Json(new { success = false, message = ex.Message });
			}
		}

		/// <summary>
		/// Finalisasi review dokumen dari halaman ReviewDocuments (dapat menyetujui meski belum lengkap dengan bukti).
		/// </summary>
		[HttpPost]
		[Authorize]
		public async Task<IActionResult> FinalizeDocuments(int pengajuanId, bool approveIncomplete, string? reason, string? proofPath)
		{
			var adminNrp = User.Identity?.Name ?? HttpContext.Session.GetString("nrp");
			if (string.IsNullOrEmpty(adminNrp))
				return Json(new { success = false, message = "Sesi berakhir." });

			var pengajuan = await _context.tbl_r_mitra_pengajuan.FindAsync(pengajuanId);
			if (pengajuan == null)
				return Json(new { success = false, message = "Pengajuan tidak ditemukan." });

			var docs = await _context.tbl_r_dokumen_mitra.Where(d => d.id_mitra_pengajuan == pengajuanId).ToListAsync();
			var hasIncomplete = docs.Any(d => d.status_dokumen != "Disetujui");

			if (hasIncomplete && !approveIncomplete)
				return Json(new { success = false, message = "Masih ada dokumen belum disetujui. Aktifkan opsi setujui meski belum lengkap." });

			if (approveIncomplete && hasIncomplete)
			{
				if (string.IsNullOrWhiteSpace(reason) || string.IsNullOrWhiteSpace(proofPath))
					return Json(new { success = false, message = "Alasan dan bukti wajib diisi untuk menyetujui meski belum lengkap." });

				var note = $"[APPROVED W/ MISSING DOCS] {reason} | Bukti: {proofPath}";
				pengajuan.catatan_admin = string.IsNullOrWhiteSpace(pengajuan.catatan_admin)
					? note
					: $"{pengajuan.catatan_admin} || {note}";
			}

			pengajuan.status_pengajuan = "Disetujui";
			pengajuan.review_by = adminNrp;
			pengajuan.review_at = DateTime.Now;

			await _context.SaveChangesAsync();

			return Json(new { success = true, message = "Pengajuan difinalisasi." });
		}
	}

	/// <summary>
	/// ViewModel khusus untuk halaman Review OHS
	/// </summary>
	public class MitraReviewViewModel
	{
		public tbl_r_mitra_pengajuan Pengajuan { get; set; }
		public List<tbl_m_dokumen_kepatuhan> AllMasterDocuments { get; set; }
		public List<int> RequiredDocumentIds { get; set; } = new List<int>(); // Untuk menampung ID checkbox
	}
}

