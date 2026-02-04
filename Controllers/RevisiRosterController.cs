using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
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
	public class RevisiRosterController : Controller
	{
		private readonly AppDBContext _context;
		private readonly ILogger<RevisiRosterController> _logger;
		private readonly IWebHostEnvironment _webHostEnvironment; // <-- PERBAIKAN: Tambahkan ini
		private string controller_name = "RevisiRoster";
		private string title_name = "Pengajuan Revisi Roster";

		// --- PERBAIKAN: Inject IWebHostEnvironment ---
		public RevisiRosterController(AppDBContext context,
									  ILogger<RevisiRosterController> logger,
									  IWebHostEnvironment webHostEnvironment) // <-- Tambahkan ini
		{
			_context = context;
			_logger = logger;
			_webHostEnvironment = webHostEnvironment; // <-- Tambahkan ini
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

		public IActionResult Approval()
		{
			try
			{
				// --- PERBAIKAN: Samakan logika otentikasi dengan Index() ---
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
					_logger.LogWarning("User tanpa kategori_user_id mencoba akses RevisiRoster/Approval.");
					return RedirectToAction("Index", "Login");
				}
				// --- Akhir Perbaikan Otentikasi ---

				var cekAkses = _context.tbl_r_menu
					.Count(x => x.kategori_user_id == kategoriUserId &&
								(x.comp_code == null || x.comp_code == compCode) &&
								x.link_controller == controller_name);

				if (cekAkses == 0)
				{
					_logger.LogWarning($"Akses ditolak: {nrp} ({kategoriUserId}) mencoba akses {controller_name}/Approval");
					return RedirectToAction("Index", "Login");
				}

				var menuList = _context.tbl_r_menu
					.Where(x => x.kategori_user_id == kategoriUserId)
					.Where(x => x.comp_code == null || x.comp_code == compCode)
					.OrderBy(x => x.type)
					.ThenBy(x => x.title)
					.ToList();


				var masterCount = menuList.Count(x => x.type == "Master");
				var MaOwnerCount = menuList.Count(x => x.type == "MaOwner");
				var HROwnerCount = menuList.Count(x => x.type == "HROwner"); // ✅ hitung HROwner
				var AdminOwnerCount = menuList.Count(x => x.type == "AdminOwner");
				ViewBag.Title = title_name;
				ViewBag.Controller = controller_name;
				ViewBag.Setting = _context.tbl_m_setting_aplikasi.FirstOrDefault();
				ViewBag.Menu = menuList;
				ViewBag.MenuMasterCount = masterCount;
				ViewBag.HROwnerCount = HROwnerCount;
				ViewBag.AdminOwnerCount = AdminOwnerCount;
				ViewBag.MaOwnerCount = MaOwnerCount;
				ViewBag.insert_by = nrp; // <-- PERBAIKAN: Gunakan var nrp
				ViewBag.departemen = dept; // <-- PERBAIKAN: Gunakan var dept
				ViewBag.company = compCode;

				return View();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat memuat halaman Revisi Roster.");
				return RedirectToAction("Index", "Login");
			}
		}
		// --- API UNTUK UI INTERAKTIF ---

		[HttpGet]
		public async Task<IActionResult> SearchKaryawan(string term)
		{
			var kategoriUserId = HttpContext.Session.GetString("kategori_user_id");
			var departemenUser = HttpContext.Session.GetString("dept");

			if (string.IsNullOrEmpty(term) || term.Length < 3)
			{
				return Json(new List<object>());
			}

			IQueryable<vw_m_karyawan_indexim> query = _context.vw_m_karyawan_indexim
				.Where(k => k.no_nik.Contains(term) || k.nama_lengkap.ToLower().Contains(term.ToLower()));

			// 🚫 Batasi pencarian kalau bukan ADMINISTRATOR
			if (kategoriUserId != "ADMINISTRATOR")
			{
				query = query.Where(k => k.depart == departemenUser);
			}

			var karyawan = await query
				.Take(10)
				.Select(k => new {
					label = $"{k.no_nik} - {k.nama_lengkap}",
					value = k.no_nik
				})
				.ToListAsync();

			return Json(karyawan);
		}


		[HttpGet]
		public async Task<IActionResult> GetKaryawanDetail(string nik)
		{

			if (string.IsNullOrEmpty(nik)) return BadRequest();

			var detail = await _context.vw_m_karyawan_indexim
				.FirstOrDefaultAsync(k => k.no_nik == nik);

			if (detail == null) return NotFound();

			return Json(detail);
		}

		[HttpGet]
		public async Task<IActionResult> GetCurrentRoster(string nik, DateTime tanggal)
		{
			if (string.IsNullOrEmpty(nik)) return BadRequest();

			var roster = await _context.tbl_m_roster_detail
				.FirstOrDefaultAsync(r => r.nik == nik && r.working_date.HasValue && r.working_date.Value.Date == tanggal.Date);

			if (roster == null)
			{
				return Json(new { status = "Tidak Ditemukan" });
			}

			return Json(new { status = roster.status });
		}

		[HttpGet]
		public async Task<IActionResult> GetRosterRange(string nik, DateTime startDate, DateTime endDate)
		{
			if (string.IsNullOrEmpty(nik) || startDate == default || endDate == default)
			{
				return Json(new { success = false, message = "NIK atau rentang tanggal tidak valid." });
			}

			if (endDate < startDate)
			{
				return Json(new { success = false, message = "Tanggal akhir tidak boleh lebih awal dari tanggal mulai." });
			}

			var days = (endDate.Date - startDate.Date).TotalDays + 1;
			if (days > 31)
			{
				return Json(new { success = false, message = "Rentang tanggal maksimal 31 hari." });
			}

			var rosterDetails = await _context.tbl_m_roster_detail
				.Where(r => r.nik == nik && r.working_date >= startDate.Date && r.working_date <= endDate.Date)
				.ToDictionaryAsync(r => r.working_date.Value.Date, r => r.status);

			var result = new List<object>();
			for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
			{
				rosterDetails.TryGetValue(date, out var status);
				result.Add(new
				{
					tanggal = date.ToString("yyyy-MM-dd"),
					status = status
				});
			}

			return Json(new { success = true, data = result });
		}

		[HttpGet]
		public async Task<IActionResult> GetRosterKeterangan()
		{
			var data = await _context.tbl_m_roster_keterangan
				.OrderBy(k => k.kode)
				.Select(k => new { k.kode, k.keterangan, k.warna })
				.ToListAsync();
			return Json(data);
		}


		[HttpPost]
		public async Task<IActionResult> Create([FromForm] tbl_r_revisi_roster model)
		{
			IFormFile? dokumen_bukti = null;
			if (Request.Form.Files.Any(f => f.Name == "dokumen_bukti"))
			{
				dokumen_bukti = Request.Form.Files.First(f => f.Name == "dokumen_bukti");
			}

			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
				var errorMessage = string.Join("; ", errors);
				_logger.LogWarning("ModelState tidak valid: {ErrorMessage}", errorMessage);
				return Json(new { success = false, message = $"Data tidak valid: {errorMessage}" });
			}

			if (!model.tanggal_awal.HasValue && model.tanggal_roster.HasValue)
			{
				// fallback jika form lama masih mengirim tanggal_roster
				model.tanggal_awal = model.tanggal_roster;
			}

			if (!model.tanggal_awal.HasValue)
			{
				return Json(new { success = false, message = "Tanggal mulai jadwal kerja wajib diisi." });
			}

			var tanggalMulai = model.tanggal_awal.Value.Date;
			var tanggalAkhir = model.tanggal_akhir?.Date ?? tanggalMulai;

			if (tanggalAkhir < tanggalMulai)
			{
				return Json(new { success = false, message = "Tanggal akhir tidak boleh lebih awal dari tanggal mulai." });
			}

			var totalHari = (tanggalAkhir - tanggalMulai).TotalDays + 1;
			if (totalHari > 31) // batasi 1 bulan agar tidak berat
			{
				return Json(new { success = false, message = "Rentang tanggal maksimal 31 hari." });
			}

			model.tanggal_awal = tanggalMulai;
			model.tanggal_akhir = tanggalAkhir;
			// Simpan juga di kolom lama untuk kompatibilitas
			model.tanggal_roster = tanggalMulai;

			if (dokumen_bukti != null && dokumen_bukti.Length > 0)
			{
				if (Path.GetExtension(dokumen_bukti.FileName).ToLower() != ".pdf")
				{
					return Json(new { success = false, message = "Format file tidak valid. Harap unggah dokumen PDF." });
				}

				try
				{
					string uniqueFileName = $"{model.nik}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(dokumen_bukti.FileName)}";

					// --- PERBAIKAN: Gunakan IWebHostEnvironment ---
					string wwwRootPath = _webHostEnvironment.WebRootPath;
					string directoryPath = Path.Combine(wwwRootPath, "File", "Roster"); // <-- Path dinamis
					string filePath = Path.Combine(directoryPath, uniqueFileName);

					if (!Directory.Exists(directoryPath))
					{
						Directory.CreateDirectory(directoryPath);
					}

					using (var stream = new FileStream(filePath, FileMode.Create))
					{
						await dokumen_bukti.CopyToAsync(stream);
					}

					model.file_path = $"/File/Roster/{uniqueFileName}"; // <-- Path relatif untuk URL
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error saat menyimpan file upload.");
					return Json(new { success = false, message = "Terjadi kesalahan saat menyimpan file bukti." });
				}
			}
			else
			{
				model.file_path = null;
			}


			try
			{
				var nrp = HttpContext.Session.GetString("nrp");
				if (string.IsNullOrEmpty(nrp))
				{
					return Json(new { success = false, message = "Sesi Anda telah berakhir. Silakan login kembali." });
				}

				var segmentsPayload = Request.Form["segments_json"].FirstOrDefault();
				if (string.IsNullOrWhiteSpace(segmentsPayload))
				{
					return Json(new { success = false, message = "Detail perubahan jadwal belum lengkap." });
				}

				var parsedSegments = ParseSegments(segmentsPayload);
				if (!parsedSegments.Any())
				{
					return Json(new { success = false, message = "Detail perubahan jadwal tidak valid." });
				}

				var orderedSegments = parsedSegments
					.OrderBy(s => s.tanggal_awal ?? s.tanggal_akhir ?? DateTime.MinValue)
					.ToList();

				var coveredDates = new HashSet<DateTime>();
				foreach (var segment in orderedSegments)
				{
					var start = (segment.tanggal_awal ?? segment.tanggal_akhir ?? segment.tanggal_awal ?? DateTime.MinValue).Date;
					var end = (segment.tanggal_akhir ?? segment.tanggal_awal ?? start).Date;
					if (end < start)
					{
						var temp = start;
						start = end;
						end = temp;
					}

					if (string.IsNullOrWhiteSpace(segment.status_baru))
					{
						return Json(new { success = false, message = "Setiap tanggal harus memiliki kode roster revisi." });
					}

					for (var date = start; date <= end; date = date.AddDays(1))
					{
						coveredDates.Add(date);
					}
				}

				if (coveredDates.Count != totalHari || !coveredDates.Contains(tanggalMulai) || !coveredDates.Contains(tanggalAkhir))
				{
					return Json(new { success = false, message = "Detail perubahan tidak sesuai dengan rentang tanggal yang dipilih." });
				}

				var rosterDetails = await _context.tbl_m_roster_detail
					.Where(r => r.nik == model.nik && r.working_date.HasValue && r.working_date.Value.Date >= tanggalMulai && r.working_date.Value.Date <= tanggalAkhir)
					.ToDictionaryAsync(r => r.working_date.Value.Date, r => r.status);

				foreach (var segment in orderedSegments)
				{
					var start = (segment.tanggal_awal ?? segment.tanggal_akhir ?? segment.tanggal_awal ?? tanggalMulai).Date;
					var end = (segment.tanggal_akhir ?? segment.tanggal_awal ?? start).Date;
					if (end < start)
					{
						var temp = start;
						start = end;
						end = temp;
					}

					for (var date = start; date <= end; date = date.AddDays(1))
					{
						rosterDetails.TryGetValue(date, out var actualStatus);
						segment.status_awal = actualStatus ?? segment.status_awal ?? "TIDAK ADA DATA";
					}
				}

				model.status_awal = orderedSegments.First().status_awal ?? "TIDAK ADA DATA";
				model.status_baru = orderedSegments.First().status_baru;
				model.segments = JsonSerializer.Serialize(orderedSegments);

				model.id = Guid.NewGuid().ToString();
				model.status = "DIAJUKAN";
				model.insert_by = nrp;
				model.ip = HttpContext.Connection.RemoteIpAddress?.ToString();

				var currentTime = DateTime.Now;

				// --- PERBAIKAN: Simpan sebagai DateTime, bukan string ---
				model.created_at = currentTime; // <-- Hapus .ToString(...)
				model.updated_at = currentTime; // <-- Hapus .ToString(...)

				_context.tbl_r_revisi_roster.Add(model);
				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "Pengajuan revisi roster berhasil disimpan." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat menyimpan pengajuan revisi roster. Pesan InnerException: {InnerExceptionMessage}", ex.InnerException?.Message);
				return Json(new { success = false, message = $"Terjadi kesalahan internal pada server. Silakan hubungi administrator." });
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetAll(bool includeAllDepartments = false)
		{
			try
			{
				var kategoriUserId = HttpContext.Session.GetString("kategori_user_id");
				var departemenUser = HttpContext.Session.GetString("dept");

			var dataQuery = from revisi in _context.tbl_r_revisi_roster
								join karyawan in _context.vw_m_karyawan_indexim
									on revisi.nik equals karyawan.no_nik into gj
								from subkaryawan in gj.DefaultIfEmpty()
								select new
								{
									revisi.id,
									tanggal = revisi.created_at ?? revisi.updated_at ?? revisi.tanggal_awal ?? revisi.tanggal_roster,
									revisi.nik,
									nama_lengkap = subkaryawan.nama_lengkap ?? "N/A",
									tanggal_roster = revisi.tanggal_roster,
									tanggal_awal = revisi.tanggal_awal,
									tanggal_akhir = revisi.tanggal_akhir,
									revisi.status_awal,
									revisi.status_baru,
									revisi.status,
									revisi.remarks,
									depart = subkaryawan.depart,
									revisi.file_path,
									revisi.segments
								};


				var isAdministrator = string.Equals(kategoriUserId, "ADMINISTRATOR", StringComparison.OrdinalIgnoreCase);
				var isAllDeptViewer = !string.IsNullOrWhiteSpace(departemenUser) &&
									  string.Equals(departemenUser.Trim(), "HUMAN CAPITAL & GENERAL SERVICES", StringComparison.OrdinalIgnoreCase);

				if (!includeAllDepartments && !isAdministrator && !isAllDeptViewer && !string.IsNullOrWhiteSpace(departemenUser))
				{
					dataQuery = dataQuery.Where(x => x.depart == departemenUser);
				}

				var rawData = await dataQuery
					.OrderByDescending(x => x.tanggal ?? DateTime.MinValue)
					.ToListAsync();

				var processed = rawData.Select(x =>
				{
					var segments = ParseSegments(x.segments);
					return new
					{
						x.id,
						x.tanggal,
						x.nik,
						x.nama_lengkap,
						x.tanggal_roster,
						x.tanggal_awal,
						x.tanggal_akhir,
						x.status_awal,
						x.status_baru,
						x.status,
						x.remarks,
						x.depart,
						x.file_path,
						status_awal_summary = BuildSummary(segments, s => s.status_awal),
						status_baru_summary = BuildSummary(segments, s => s.status_baru)
					};
				}).ToList();

				return Json(new { data = processed });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching all revisi roster data.");
				return Json(new { success = false, message = $"Terjadi kesalahan saat mengambil data: {ex.Message}" });
			}
		}


		// Endpoint untuk mengambil data yang perlu di-approve
		[HttpGet]
		public async Task<IActionResult> GetPendingApprovals()
		{
			try
			{
				var rawData = await (from revisi in _context.tbl_r_revisi_roster
				//ÿ where revisi.status == "DIAJUKAN" // <-- Tinjau ini jika Anda hanya ingin yg pending
				join karyawan in _context.vw_m_karyawan_indexim on revisi.nik equals karyawan.no_nik into gj
				from subkaryawan in gj.DefaultIfEmpty()
				select new
				{
					revisi.id,
					tanggal = revisi.created_at ?? revisi.updated_at ?? revisi.tanggal_awal ?? revisi.tanggal_roster,
					revisi.nik,
					nama_lengkap = subkaryawan.nama_lengkap ?? "N/A",
					tanggal_roster = revisi.tanggal_roster,
					tanggal_awal = revisi.tanggal_awal,
					tanggal_akhir = revisi.tanggal_akhir,
					revisi.status_awal,
					revisi.status_baru,
					revisi.status,
					revisi.remarks,
					revisi.file_path,
					revisi.segments
				}).OrderByDescending(x => x.tanggal)
				.ToListAsync();

			var processed = rawData.Select(x =>
			{
				var segments = ParseSegments(x.segments);
				return new
				{
					x.id,
					x.tanggal,
					x.nik,
					x.nama_lengkap,
					x.tanggal_roster,
					x.tanggal_awal,
					x.tanggal_akhir,
					x.status_awal,
					x.status_baru,
					x.status,
					x.remarks,
					x.file_path,
					status_awal_summary = BuildSummary(segments, s => s.status_awal),
					status_baru_summary = BuildSummary(segments, s => s.status_baru)
				};
			}).ToList();

			return Json(new { data = processed });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching pending approvals.");
				return Json(new { success = false, message = "Gagal mengambil data persetujuan." });
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetRosterTimeline(string nik, DateTime tanggal)
		{
			if (string.IsNullOrEmpty(nik))
			{
				return Json(new { success = false, message = "NIK tidak valid." });
			}

			try
			{
				var karyawan = await _context.vw_m_karyawan_indexim
					.Select(k => new { k.no_nik, k.nama_lengkap, k.depart })
					.FirstOrDefaultAsync(k => k.no_nik == nik);

				if (karyawan == null)
				{
					return Json(new { success = false, message = "Karyawan tidak ditemukan." });
				}

				// PERBAIKAN: Menghapus filter status == "DIAJUKAN"
				// Sekarang kita ambil revisi terbaru untuk tanggal tersebut, apapun statusnya.
				var revisi = await _context.tbl_r_revisi_roster
					.Where(r => r.nik == nik && (r.tanggal_awal.HasValue || r.tanggal_roster.HasValue))
					.Where(r => r.tanggal_awal.HasValue ? r.tanggal_awal.Value.Date <= (tanggal == default ? DateTime.Now.Date : tanggal.Date)
													   : r.tanggal_roster.Value.Date <= (tanggal == default ? DateTime.Now.Date : tanggal.Date))
					.OrderByDescending(r => r.created_at ?? r.updated_at ?? r.tanggal_awal ?? r.tanggal_roster)
					.FirstOrDefaultAsync();

				if (revisi == null)
				{
					return Json(new { success = false, message = "Data revisi tidak ditemukan." });
				}

				var (rangeAwal, rangeAkhir) = GetRangeDates(revisi);
				var anchorDate = tanggal == default ? rangeAwal : tanggal.Date;

				var startDate = anchorDate.AddDays(-7);
				var endDate = anchorDate.AddDays(7);

				var rosterDetails = await _context.tbl_m_roster_detail
					.Where(r => r.nik == nik && r.working_date >= startDate && r.working_date <= endDate)
					.ToDictionaryAsync(r => r.working_date.Value.Date, r => r.status);

				var timeline = new List<object>();
				for (var dt = startDate; dt <= endDate; dt = dt.AddDays(1))
				{
					rosterDetails.TryGetValue(dt, out var status);
					timeline.Add(new { date = dt.ToString("yyyy-MM-dd"), status = status });
				}

				var segments = ParseSegments(revisi.segments);
				var summaryAwal = BuildSummary(segments, s => s.status_awal);
				var summaryBaru = BuildSummary(segments, s => s.status_baru);
				var detailPerDay = BuildDailySegmentDetails(segments);

				var response = new
				{
					success = true,
					karyawan = karyawan,
					timeline = timeline,
					perubahan = new
					{
						tanggal = anchorDate.ToString("yyyy-MM-dd"),
						periode_awal = rangeAwal.ToString("yyyy-MM-dd"),
						periode_akhir = rangeAkhir.ToString("yyyy-MM-dd"),
						awal_summary = summaryAwal,
						baru_summary = summaryBaru,
						detail = detailPerDay
					}
				};

				return Json(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting roster timeline for NIK {nik}", nik);
				return Json(new { success = false, message = "Terjadi kesalahan server." });
			}
		}


		// Endpoint untuk menyetujui pengajuan
		[HttpPost]
		public async Task<IActionResult> ApproveRequest(string id)
		{
			if (string.IsNullOrEmpty(id)) return BadRequest();

			using (var transaction = await _context.Database.BeginTransactionAsync())
			{
				try
				{
					var revisi = await _context.tbl_r_revisi_roster.FindAsync(id);
					if (revisi == null || revisi.status != "DIAJUKAN")
					{
						return Json(new { success = false, message = "Data pengajuan tidak ditemukan atau sudah diproses." });
					}

					if (!revisi.tanggal_awal.HasValue && !revisi.tanggal_roster.HasValue)
					{
						return Json(new { success = false, message = "Tanggal jadwal pada pengajuan tidak valid." });
					}

					var (startDate, endDate) = GetRangeDates(revisi);
					var now = DateTime.Now;

					var segments = ParseSegments(revisi.segments);
					if (segments == null || segments.Count == 0)
					{
						segments = new List<RevisiRosterSegment>
						{
							new RevisiRosterSegment
							{
								tanggal_awal = startDate,
								tanggal_akhir = endDate,
								status_awal = revisi.status_awal,
								status_baru = revisi.status_baru
							}
						};
					}

					var existingDetails = await _context.tbl_m_roster_detail
						.Where(r => r.nik == revisi.nik && r.working_date.HasValue && r.working_date.Value.Date >= startDate && r.working_date.Value.Date <= endDate)
						.ToListAsync();

					foreach (var segment in segments)
					{
						var segStart = (segment.tanggal_awal ?? segment.tanggal_akhir ?? startDate).Date;
						var segEnd = (segment.tanggal_akhir ?? segment.tanggal_awal ?? segStart).Date;
						if (segEnd < segStart)
						{
							var temp = segStart;
							segStart = segEnd;
							segEnd = temp;
						}

						for (var dt = segStart; dt <= segEnd; dt = dt.AddDays(1))
						{
							var rosterDetail = existingDetails.FirstOrDefault(r => r.working_date.HasValue && r.working_date.Value.Date == dt);
							if (rosterDetail == null)
							{
								rosterDetail = new tbl_m_roster_detail
								{
									id = Guid.NewGuid().ToString(),
									nik = revisi.nik,
									working_date = dt,
									created_at = now,
									ip = HttpContext.Connection.RemoteIpAddress?.ToString(),
									remarks = "Revisi Roster"
								};
								_context.tbl_m_roster_detail.Add(rosterDetail);
								existingDetails.Add(rosterDetail);
							}
							else
							{
								rosterDetail.remarks = "Revisi Roster";
							}

							rosterDetail.status = segment.status_baru;
							rosterDetail.updated_at = now;
						}
					}

					revisi.status = "DISETUJUI";

					// --- PERBAIKAN: Simpan sebagai DateTime, bukan string ---
					revisi.updated_at = now; // <-- Hapus .ToString(...)

					await _context.SaveChangesAsync();
					await transaction.CommitAsync();

					return Json(new { success = true, message = "Pengajuan berhasil disetujui." });
				}
				catch (Exception ex)
				{
					await transaction.RollbackAsync();
					_logger.LogError(ex, "Error saat menyetujui pengajuan ID: {RequestId}", id);
					return Json(new { success = false, message = "Terjadi kesalahan pada server saat proses persetujuan." });
				}
			}
		}

		// Endpoint untuk menolak pengajuan
		[HttpPost]

		public async Task<IActionResult> RejectRequest(string id, string alasan)
		{
			if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(alasan))
			{
				return BadRequest("ID dan alasan penolakan harus diisi.");
			}

			try
			{
				var revisi = await _context.tbl_r_revisi_roster.FindAsync(id);
				if (revisi == null || revisi.status != "DIAJUKAN")
				{
					return Json(new { success = false, message = "Data pengajuan tidak ditemukan atau sudah diproses." });
				}

				revisi.status = "DITOLAK";
				revisi.remarks += $" | DITOLAK: {alasan}";

				// --- PERBAIKAN: Simpan sebagai DateTime, bukan string ---
				revisi.updated_at = DateTime.Now; // <-- Hapus .ToString(...)

				await _context.SaveChangesAsync();
				return Json(new { success = true, message = "Pengajuan berhasil ditolak." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat menolak pengajuan ID: {RequestId}", id);
				return Json(new { success = false, message = "Terjadi kesalahan pada server saat proses penolakan." });
			}
		}

		private (DateTime startDate, DateTime endDate) GetRangeDates(tbl_r_revisi_roster revisi)
		{
			var startDate = revisi.tanggal_awal?.Date ?? revisi.tanggal_roster?.Date ?? DateTime.Now.Date;
			var endDate = revisi.tanggal_akhir?.Date ?? startDate;
			if (endDate < startDate)
			{
				endDate = startDate;
			}
			return (startDate, endDate);
		}

		private List<RevisiRosterSegment> ParseSegments(string? segmentsJson)
		{
			if (string.IsNullOrWhiteSpace(segmentsJson))
			{
				return new List<RevisiRosterSegment>();
			}

			try
			{
				var segments = JsonSerializer.Deserialize<List<RevisiRosterSegment>>(segmentsJson);
				return segments ?? new List<RevisiRosterSegment>();
			}
			catch (JsonException)
			{
				return new List<RevisiRosterSegment>();
			}
		}

		private List<SegmentSummary> BuildSummary(List<RevisiRosterSegment> segments, Func<RevisiRosterSegment, string?> selector)
		{
			var summaries = new List<SegmentSummary>();
			if (segments == null || segments.Count == 0)
			{
				return summaries;
			}

			var ordered = segments
				.OrderBy(s => s.tanggal_awal ?? s.tanggal_akhir ?? DateTime.MinValue)
				.ToList();

			foreach (var segment in ordered)
			{
				var status = selector(segment);
				if (string.IsNullOrEmpty(status))
				{
					continue;
				}

				var start = segment.tanggal_awal ?? segment.tanggal_akhir ?? DateTime.Now.Date;
				var end = segment.tanggal_akhir ?? start;

				if (start > end)
				{
					var temp = start;
					start = end;
					end = temp;
				}

				if (summaries.Count > 0)
				{
					var last = summaries.Last();
					if (last.status == status && last.endDate.AddDays(1) >= start)
					{
						last.endDate = end > last.endDate ? end : last.endDate;
						continue;
					}
				}

				summaries.Add(new SegmentSummary
				{
					startDate = start,
					endDate = end,
					status = status
				});
			}

			return summaries;
		}

		private List<object> BuildDailySegmentDetails(List<RevisiRosterSegment> segments)
		{
			var detailsMap = new SortedDictionary<DateTime, (string? statusAwal, string? statusBaru)>();
			if (segments == null || segments.Count == 0)
			{
				return new List<object>();
			}

			foreach (var segment in segments.OrderBy(s => s.tanggal_awal ?? s.tanggal_akhir ?? DateTime.MinValue))
			{
				var start = (segment.tanggal_awal ?? segment.tanggal_akhir ?? DateTime.MinValue).Date;
				var end = (segment.tanggal_akhir ?? segment.tanggal_awal ?? start).Date;

				if (start == DateTime.MinValue && end == DateTime.MinValue)
				{
					continue;
				}

				if (end < start)
				{
					var temp = start;
					start = end;
					end = temp;
				}

				for (var date = start; date <= end; date = date.AddDays(1))
				{
					detailsMap[date] = (segment.status_awal, segment.status_baru);
				}
			}

			return detailsMap.Select(kvp => new
			{
				tanggal = kvp.Key.ToString("yyyy-MM-dd"),
				status_awal = kvp.Value.statusAwal,
				status_baru = kvp.Value.statusBaru
			}).ToList<object>();
		}

		private class RevisiRosterSegment
		{
			public DateTime? tanggal_awal { get; set; }
			public DateTime? tanggal_akhir { get; set; }
			public string? status_awal { get; set; }
			public string? status_baru { get; set; }
		}

		private class SegmentSummary
		{
			public DateTime startDate { get; set; }
			public DateTime endDate { get; set; }
			public string? status { get; set; }
		}

		[HttpPost]
		public async Task<IActionResult> Delete(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return Json(new { success = false, message = "ID tidak valid." });
			}

			try
			{
				var revisi = await _context.tbl_r_revisi_roster.FindAsync(id);

				if (revisi == null)
				{
					return Json(new { success = false, message = "Data pengajuan tidak ditemukan." });
				}

				if (revisi.status != "DIAJUKAN")
				{
					return Json(new { success = false, message = "Data yang sudah diproses (Disetujui/Ditolak) tidak dapat dihapus." });
				}

				// Hapus file fisik jika ada
				if (!string.IsNullOrEmpty(revisi.file_path))
				{
					try
					{
						// [PERBAIKAN] Gunakan IWebHostEnvironment untuk path dinamis
						string wwwRootPath = _webHostEnvironment.WebRootPath;
						// Pastikan file_path dimulai dengan '/' agar Path.Combine bekerja benar
						string relativePath = revisi.file_path.StartsWith("/") ? revisi.file_path.TrimStart('/') : revisi.file_path;
						relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar); // Sesuaikan separator
						string physicalPath = Path.Combine(wwwRootPath, relativePath);


						if (System.IO.File.Exists(physicalPath))
						{
							System.IO.File.Delete(physicalPath);
							_logger.LogInformation("Berhasil menghapus file fisik: {FilePath}", physicalPath);
						}
						else
						{
							_logger.LogWarning("File fisik tidak ditemukan saat mencoba menghapus: {FilePath}", physicalPath);
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Gagal menghapus file fisik: {FilePath}", revisi.file_path);
					}
				}

				_context.tbl_r_revisi_roster.Remove(revisi);
				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "Pengajuan berhasil dibatalkan dan dihapus." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat menghapus pengajuan ID: {RequestId}", id);
				return Json(new { success = false, message = "Terjadi kesalahan pada server saat proses penghapusan." });
			}
		}
	}
}
