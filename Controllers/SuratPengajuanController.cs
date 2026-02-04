using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using one_db.Data;
using one_db.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
// Tambahan untuk rendering view dan PDF
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.IO;
using QRCoder;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Html2pdf;
using iText.Kernel.Utils;
using System.Collections.Generic;
using iText.Layout.Element;
// Diperlukan untuk IWebHostEnvironment
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace one_db.Controllers
{
	[Authorize]
	public class SuratPengajuanController : Controller
	{
		private readonly AppDBContext _context;
		private readonly ILogger<SuratPengajuanController> _logger;
		private readonly IWebHostEnvironment _env;
		private readonly ICompositeViewEngine _viewEngine;
		private readonly ITempDataProvider _tempDataProvider;

		private const string controller_name = "SuratPengajuan";
		private const string title_name = "SuratPengajuan";

		// Konstanta Status
		private const string STATUS_MENUNGGU_REVIEW = "Menunggu Review";
		private const string STATUS_DITOLAK_REVIEW = "Ditolak Review";
		private const string STATUS_MENUNGGU_MANAGER = "Menunggu Persetujuan Manager";
		private const string STATUS_DITOLAK_MANAGER = "Ditolak Manager";
		private const string STATUS_SIAP_PROSES = "Siap Proses"; // Tetap ada untuk Non-SKK
		private const string STATUS_SELESAI = "Selesai";
		private const string STATUS_DITOLAK_ADMIN = "Ditolak Admin"; // Untuk penolakan di tahap final (Non-SKK)

		// Konstanta Jenis Pengajuan
		private const string JENIS_SKK = "Surat Keterangan Kerja";
		private const string JENIS_PERNIKAHAN = "Hadiah Pernikahan";
		private const string JENIS_DUKA = "Santunan Duka";


		public SuratPengajuanController(
			AppDBContext context,
			ILogger<SuratPengajuanController> logger,
			IWebHostEnvironment env,
			ICompositeViewEngine viewEngine,
			ITempDataProvider tempDataProvider)
		{
			_context = context;
			_logger = logger;
			_env = env;
			_viewEngine = viewEngine;
			_tempDataProvider = tempDataProvider;
		}


		[Authorize]
		public IActionResult Index() // Halaman untuk User Biasa
		{
			// ... (Kode Index tetap sama) ...
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
					_logger.LogWarning("User tanpa kategori_user_id mencoba akses Controller."); // Pesan lebih generik
					return RedirectToAction("Index", "Login");
				}

				// ✅ 4️⃣ Dapatkan nama controller otomatis
				var controllerName = ControllerContext.ActionDescriptor.ControllerName; // Harus SuratPengajuan

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

				// 📊 7️⃣ Hitung jumlah tiap kategori menu (opsional, bisa dihapus jika tidak dipakai di layout)
				// ViewBag.MenuMasterCount = menuList.Count(x => x.type == "Master");
				// ... (hitung count lainnya) ...

				// 📦 8️⃣ Data tambahan ke View
				ViewBag.Title = title_name; // Gunakan konstanta
				ViewBag.Controller = controllerName;
				ViewBag.Setting = _context.tbl_m_setting_aplikasi.FirstOrDefault();
				ViewBag.Menu = menuList; // Kirim menu ke layout
				ViewBag.insert_by = nrp;
				ViewBag.departemen = dept;
				ViewBag.nama = nama;
				ViewBag.company = compCode;

				_logger.LogInformation($"User {nrp} ({kategoriUserId}) mengakses {controllerName} di {compCode}");
				return View(); // Mengembalikan View Index.cshtml untuk User
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat load controller {controller}", controller_name); // Log controller name
				return RedirectToAction("Index", "Login");
			}
		}


		[Authorize]
		public IActionResult Manager() // Halaman untuk Manager
		{
			// ... (Kode ApprovalManager tetap sama) ...
			try
			{
				// ✅ Ambil data dari Claims dulu, fallback ke Session
				var kategoriUserId = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
					?? HttpContext.Session.GetString("kategori_user_id");
				var compCode = User.FindFirst("comp_code")?.Value
					?? HttpContext.Session.GetString("company");
				var nrp = User.Identity?.Name ?? HttpContext.Session.GetString("nrp");
				var nama = User.FindFirst("nama")?.Value ?? HttpContext.Session.GetString("nama"); // Ambil nama
				var dept = User.FindFirst("dept_code")?.Value ?? HttpContext.Session.GetString("dept"); // Ambil dept


				if (string.IsNullOrEmpty(kategoriUserId) || string.IsNullOrEmpty(nrp)) // Cek nrp juga
				{
					_logger.LogWarning("User tanpa kategori_user_id/nrp mencoba akses ApprovalManager.");
					return RedirectToAction("Index", "Login");
				}

				// ✅ Ambil nama controller otomatis (biar fleksibel)
				var controllerName = "SuratPengajuan"; // Nama controller spesifik
				var actionName = ControllerContext.ActionDescriptor.ActionName; // Nama action saat ini (ApprovalManager)

				// 🔒 RBAC check: pastikan user punya akses ke ACTION ini
				// Sebaiknya cek berdasarkan kombinasi controller & action atau link_function
				bool punyaAkses = _context.tbl_r_menu.Any(x =>
				   x.kategori_user_id == kategoriUserId &&
				   (x.comp_code == null || x.comp_code == compCode) &&
				   x.link_controller == controllerName && // Pastikan controller cocok
				   x.link_function == actionName);       // Pastikan action cocok

				if (!punyaAkses)
				{
					_logger.LogWarning($"Akses ditolak: {nrp} ({kategoriUserId}) mencoba akses {controllerName}/{actionName}");
					return RedirectToAction("Index", "Login"); // Atau halaman "Unauthorized"
				}

				// 📋 Ambil semua menu untuk sidebar
				var menuList = _context.tbl_r_menu
					.Where(x => x.kategori_user_id == kategoriUserId)
					.Where(x => x.comp_code == null || x.comp_code == compCode)
					.OrderBy(x => x.type)
					.ThenBy(x => x.title)
					.ToList();

				// 📦 Siapkan ViewBag untuk layout dan view
				ViewBag.Title = "Approval Manager"; // Judul spesifik halaman
				ViewBag.Controller = controllerName; // Nama controller
				ViewBag.Menu = menuList; // Untuk sidebar layout
				ViewBag.Setting = _context.tbl_m_setting_aplikasi.FirstOrDefault();
				ViewBag.insert_by = nrp; // Untuk layout
				ViewBag.nama = nama; // Untuk layout
				ViewBag.departemen = dept; // Untuk layout
				ViewBag.company = compCode; // Untuk layout

				_logger.LogInformation($"User {nrp} ({kategoriUserId}) mengakses {controllerName}/{actionName}");
				return View(); // Mengembalikan View ApprovalManager.cshtml
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat load menu Approval Manager");
				return RedirectToAction("Index", "Login");
			}
		}


		// Hapus method Manager(), karena sepertinya duplikat/tidak sesuai
		// public IActionResult Manager() { ... }

		[Authorize] // Tambahkan Authorize
		public IActionResult Approval() // Halaman untuk Admin
		{
			// ... (Kode Approval mirip dengan ApprovalManager, pastikan RBAC benar) ...
			try
			{
				// ✅ Ambil data dari Claims dulu, fallback ke Session
				var kategoriUserId = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
					?? HttpContext.Session.GetString("kategori_user_id");
				var compCode = User.FindFirst("comp_code")?.Value
					?? HttpContext.Session.GetString("company");
				var nrp = User.Identity?.Name ?? HttpContext.Session.GetString("nrp");
				var nama = User.FindFirst("nama")?.Value ?? HttpContext.Session.GetString("nama"); // Ambil nama
				var dept = User.FindFirst("dept_code")?.Value ?? HttpContext.Session.GetString("dept"); // Ambil dept

				if (string.IsNullOrEmpty(kategoriUserId) || string.IsNullOrEmpty(nrp))
				{
					_logger.LogWarning("User tanpa kategori_user_id/nrp mencoba akses Approval Admin.");
					return RedirectToAction("Index", "Login");
				}

				var controllerName = "SuratPengajuan"; // Nama controller spesifik
				var actionName = ControllerContext.ActionDescriptor.ActionName; // Nama action saat ini (Approval)

				// 🔒 RBAC check
				bool punyaAkses = _context.tbl_r_menu.Any(x =>
				   x.kategori_user_id == kategoriUserId &&
				   (x.comp_code == null || x.comp_code == compCode) &&
				   x.link_controller == controllerName &&
				   x.link_function == actionName);

				if (!punyaAkses)
				{
					_logger.LogWarning($"Akses ditolak: {nrp} ({kategoriUserId}) mencoba akses {controllerName}/{actionName}");
					return RedirectToAction("Index", "Login");
				}

				// 📋 Ambil menu untuk sidebar
				var menuList = _context.tbl_r_menu
					.Where(x => x.kategori_user_id == kategoriUserId)
					.Where(x => x.comp_code == null || x.comp_code == compCode)
					.OrderBy(x => x.type)
					.ThenBy(x => x.title)
					.ToList();

				// 📦 Siapkan ViewBag
				ViewBag.Title = "Review & Proses Admin"; // Judul spesifik halaman
				ViewBag.Controller = controllerName;
				ViewBag.Menu = menuList;
				ViewBag.Setting = _context.tbl_m_setting_aplikasi.FirstOrDefault();
				ViewBag.insert_by = nrp;
				ViewBag.nama = nama;
				ViewBag.departemen = dept;
				ViewBag.company = compCode;

				_logger.LogInformation($"User {nrp} ({kategoriUserId}) mengakses {controllerName}/{actionName}");
				return View(); // Mengembalikan View Approval.cshtml
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat load menu Approval Admin");
				return RedirectToAction("Index", "Login");
			}
		}

		// ================================
		// 📌 API & DATA METHODS
		// ================================

		[HttpGet]
		public async Task<IActionResult> SearchKaryawan(string term)
		{
			// ... (Kode SearchKaryawan tetap sama) ...
			if (string.IsNullOrEmpty(term) || term.Length < 3)
			{
				return Json(new { data = Array.Empty<object>() });
			}

			term = term.ToLower();

			try
			{
				var kategoriUserId = HttpContext.Session.GetString("kategori_user_id");
				var departemenSession = HttpContext.Session.GetString("dept");

				var query = _context.vw_m_karyawan_indexim
					.AsNoTracking()
					.Where(k =>
						EF.Functions.Like(k.no_nik.ToLower(), $"%{term}%") ||
						EF.Functions.Like(k.nama_lengkap.ToLower(), $"%{term}%")
					);

				// Filter berdasarkan departemen HANYA jika bukan Administrator
				if (!string.Equals(kategoriUserId, "ADMINISTRATOR", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(departemenSession))
				{
					query = query.Where(k => k.depart == departemenSession);
				}


				var data = await query
					.Take(10) // Batasi hasil untuk performa
					.Select(k => new
					{
						nik = k.no_nik,
						nama_lengkap = k.nama_lengkap,
						departemen = k.depart,
						jabatan = k.posisi,
						tanggal_mulai_bekerja = k.doh, // Mungkin diperlukan view lain
						lokasi_kerja = k.level // Mungkin diperlukan view lain
					})
					.ToListAsync();

				return Json(new { data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat pencarian karyawan dengan term: {term}", term);
				return StatusCode(500, new { message = "Terjadi kesalahan pada server." });
			}
		}

		[HttpPost]
		public async Task<IActionResult> Insert(
			[FromForm] tbl_t_pengajuan a, // Model utama
										  // File-file opsional
			IFormFile file_buku_nikah, IFormFile file_ktp_pernikahan,
			IFormFile file_surat_kematian, IFormFile file_ktp_duka, IFormFile file_kk_duka, IFormFile file_other_duka)
		{
			// ... (Kode Insert tetap sama) ...
			if (a == null) return Json(new { success = false, message = "Data tidak valid." });
			if (string.IsNullOrEmpty(a.jenis_pengajuan) || string.IsNullOrEmpty(a.nik))
				return Json(new { success = false, message = "Jenis pengajuan dan NIK tidak boleh kosong." });

			// Validasi file bisa ditambahkan di sini jika perlu

			try
			{
				// Cek duplikasi (contoh sederhana, bisa disesuaikan)
				// bool isDuplicate = await _context.tbl_t_pengajuan.AnyAsync(p => p.nik == a.nik && p.jenis_pengajuan == a.jenis_pengajuan && p.status != STATUS_DITOLAK_REVIEW && p.status != STATUS_DITOLAK_MANAGER && p.status != STATUS_DITOLAK_ADMIN);
				// if (isDuplicate) {
				//     return Json(new { success = false, message = "Pengajuan serupa sudah ada dan sedang diproses." });
				// }


				// Simpan file ke wwwroot/File/[SubFolder]/[guid]_[namafile]
				var pernikahanFolder = JENIS_PERNIKAHAN.Replace(" ", "_");
				var dukaFolder = JENIS_DUKA.Replace(" ", "_");
				a.file_path_buku_nikah = await SimpanFileAsync(file_buku_nikah, pernikahanFolder); // Gunakan nama jenis sebagai subfolder
				a.file_path_ktp_pernikahan = await SimpanFileAsync(file_ktp_pernikahan, pernikahanFolder);
				a.file_path_surat_kematian = await SimpanFileAsync(file_surat_kematian, dukaFolder);
				a.file_path_ktp_duka = await SimpanFileAsync(file_ktp_duka, dukaFolder);
				a.file_path_kk_duka = await SimpanFileAsync(file_kk_duka, dukaFolder);
				var otherSubFolder = string.Equals(a.jenis_pengajuan, JENIS_PERNIKAHAN, StringComparison.OrdinalIgnoreCase) ? pernikahanFolder : dukaFolder;
				a.file_path_other_duka = await SimpanFileAsync(file_other_duka, otherSubFolder);

				// Isi data default
				a.id = Guid.NewGuid().ToString(); // Generate ID baru
				a.tanggal_pengajuan = DateTime.Today;
				a.created_at = DateTime.Now;
				a.status = STATUS_MENUNGGU_REVIEW; // Status awal
				a.ip = HttpContext.Connection.RemoteIpAddress?.ToString(); // Ambil IP client
				a.insert_by = User.Identity?.Name ?? HttpContext.Session.GetString("nrp"); // Ambil Nrp/Nik penginput

				// Tambahkan nomor surat default untuk SKK jika belum ada
				if (a.jenis_pengajuan == JENIS_SKK && string.IsNullOrEmpty(a.nomor))
				{
					// Format Nomor: [ID 4 digit]/IC-SITE/HCGS/SURKET/[Bulan Romawi]/[Tahun]
					var today = DateTime.Now;
					var monthRoman = ToRoman(today.Month);
					var year = today.Year;
					// Ambil 4 digit terakhir dari ID baru atau seluruh ID jika kurang dari 4
					var idPart = a.id.Length > 4 ? a.id.Substring(a.id.Length - 4).ToUpper() : a.id.ToUpper();
					a.nomor = $"{idPart}/IC-SITE/HCGS/SURKET/{monthRoman}/{year}";
				}


				_context.tbl_t_pengajuan.Add(a);
				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "Data pengajuan berhasil disimpan dan sedang menunggu review Admin." });
			}
			catch (DbUpdateException dbEx)
			{
				_logger.LogError(dbEx, "Database error on Insert action");
				// Periksa inner exception untuk detail
				var innermostEx = dbEx.InnerException ?? dbEx;
				return Json(new { success = false, message = "Gagal menyimpan ke database: " + innermostEx.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error on Insert action");
				// Jangan tampilkan ex.Message langsung ke user di produksi
				return Json(new { success = false, message = "Terjadi kesalahan internal saat menyimpan pengajuan." });
			}
		}

		private async Task<string> SimpanFileAsync(IFormFile file, string subFolder)
		{
			// ... (Kode SimpanFileAsync tetap sama) ...
			if (file == null || file.Length == 0) return null;

			// Pastikan subfolder valid (hindari path traversal)
			subFolder = subFolder.Replace("..", "").Replace("/", "").Replace("\\", "");
			if (string.IsNullOrWhiteSpace(subFolder)) subFolder = "Lainnya"; // Default folder

			try
			{
				// Path relatif: /File/[SubFolder]/[NamaFileUnik]
				string relativeFolderPath = Path.Combine("File", subFolder);
				// Path fisik absolut di server
				string physicalFolderPath = Path.Combine(_env.WebRootPath, relativeFolderPath);

				Directory.CreateDirectory(physicalFolderPath); // Buat folder jika belum ada

				// Buat nama file unik untuk menghindari konflik
				string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
				string physicalFilePath = Path.Combine(physicalFolderPath, uniqueFileName);

				using (var stream = new FileStream(physicalFilePath, FileMode.Create))
				{
					await file.CopyToAsync(stream);
				}

				// Kembalikan path relatif yang bisa diakses dari web
				// Contoh: /File/Santunan_Duka/guid_namafile.pdf
				return $"/{relativeFolderPath.Replace(Path.DirectorySeparatorChar, '/')}/{uniqueFileName}";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Gagal menyimpan file {FileName} ke folder {SubFolder}", file.FileName, subFolder);
				return null; // Kembalikan null jika gagal
			}
		}

		[HttpGet]
		public IActionResult GetAll() // Untuk tabel User Biasa
		{
			try
			{
				var nrp = User.Identity?.Name ?? HttpContext.Session.GetString("nrp");
				if (string.IsNullOrEmpty(nrp))
				{
					_logger.LogWarning("GetAll dipanggil tanpa Nrp user.");
					// PERBAIKAN: Kembalikan struktur yang diharapkan DataTables
					return Json(new { data = new List<object>() });
				}

				// Ambil data HANYA untuk user yang login
				var data = GetBasePengajuanQuery() // Gunakan query dasar yang sudah join
							.Where(p => p.insert_by == nrp) // Filter berdasarkan Nrp/Nik pembuat
							.ToList(); // Materialize query

				// PERBAIKAN: Tambahkan logging jumlah data yang ditemukan
				_logger.LogInformation("GetAll found {Count} records for user {Nrp}", data.Count, nrp);


				return Json(new { data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching GetAll data for user.");
				// PERBAIKAN: Kembalikan struktur yang diharapkan DataTables saat error
				return Json(new { data = new List<object>() }); // Kembalikan data kosong
																// Atau bisa juga kembalikan error status code jika JS bisa menangani:
																// return StatusCode(500, Json(new { message = "Gagal memuat riwayat pengajuan." }));
			}
		}

		[Authorize] // Pastikan hanya user terotentikasi yang bisa delete
		[HttpPost]
		// [ValidateAntiForgeryToken] // Aktifkan jika menggunakan AntiForgeryToken di form/JS
		public async Task<IActionResult> Delete(string id) // Ubah ke async Task
		{
			if (string.IsNullOrEmpty(id))
			{
				return Json(new { success = false, message = "ID tidak valid." });
			}

			try
			{
				var nrp = User.Identity?.Name ?? HttpContext.Session.GetString("nrp");
				var pengajuan = await _context.tbl_t_pengajuan.FirstOrDefaultAsync(p => p.id == id);

				if (pengajuan == null)
				{
					return Json(new { success = false, message = "Data pengajuan tidak ditemukan." });
				}

				// PENTING: User hanya boleh menghapus pengajuannya sendiri DAN jika statusnya masih Menunggu Review
				if (pengajuan.insert_by != nrp)
				{
					_logger.LogWarning("User {Nrp} mencoba menghapus pengajuan milik user lain (ID: {PengajuanId})", nrp, id);
					return Json(new { success = false, message = "Anda tidak diizinkan menghapus pengajuan ini." });
				}
				if (pengajuan.status != STATUS_MENUNGGU_REVIEW)
				{
					return Json(new { success = false, message = $"Pengajuan dengan status '{pengajuan.status}' tidak dapat dihapus." });
				}


				// Hapus file fisik terkait
				HapusFileFisik(pengajuan.file_path_buku_nikah);
				HapusFileFisik(pengajuan.file_path_ktp_pernikahan);
				HapusFileFisik(pengajuan.file_path_surat_kematian);
				HapusFileFisik(pengajuan.file_path_ktp_duka);
				HapusFileFisik(pengajuan.file_path_kk_duka);
				HapusFileFisik(pengajuan.file_path_other_duka);
				// Tambahkan penghapusan file_path jika ada
				HapusFileFisik(pengajuan.file_path);


				_context.tbl_t_pengajuan.Remove(pengajuan);
				await _context.SaveChangesAsync(); // Gunakan async

				return Json(new { success = true, message = "Pengajuan dan file terkait berhasil dihapus." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat menghapus pengajuan ID: {id}", id);
				return Json(new { success = false, message = "Terjadi kesalahan saat menghapus data." });
			}
		}


		private void HapusFileFisik(string relativePath)
		{
			// ... (Kode HapusFileFisik tetap sama) ...
			if (string.IsNullOrEmpty(relativePath)) return;

			try
			{
				// Pastikan path valid dan berada dalam wwwroot
				// Hapus ~/, ganti / dengan separator sistem
				string cleanRelativePath = relativePath.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar);
				string physicalPath = Path.Combine(_env.WebRootPath, cleanRelativePath);

				// Validasi tambahan: Pastikan path tidak keluar dari wwwroot (mencegah path traversal)
				string webRootFullPath = Path.GetFullPath(_env.WebRootPath);
				string fileFullPath = Path.GetFullPath(physicalPath);

				if (!fileFullPath.StartsWith(webRootFullPath))
				{
					_logger.LogWarning("Percobaan menghapus file di luar wwwroot: {path}", relativePath);
					return;
				}


				if (System.IO.File.Exists(physicalPath))
				{
					System.IO.File.Delete(physicalPath);
					_logger.LogInformation("File fisik berhasil dihapus: {path}", physicalPath);
				}
				else
				{
					_logger.LogWarning("File fisik tidak ditemukan saat akan dihapus: {path}", physicalPath);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Gagal menghapus file fisik: {path}", relativePath);
				// Pertimbangkan apakah error ini perlu dilempar atau hanya dicatat
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetForManagerApproval() // Ubah ke async
		{
			try
			{
				var data = await GetBasePengajuanQuery() // Gunakan async
							.Where(p => p.status == STATUS_MENUNGGU_MANAGER)
							.ToListAsync(); // Gunakan async

				// PERBAIKAN: Tambahkan logging jumlah data yang ditemukan
				_logger.LogInformation("GetForManagerApproval found {Count} records.", data.Count);


				return Json(new { data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching data for manager approval.");
				// PERBAIKAN: Kembalikan struktur yang diharapkan DataTables saat error
				return Json(new { data = new List<object>() });
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetForAdmin() // Ubah ke async
		{
			try
			{
				// Status yang relevan untuk Admin (Review, Siap Proses, Selesai, Semua Ditolak)
				var statusesForAdmin = new List<string> {
					STATUS_MENUNGGU_REVIEW,
					STATUS_DITOLAK_REVIEW,
					STATUS_MENUNGGU_MANAGER, // Admin perlu melihat ini juga (read-only)
                    STATUS_DITOLAK_MANAGER,
					STATUS_SIAP_PROSES,
					STATUS_SELESAI,
					STATUS_DITOLAK_ADMIN
				};
				var data = await GetBasePengajuanQuery() // Gunakan async
							.Where(p => statusesForAdmin.Contains(p.status))
							.OrderByDescending(p => p.tanggal_pengajuan ?? p.created_at)
							.ToListAsync(); // Gunakan async

				// PERBAIKAN: Tambahkan logging jumlah data yang ditemukan
				_logger.LogInformation("GetForAdmin found {Count} records.", data.Count);


				return Json(new { data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching data for admin.");
				// PERBAIKAN: Kembalikan struktur yang diharapkan DataTables saat error
				return Json(new { data = new List<object>() });
			}
		}


		// PERBAIKAN: GetBasePengajuanQuery menggunakan LINQ Join
		// Query dasar untuk mengambil data pengajuan dengan join ke data karyawan
		private IQueryable<vw_m_surat_pengajuan> GetBasePengajuanQuery()
		{
			try
			{
				// Lakukan join antara tbl_t_pengajuan (p) dan vw_m_karyawan_indexim (k)
				var query = from p in _context.tbl_t_pengajuan.AsNoTracking()
							join k in _context.vw_m_karyawan_indexim.AsNoTracking()
								on p.nik equals k.no_nik into gj // Left join
							from subk in gj.DefaultIfEmpty() // Handle jika karyawan tidak ditemukan
							select new vw_m_surat_pengajuan // Proyeksikan ke model view
							{
								// Ambil semua kolom dari tbl_t_pengajuan (p)
								id = p.id,
								nik = p.nik,
								jenis_pengajuan = p.jenis_pengajuan,
								nomor = p.nomor, // Kolom baru
								status_karyawan = p.status_karyawan, // Kolom baru
								tanggal_pengajuan = p.tanggal_pengajuan,
								status = p.status,
								keperluan = p.keperluan,
								nama_pasangan = p.nama_pasangan,
								pernah_klaim_pernikahan = p.pernah_klaim_pernikahan,
								nama_almarhum = p.nama_almarhum,
								hubungan_keluarga = p.hubungan_keluarga,
								file_path_buku_nikah = p.file_path_buku_nikah,
								file_path_ktp_pernikahan = p.file_path_ktp_pernikahan,
								file_path_surat_kematian = p.file_path_surat_kematian,
								file_path_ktp_duka = p.file_path_ktp_duka,
								file_path_kk_duka = p.file_path_kk_duka,
								file_path_other_duka = p.file_path_other_duka,
								file_path = p.file_path,
								tgl_menikah = p.tgl_menikah,
								tgl_meninggal = p.tgl_meninggal,
								status_karyawan_saat_duka = p.status_karyawan_saat_duka, // Kolom verifikasi
								status_hris = p.status_hris, // Kolom verifikasi
								pernah_klaim_duka = p.pernah_klaim_duka, // Kolom verifikasi
								tgl_klaim_duka_sebelumnya = p.tgl_klaim_duka_sebelumnya, // Kolom verifikasi
								remarks = p.remarks,
								insert_by = p.insert_by,
								ip = p.ip,
								created_at = p.created_at,
								updated_at = p.updated_at,

								// --- PERBAIKAN: Logika Data Editan ---
								// Ambil data editan jika ada, jika tidak (NULL), ambil data asli dari join
								nama_lengkap = p.nama_lengkap_edit ?? (subk != null ? subk.nama_lengkap : "(Karyawan Tidak Ditemukan)"),
								depart = p.depart_edit ?? (subk != null ? subk.depart : "-"),
								posisi = p.posisi_edit ?? (subk != null ? subk.posisi : "-"),
								doh = p.doh_edit ?? (subk != null ? subk.doh : null),

								// Ambil kolom editan mentah (untuk form admin)
								nama_lengkap_edit = p.nama_lengkap_edit,
								posisi_edit = p.posisi_edit,
								depart_edit = p.depart_edit,
								doh_edit = p.doh_edit,
								// --- AKHIR PERBAIKAN ---
							};

				return query.OrderByDescending(x => x.created_at); // Pindahkan OrderBy ke sini
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating base pengajuan query with LINQ join.");
				// Kembalikan query kosong jika terjadi error
				return Enumerable.Empty<vw_m_surat_pengajuan>().AsQueryable();
			}
		}


		// ================================
		// 📌 PRINTING & PROCESSING
		// ================================

		[Authorize]
		public async Task<IActionResult> PrintView(string id)
		{
			// ... (Kode PrintView tetap sama) ...
			try
			{
				if (string.IsNullOrEmpty(id)) return BadRequest("ID Pengajuan tidak valid.");

				// PERBAIKAN: Gunakan GetBasePengajuanQuery yang baru
				var data = await GetBasePengajuanQuery().FirstOrDefaultAsync(p => p.id == id);


				if (data == null) return NotFound("Data pengajuan tidak ditemukan.");

				// Pastikan status Selesai sebelum bisa print? (Opsional)
				// if(data.status != STATUS_SELESAI) {
				//     return BadRequest("Pengajuan belum selesai diproses.");
				// }

				// PERBAIKAN: Ambil data dari query, BUKAN dari ViewBag
				// (ViewBag belum diisi di sini)

				switch (data.jenis_pengajuan)
				{
					case JENIS_SKK: // Gunakan konstanta
									// Nomor surat SEHARUSNYA sudah ada di data.nomor
						if (string.IsNullOrEmpty(data.nomor))
						{
							_logger.LogWarning("Mencoba print SKK ID {id} tanpa nomor surat.", id);
							// Bisa generate ulang jika diperlukan, tapi sebaiknya sudah ada
							var today = DateTime.Now;
							var monthRoman = ToRoman(today.Month);
							var year = today.Year;
							var idPart = data.id.Length > 4 ? data.id.Substring(data.id.Length - 4).ToUpper() : data.id.ToUpper();
							ViewBag.LetterNumber = $"{idPart}/IC-SITE/HCGS/SURKET/{monthRoman}/{year}"; // Fallback
						}
						else
						{
							ViewBag.LetterNumber = data.nomor;
						}
						ViewBag.CurrentDateFormatted = DateTime.Now.ToString("d MMMM yyyy", new CultureInfo("id-ID"));
						// Kirim juga status karyawan ke view
						ViewBag.StatusKaryawan = data.status_karyawan; // Ambil dari data
						return View("PrintView", data); // Nama view harus cocok

					case JENIS_PERNIKAHAN: // Gunakan konstanta
						return View("PrintPernikahan", data); // Nama view harus cocok

					case JENIS_DUKA: // Gunakan konstanta
						SetDukaQrSignatures(data);
						return View("PrintDuka", data); // Nama view harus cocok

					default:
						_logger.LogWarning("Jenis pengajuan tidak dikenal saat print: {Jenis}", data.jenis_pengajuan);
						return BadRequest($"Jenis pengajuan '{data.jenis_pengajuan}' tidak memiliki template cetak.");
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat generate view print untuk ID: {id}", id);
				// Return halaman error atau pesan sederhana
				return Content("Terjadi kesalahan saat membuat halaman cetak. Silakan coba lagi atau hubungi administrator.");
			}
		}

		[Authorize]
		public async Task<IActionResult> PrintGabunganDuka(string id)
		{
			// ... (Kode PrintGabunganDuka tetap sama) ...
			try
			{
				if (string.IsNullOrEmpty(id)) return BadRequest("ID tidak valid.");

				// PERBAIKAN: Gunakan GetBasePengajuanQuery yang baru
				var data = await GetBasePengajuanQuery()
									.FirstOrDefaultAsync(p => p.id == id && p.jenis_pengajuan == JENIS_DUKA); // Konstanta

				if (data == null) return NotFound("Data pengajuan santunan duka tidak ditemukan.");

				// Pastikan status selesai? (Opsional)
				// if(data.status != STATUS_SELESAI) return BadRequest("Pengajuan belum selesai.");

				// Render view PrintDuka menjadi HTML string
				SetDukaQrSignatures(data);
				string htmlContent = await RenderViewToStringAsync("PrintDuka", data); // Pastikan nama view benar

				// Folder temporary untuk menyimpan PDF sementara
				string tempFolder = Path.Combine(_env.WebRootPath, "File", "Temp");
				Directory.CreateDirectory(tempFolder); // Buat jika belum ada
				string formPdfPath = Path.Combine(tempFolder, $"form_{Guid.NewGuid()}.pdf"); // PDF dari HTML

				// Konversi HTML ke PDF menggunakan iText html2pdf
				var converterProps = new ConverterProperties();
				converterProps.SetBaseUri(_env.WebRootPath); // Penting untuk CSS/gambar di HTML
				using (var fs = new FileStream(formPdfPath, FileMode.Create))
				{
					HtmlConverter.ConvertToPdf(htmlContent, fs, converterProps);
				}

				var attachmentPaths = new List<string> { formPdfPath };
				AddAttachmentIfValid(attachmentPaths, data.file_path_surat_kematian);
				AddAttachmentIfValid(attachmentPaths, data.file_path_ktp_duka);
				AddAttachmentIfValid(attachmentPaths, data.file_path_kk_duka);
				AddAttachmentIfValid(attachmentPaths, data.file_path_other_duka);

				var outputBytes = MergeFilesToPdf(attachmentPaths, tempFolder);

				if (System.IO.File.Exists(formPdfPath))
				{
					try { System.IO.File.Delete(formPdfPath); } catch { /* Abaikan jika gagal hapus */ }
				}

				if (outputBytes.Length == 0)
				{
					_logger.LogError("Gagal membuat PDF gabungan ID {id}, output kosong.", id);
					return Content("Gagal membuat PDF gabungan karena tidak ada file yang berhasil diproses.");
				}

				return File(outputBytes, "application/pdf", $"SantunanDuka_{data.nama_lengkap}_{data.nik}.pdf"); // Nama file lebih deskriptif
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Gagal membuat PDF gabungan untuk ID: {id}", id);
				return Content("Terjadi kesalahan saat membuat dokumen PDF gabungan.");
			}
		}

		[Authorize]
		public async Task<IActionResult> PrintGabunganPernikahan(string id)
		{
			try
			{
				if (string.IsNullOrEmpty(id)) return BadRequest("ID tidak valid.");

				var data = await GetBasePengajuanQuery()
									.FirstOrDefaultAsync(p => p.id == id && p.jenis_pengajuan == JENIS_PERNIKAHAN);

				if (data == null) return NotFound("Data pengajuan pernikahan tidak ditemukan.");

				SetPernikahanQrSignatures(data);
				string htmlContent = await RenderViewToStringAsync("PrintPernikahan", data);

				string tempFolder = Path.Combine(_env.WebRootPath, "File", "Temp");
				Directory.CreateDirectory(tempFolder);
				string formPdfPath = Path.Combine(tempFolder, $"form_{Guid.NewGuid()}.pdf");

				var converterProps = new ConverterProperties();
				converterProps.SetBaseUri(_env.WebRootPath);
				using (var fs = new FileStream(formPdfPath, FileMode.Create))
				{
					HtmlConverter.ConvertToPdf(htmlContent, fs, converterProps);
				}

				var attachmentPaths = new List<string> { formPdfPath };
				AddAttachmentIfValid(attachmentPaths, data.file_path_buku_nikah);
				AddAttachmentIfValid(attachmentPaths, data.file_path_ktp_pernikahan);
				AddAttachmentIfValid(attachmentPaths, data.file_path_other_duka);

				var outputBytes = MergeFilesToPdf(attachmentPaths, tempFolder);

				if (System.IO.File.Exists(formPdfPath))
				{
					try { System.IO.File.Delete(formPdfPath); } catch { }
				}

				if (outputBytes.Length == 0)
				{
					_logger.LogError("Gagal membuat PDF gabungan pernikahan ID {id}, output kosong.", id);
					return Content("Gagal membuat PDF gabungan karena tidak ada file yang berhasil diproses.");
				}

				return File(outputBytes, "application/pdf", $"Pernikahan_{data.nama_lengkap}_{data.nik}.pdf");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Gagal membuat PDF gabungan pernikahan untuk ID: {id}", id);
				return Content("Terjadi kesalahan saat membuat dokumen PDF gabungan.");
			}
		}

		private byte[] MergeFilesToPdf(List<string> attachmentPaths, string tempFolder)
		{
			var outputStream = new MemoryStream();
			using (var writer = new PdfWriter(outputStream))
			using (var mergedPdf = new PdfDocument(writer))
			{
				writer.SetSmartMode(true);
				var merger = new PdfMerger(mergedPdf);

				foreach (var filePath in attachmentPaths)
				{
					string tempPdfPathForImage = null;
					try
					{
						string currentFileToMerge = filePath;
						string ext = Path.GetExtension(filePath).ToLowerInvariant();

						if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp" || ext == ".gif")
						{
							tempPdfPathForImage = Path.Combine(tempFolder, $"img_{Guid.NewGuid()}.pdf");
							using (var imgWriter = new PdfWriter(tempPdfPathForImage))
							using (var imgPdfDoc = new PdfDocument(imgWriter))
							using (var doc = new Document(imgPdfDoc))
							{
								var img = new iText.Layout.Element.Image(
									iText.IO.Image.ImageDataFactory.Create(filePath));
								img.SetAutoScale(true);
								doc.Add(img);
							}
							currentFileToMerge = tempPdfPathForImage;
						}
						else if (ext != ".pdf")
						{
							_logger.LogWarning("Format file tidak didukung untuk digabung: {file}", filePath);
							continue;
						}

						using var pdfReader = new PdfReader(currentFileToMerge);
						pdfReader.SetUnethicalReading(true);
						using var sourcePdf = new PdfDocument(pdfReader);
						merger.Merge(sourcePdf, 1, sourcePdf.GetNumberOfPages());
					}
					catch (Exception mergeEx)
					{
						_logger.LogError(mergeEx, "Gagal menggabungkan file: {file}", filePath);
					}
					finally
					{
						if (tempPdfPathForImage != null && System.IO.File.Exists(tempPdfPathForImage))
						{
							try { System.IO.File.Delete(tempPdfPathForImage); } catch { }
						}
					}
				}
			}

			return outputStream.ToArray();
		}

		private void AddAttachmentIfValid(List<string> attachmentPaths, string relativePath)
		{
			if (string.IsNullOrWhiteSpace(relativePath)) return;

			try
			{
				string cleanRelativePath = relativePath.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar);
				string physicalPath = Path.Combine(_env.WebRootPath, cleanRelativePath);

				string webRootFullPath = Path.GetFullPath(_env.WebRootPath);
				string fileFullPath = Path.GetFullPath(physicalPath);
				if (!fileFullPath.StartsWith(webRootFullPath, StringComparison.OrdinalIgnoreCase))
				{
					_logger.LogWarning("Attachment path di luar wwwroot di-skip: {path}", relativePath);
					return;
				}

				if (System.IO.File.Exists(physicalPath))
				{
					attachmentPaths.Add(physicalPath);
				}
				else
				{
					_logger.LogWarning("Attachment file not found: {path}", physicalPath);
				}
			}
			catch (Exception pathEx)
			{
				_logger.LogError(pathEx, "Error processing attachment path: {path}", relativePath);
			}
		}

		private void SetDukaQrSignatures(vw_m_surat_pengajuan data)
		{
			// Buat teks QR menggunakan data pengajuan supaya bisa dibaca ulang ketika dicetak/di-PDF-kan
			var createdAt = data.created_at?.ToString("dd MMM yyyy HH:mm", new CultureInfo("id-ID")) ?? "-";

			var qrPemohonText = $"Review Admin HC - Santunan Duka - ID {data.id} - dibuat {createdAt}";
			var qrAtasanText = $"Persetujuan Manager HCGS - Santunan Duka - ID {data.id} - dibuat {createdAt}";
			var qrReviewText = $"Hasil Review HCGS Manager - Santunan Duka - ID {data.id} - dibuat {createdAt}";

			ViewData["QrPemohonUri"] = GenerateQrDataUri(qrPemohonText, 120);
			ViewData["QrAtasanUri"] = GenerateQrDataUri(qrAtasanText, 120);
			ViewData["QrReviewManagerUri"] = GenerateQrDataUri(qrReviewText, 100);
		}

		private string GenerateQrDataUri(string text, int pixels = 120)
		{
			var payload = string.IsNullOrWhiteSpace(text) ? "-" : text;
			using var qrGenerator = new QRCodeGenerator();
			using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
			var qrCode = new PngByteQRCode(qrData);
			var qrBytes = qrCode.GetGraphic(pixels);
			return $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}";
		}

		private void SetPernikahanQrSignatures(vw_m_surat_pengajuan data)
		{
			var createdAt = data.created_at?.ToString("dd MMM yyyy HH:mm", new CultureInfo("id-ID")) ?? "-";
			var qrPemohonText = $"Review Admin HC - Hadiah Pernikahan - ID {data.id} - dibuat {createdAt}";
			var qrAtasanText = $"Persetujuan Manager HCGS - Hadiah Pernikahan - ID {data.id} - dibuat {createdAt}";

			ViewData["PernikahanQrPemohonUri"] = GenerateQrDataUri(qrPemohonText, 120);
			ViewData["PernikahanQrAtasanUri"] = GenerateQrDataUri(qrAtasanText, 120);
		}


		private string ToRoman(int number)
		{
			// ... (Kode ToRoman tetap sama) ...
			if (number < 1 || number > 3999) return number.ToString(); // Handle invalid range or return number if preferred
			if (number >= 1000) return "M" + ToRoman(number - 1000);
			if (number >= 900) return "CM" + ToRoman(number - 900);
			if (number >= 500) return "D" + ToRoman(number - 500);
			if (number >= 400) return "CD" + ToRoman(number - 400);
			if (number >= 100) return "C" + ToRoman(number - 100);
			if (number >= 90) return "XC" + ToRoman(number - 90);
			if (number >= 50) return "L" + ToRoman(number - 50);
			if (number >= 40) return "XL" + ToRoman(number - 40);
			if (number >= 10) return "X" + ToRoman(number - 10);
			if (number >= 9) return "IX" + ToRoman(number - 9);
			if (number >= 5) return "V" + ToRoman(number - 5);
			if (number >= 4) return "IV" + ToRoman(number - 4);
			if (number >= 1) return "I" + ToRoman(number - 1);
			return string.Empty; // Should not happen if input > 0
		}

		private async Task<string> RenderViewToStringAsync(string viewName, object model)
		{
			// ... (Kode RenderViewToStringAsync tetap sama) ...
			// Pastikan ControllerContext.HttpContext tersedia atau gunakan IHttpContextAccessor
			var httpContext = ControllerContext.HttpContext ?? new DefaultHttpContext { RequestServices = HttpContext.RequestServices }; // Gunakan HttpContext dari Controller jika ada
			var actionContext = new ActionContext(httpContext, ControllerContext.RouteData, ControllerContext.ActionDescriptor);

			// Coba cari view menggunakan engine yang ada
			ViewEngineResult viewResult = _viewEngine.FindView(actionContext, viewName, false); // isMainPage = false

			// Coba get view jika find gagal (terkadang diperlukan)
			if (viewResult.View == null)
			{
				viewResult = _viewEngine.GetView(executingFilePath: null, viewPath: $"~/Views/SuratPengajuan/{viewName}.cshtml", isMainPage: false);
			}


			if (viewResult.View == null)
			{
				_logger.LogError($"View '{viewName}' tidak ditemukan. Path yang dicari: ~/Views/SuratPengajuan/{viewName}.cshtml");
				throw new ArgumentNullException(nameof(viewName), $"View '{viewName}' tidak ditemukan.");
			}

			// Siapkan ViewDataDictionary dengan model
			var viewData = new ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(), new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
			{
				Model = model // Set model ke view
			};
			// Tambahkan item ViewBag yang mungkin diperlukan oleh view cetak
			// Contoh: ViewBag.LetterNumber, ViewBag.CurrentDateFormatted, ViewBag.StatusKaryawan
			// Salin dari ViewData controller jika perlu
			foreach (var item in ViewData)
			{
				if (!viewData.ContainsKey(item.Key))
				{
					viewData.Add(item.Key, item.Value);
				}
			}


			using (var sw = new StringWriter())
			{
				var viewContext = new ViewContext(
					actionContext,
					viewResult.View,
					viewData, // Kirim viewData dengan model
					new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
					sw,
					new HtmlHelperOptions()
				);

				try
				{
					await viewResult.View.RenderAsync(viewContext);
					return sw.ToString();
				}
				catch (Exception renderEx)
				{
					_logger.LogError(renderEx, "Error saat rendering view '{viewName}' ke string.", viewName);
					throw; // Lempar ulang error setelah dicatat
				}
			}
		}


		// Endpoint untuk dipanggil oleh Manager
		[HttpPost]
		// [ValidateAntiForgeryToken]
		public async Task<IActionResult> ProsesApprovalManager([FromBody] JsonElement data)
		{
			try
			{
				var id = data.GetProperty("id").GetString();
				var newStatusChoice = data.GetProperty("newStatus").GetString(); // "Disetujui" atau "Ditolak"
				var remarks = data.TryGetProperty("remarks", out var remarksProp) ? remarksProp.GetString() : null; // Ambil remarks jika ada

				if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(newStatusChoice))
				{
					return Json(new { success = false, message = "ID atau status tidak valid." });
				}

				var pengajuan = await _context.tbl_t_pengajuan.FindAsync(id);
				if (pengajuan == null) return Json(new { success = false, message = "Data pengajuan tidak ditemukan." });

				// Pastikan status saat ini adalah Menunggu Manager
				if (pengajuan.status != STATUS_MENUNGGU_MANAGER)
				{
					return Json(new { success = false, message = $"Tindakan tidak valid untuk status saat ini ({pengajuan.status})." });
				}


				// PERUBAHAN ALUR SKK & DUKA: Jika disetujui, langsung Selesai
				if (newStatusChoice == "Disetujui")
				{
					pengajuan.status = (pengajuan.jenis_pengajuan == JENIS_SKK || pengajuan.jenis_pengajuan == JENIS_DUKA)
										? STATUS_SELESAI
										: STATUS_SIAP_PROSES; // Hanya Pernikahan yang jadi Siap Proses
				}
				else // Ditolak
				{
					if (string.IsNullOrWhiteSpace(remarks))
					{
						return Json(new { success = false, message = "Alasan penolakan oleh Manager wajib diisi." });
					}
					pengajuan.status = STATUS_DITOLAK_MANAGER;
				}

				pengajuan.remarks = remarks; // Simpan remarks (alasan tolak atau catatan setuju)
				pengajuan.updated_at = DateTime.Now;
				// Tambahkan siapa yang approve/reject jika perlu (misal, simpan NRP manager)
				// pengajuan.approved_by_manager = User.Identity?.Name ?? HttpContext.Session.GetString("nrp");


				_context.tbl_t_pengajuan.Update(pengajuan);
				await _context.SaveChangesAsync();

				return Json(new { success = true, message = $"Pengajuan berhasil {(newStatusChoice == "Disetujui" ? "disetujui" : "ditolak")} oleh Manager." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat proses approval manager");
				return Json(new { success = false, message = "Terjadi kesalahan saat memproses persetujuan Manager." });
			}
		}

		// Endpoint untuk dipanggil oleh Admin saat Review
		[HttpPost]
		// [ValidateAntiForgeryToken]
		public async Task<IActionResult> ProsesReviewAdmin([FromBody] JsonElement data)
		{
			try
			{
				var id = data.GetProperty("id").GetString();
				var action = data.GetProperty("action").GetString(); // "Lanjutkan" atau "Tolak"
				var remarks = data.TryGetProperty("remarks", out var remarksProp) ? remarksProp.GetString() : null;
				var jenisPengajuan = data.TryGetProperty("jenis_pengajuan", out var jenisProp) ? jenisProp.GetString() : null; // Ambil jenis

				if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(action) || string.IsNullOrEmpty(jenisPengajuan))
				{
					return Json(new { success = false, message = "Data tidak lengkap (ID, action, jenis pengajuan)." });
				}

				var pengajuan = await _context.tbl_t_pengajuan.FindAsync(id);
				if (pengajuan == null) return Json(new { success = false, message = "Data pengajuan tidak ditemukan." });

				// Pastikan status saat ini adalah Menunggu Review
				if (pengajuan.status != STATUS_MENUNGGU_REVIEW)
				{
					return Json(new { success = false, message = $"Tindakan tidak valid untuk status saat ini ({pengajuan.status})." });
				}


				if (action == "Lanjutkan")
				{
					pengajuan.status = STATUS_MENUNGGU_MANAGER;

					// --- PERBAIKAN: Ambil data editan karyawan ---
					pengajuan.nama_lengkap_edit = data.TryGetProperty("nama_lengkap_edit", out var nleProp) ? nleProp.GetString() : null;
					pengajuan.posisi_edit = data.TryGetProperty("posisi_edit", out var peProp) ? peProp.GetString() : null;
					pengajuan.depart_edit = data.TryGetProperty("depart_edit", out var deProp) ? deProp.GetString() : null;
					string dohEditString = data.TryGetProperty("doh_edit", out var dohProp) && dohProp.ValueKind == JsonValueKind.String ? dohProp.GetString() : null;

					if (string.IsNullOrEmpty(pengajuan.nama_lengkap_edit) || string.IsNullOrEmpty(pengajuan.posisi_edit) || string.IsNullOrEmpty(pengajuan.depart_edit) || string.IsNullOrEmpty(dohEditString))
					{
						return Json(new { success = false, message = "Data Karyawan (Nama, Posisi, Departemen, Tgl Masuk) wajib diisi lengkap saat melanjutkan." });
					}
					if (DateTime.TryParse(dohEditString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDoh))
						pengajuan.doh_edit = parsedDoh;
					else
						pengajuan.doh_edit = null; // atau kembalikan error
												   // --- AKHIR PERBAIKAN ---


					// Jika SKK, simpan Nomor Surat dan Status Karyawan
					if (jenisPengajuan == JENIS_SKK)
					{
						var nomor = data.TryGetProperty("nomor", out var noProp) ? noProp.GetString() : null;
						var statusKaryawan = data.TryGetProperty("status_karyawan", out var skProp) ? skProp.GetString() : null;

						// Validasi input SKK
						if (string.IsNullOrWhiteSpace(nomor) || string.IsNullOrWhiteSpace(statusKaryawan))
						{
							return Json(new { success = false, message = "Nomor Surat dan Status Karyawan wajib diisi saat melanjutkan SKK." });
						}
						pengajuan.nomor = nomor;
						pengajuan.status_karyawan = statusKaryawan;
					}
					// PERUBAHAN: Jika DUKA, simpan data verifikasi
					else if (jenisPengajuan == JENIS_DUKA)
					{
						// PERBAIKAN: Baca dari payload JSON
						var statusHris = data.TryGetProperty("status_hris", out var shProp) ? shProp.GetString() : null;
						var statusKarSaatDuka = data.TryGetProperty("status_karyawan_saat_duka", out var skdProp) ? skdProp.GetString() : null;
						var pernahKlaim = data.TryGetProperty("pernah_klaim_duka", out var pkProp) ? pkProp.GetString() : null;
						string tglKlaimSebelumnyaString = data.TryGetProperty("tgl_klaim_duka_sebelumnya", out var tglProp) && tglProp.ValueKind == JsonValueKind.String ? tglProp.GetString() : null;

						// Validasi verifikasi duka
						if (string.IsNullOrEmpty(statusHris) || string.IsNullOrEmpty(statusKarSaatDuka) || string.IsNullOrEmpty(pernahKlaim))
						{
							return Json(new { success = false, message = "Verifikasi Santunan Duka (Status HRIS, Nama DB, Pernah Klaim) wajib diisi saat melanjutkan." });
						}

						pengajuan.status_hris = statusHris;
						pengajuan.status_karyawan_saat_duka = statusKarSaatDuka;
						pengajuan.pernah_klaim_duka = pernahKlaim;

						// Parse tanggal klaim sebelumnya jika ada dan valid
						if (!string.IsNullOrEmpty(tglKlaimSebelumnyaString))
						{
							// PERBAIKAN: Gunakan TryParse dan format yang eksplisit jika perlu, misal "yyyy-MM-dd"
							if (DateTime.TryParse(tglKlaimSebelumnyaString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
								pengajuan.tgl_klaim_duka_sebelumnya = parsedDate;
							else
							{
								_logger.LogWarning("Format tanggal klaim duka sebelumnya tidak valid saat review: {Tgl}", tglKlaimSebelumnyaString);
								// Pertimbangkan apakah ingin mengembalikan error atau mengabaikan tanggal
								// return Json(new { success = false, message = "Format Tanggal Klaim Sebelumnya tidak valid (contoh: YYYY-MM-DD)." });
								pengajuan.tgl_klaim_duka_sebelumnya = null; // Abaikan jika format salah
							}
						}
						else
						{
							// Validasi: Jika 'Pernah', tanggal harus diisi
							if (pernahKlaim == "Pernah")
							{
								return Json(new { success = false, message = "Tanggal Klaim Sebelumnya wajib diisi jika Pernah Klaim dipilih." });
							}
							pengajuan.tgl_klaim_duka_sebelumnya = null; // Set null jika tidak pernah atau tidak diisi
						}

						// PERBAIKAN: Logging nilai sebelum save
						_logger.LogInformation("Saving Duka Verification Data for ID {Id}: HRIS={Hris}, StatusKar={Skd}, PernahKlaim={Pk}, TglKlaim={Tgl}",
												id, statusHris, statusKarSaatDuka, pernahKlaim, pengajuan.tgl_klaim_duka_sebelumnya?.ToString("yyyy-MM-dd"));

					}
				}
				else // action == "Tolak"
				{
					if (string.IsNullOrWhiteSpace(remarks))
					{
						return Json(new { success = false, message = "Alasan penolakan review wajib diisi." });
					}
					pengajuan.status = STATUS_DITOLAK_REVIEW;

					// Hapus data editan jika ditolak
					pengajuan.nama_lengkap_edit = null;
					pengajuan.posisi_edit = null;
					pengajuan.depart_edit = null;
					pengajuan.doh_edit = null;

					// PERBAIKAN: Hapus data verifikasi duka jika ditolak saat review (opsional)
					if (jenisPengajuan == JENIS_DUKA)
					{
						pengajuan.status_hris = null;
						pengajuan.status_karyawan_saat_duka = null;
						pengajuan.pernah_klaim_duka = null;
						pengajuan.tgl_klaim_duka_sebelumnya = null;
					}
				}

				pengajuan.remarks = remarks; // Simpan remarks (alasan tolak atau catatan)
				pengajuan.updated_at = DateTime.Now;
				// Tambahkan siapa yang review jika perlu
				// pengajuan.reviewed_by = User.Identity?.Name ?? HttpContext.Session.GetString("nrp");

				// PERBAIKAN: Logging SEBELUM SaveChanges
				_logger.LogInformation("Attempting to save changes for Pengajuan ID: {Id} with Status: {Status}", pengajuan.id, pengajuan.status);


				_context.tbl_t_pengajuan.Update(pengajuan);
				await _context.SaveChangesAsync();

				// PERBAIKAN: Logging SETELAH SaveChanges (jika berhasil)
				_logger.LogInformation("Successfully saved changes for Pengajuan ID: {Id}", pengajuan.id);


				return Json(new { success = true, message = $"Review berhasil disimpan. Status: {pengajuan.status}." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat proses review admin");
				return Json(new { success = false, message = "Terjadi kesalahan saat memproses review Admin." });
			}
		}

		// Endpoint untuk dipanggil oleh Admin saat Proses Final (HANYA NON-SKK & NON-DUKA -> Hanya Pernikahan)
		[HttpPost]
		// [ValidateAntiForgeryToken]
		public async Task<IActionResult> ProsesApprovalAdmin([FromBody] JsonElement data)
		{
			try
			{
				var id = data.GetProperty("id").GetString();
				var newStatus = data.GetProperty("newStatus").GetString(); // "Selesai" atau "Ditolak"
				var remarks = data.TryGetProperty("remarks", out var remarksProp) ? remarksProp.GetString() : null;
				var jenisPengajuan = data.TryGetProperty("jenis_pengajuan", out var jenisProp) ? jenisProp.GetString() : null;

				if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(newStatus) || string.IsNullOrEmpty(jenisPengajuan))
				{
					return Json(new { success = false, message = "Data tidak lengkap (ID, status baru, jenis pengajuan)." });
				}

				// PERUBAHAN: Endpoint ini tidak boleh untuk SKK atau DUKA
				if (jenisPengajuan == JENIS_SKK || jenisPengajuan == JENIS_DUKA)
				{
					_logger.LogWarning("Percobaan memproses {Jenis} melalui endpoint ProsesApprovalAdmin (ID: {id}). Alur tidak sesuai.", jenisPengajuan, id);
					return Json(new { success = false, message = $"Tindakan tidak valid untuk {jenisPengajuan} pada tahap ini." });
				}

				var pengajuan = await _context.tbl_t_pengajuan.FindAsync(id);
				if (pengajuan == null) return Json(new { success = false, message = "Data pengajuan tidak ditemukan." });

				// Pastikan status saat ini adalah Siap Proses (untuk Pernikahan)
				if (pengajuan.status != STATUS_SIAP_PROSES)
				{
					return Json(new { success = false, message = $"Tindakan tidak valid untuk status saat ini ({pengajuan.status}). Harusnya 'Siap Proses'." });
				}

				// --- PERBAIKAN: Ambil data editan karyawan ---
				pengajuan.nama_lengkap_edit = data.TryGetProperty("nama_lengkap_edit", out var nleProp) ? nleProp.GetString() : null;
				pengajuan.posisi_edit = data.TryGetProperty("posisi_edit", out var peProp) ? peProp.GetString() : null;
				pengajuan.depart_edit = data.TryGetProperty("depart_edit", out var deProp) ? deProp.GetString() : null;
				string dohEditString = data.TryGetProperty("doh_edit", out var dohProp) && dohProp.ValueKind == JsonValueKind.String ? dohProp.GetString() : null;

				if (string.IsNullOrEmpty(pengajuan.nama_lengkap_edit) || string.IsNullOrEmpty(pengajuan.posisi_edit) || string.IsNullOrEmpty(pengajuan.depart_edit) || string.IsNullOrEmpty(dohEditString))
				{
					return Json(new { success = false, message = "Data Karyawan (Nama, Posisi, Departemen, Tgl Masuk) wajib diisi lengkap." });
				}
				if (DateTime.TryParse(dohEditString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDoh))
					pengajuan.doh_edit = parsedDoh;
				else
					pengajuan.doh_edit = null; // atau kembalikan error
											   // --- AKHIR PERBAIKAN ---


				pengajuan.status = (newStatus == "Selesai") ? STATUS_SELESAI : STATUS_DITOLAK_ADMIN;
				pengajuan.remarks = remarks; // Simpan remarks (alasan tolak final atau catatan selesai)
				pengajuan.updated_at = DateTime.Now;
				// Tambahkan siapa yang memproses final jika perlu
				// pengajuan.processed_by = User.Identity?.Name ?? HttpContext.Session.GetString("nrp");


				// Tidak ada verifikasi tambahan untuk Pernikahan

				_context.tbl_t_pengajuan.Update(pengajuan);
				await _context.SaveChangesAsync();

				return Json(new { success = true, message = $"Pengajuan ({jenisPengajuan}) berhasil {(newStatus == "Selesai" ? "diselesaikan" : "ditolak")} oleh Admin." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat proses approval admin (final)");
				return Json(new { success = false, message = "Terjadi kesalahan saat memproses tindakan final Admin." });
			}
		}

		// ---- PERBAIKAN: Endpoint BARU untuk Update Data Selesai ----
		[HttpPost]
		public async Task<IActionResult> UpdateDataSelesai([FromBody] JsonElement data)
		{
			try
			{
				var id = data.GetProperty("id").GetString();
				if (string.IsNullOrEmpty(id))
				{
					return Json(new { success = false, message = "ID tidak valid." });
				}

				var pengajuan = await _context.tbl_t_pengajuan.FindAsync(id);
				if (pengajuan == null)
				{
					return Json(new { success = false, message = "Data pengajuan tidak ditemukan." });
				}

				// Hanya bisa edit dokumen yang sudah Selesai
				if (pengajuan.status != STATUS_SELESAI)
				{
					return Json(new { success = false, message = $"Hanya data 'Selesai' yang dapat diedit langsung. Status saat ini: {pengajuan.status}." });
				}

				// --- Ambil data editan karyawan ---
				pengajuan.nama_lengkap_edit = data.TryGetProperty("nama_lengkap_edit", out var nleProp) ? nleProp.GetString() : null;
				pengajuan.posisi_edit = data.TryGetProperty("posisi_edit", out var peProp) ? peProp.GetString() : null;
				pengajuan.depart_edit = data.TryGetProperty("depart_edit", out var deProp) ? deProp.GetString() : null;
				string dohEditString = data.TryGetProperty("doh_edit", out var dohProp) && dohProp.ValueKind == JsonValueKind.String ? dohProp.GetString() : null;

				if (string.IsNullOrEmpty(pengajuan.nama_lengkap_edit) || string.IsNullOrEmpty(pengajuan.posisi_edit) || string.IsNullOrEmpty(pengajuan.depart_edit) || string.IsNullOrEmpty(dohEditString))
				{
					return Json(new { success = false, message = "Data Karyawan (Nama, Posisi, Departemen, Tgl Masuk) wajib diisi lengkap." });
				}
				if (DateTime.TryParse(dohEditString, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDoh))
					pengajuan.doh_edit = parsedDoh;
				else
					pengajuan.doh_edit = null; // atau kembalikan error
											   // --- AKHIR ---

				// Tambahkan catatan revisi (opsional, tapi bagus untuk tracking)
				var nrpAdmin = User.Identity?.Name ?? HttpContext.Session.GetString("nrp") ?? "Admin";
				pengajuan.remarks = $"[Data diedit oleh {nrpAdmin} pada {DateTime.Now:dd/MM/yyyy HH:mm}] \n" + pengajuan.remarks;

				pengajuan.updated_at = DateTime.Now;
				// Status TETAP 'Selesai'

				_context.tbl_t_pengajuan.Update(pengajuan);
				await _context.SaveChangesAsync();

				_logger.LogInformation("Data Selesai untuk ID: {Id} telah diperbarui oleh {Admin}", id, nrpAdmin);

				return Json(new { success = true, message = "Data karyawan pada dokumen Selesai berhasil diperbarui." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat update data selesai");
				return Json(new { success = false, message = "Terjadi kesalahan saat memperbarui data." });
			}
		}
		// ---- AKHIR PERBAIKAN ----

	}
}
