using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using one_db.Data;
using one_db.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace one_db.Controllers
{
	[Authorize]
	public class TravelController : Controller
	{
		private readonly AppDBContext _context;
		private readonly ILogger<TravelController> _logger;

		public TravelController(AppDBContext context, ILogger<TravelController> logger)
		{
			_context = context;
			_logger = logger;
		}

		[Authorize]
		public IActionResult Index()
		{
			try
			{
				var kategoriUserId = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
				var compCode = User.FindFirst("comp_code")?.Value;
				var nrp = User.Identity?.Name;
				var dept = User.FindFirst("dept_code")?.Value;
				var nama = User.FindFirst("nama")?.Value;

				_context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

				kategoriUserId ??= HttpContext.Session.GetString("kategori_user_id");
				compCode ??= HttpContext.Session.GetString("company");
				nrp ??= HttpContext.Session.GetString("nrp");
				dept ??= HttpContext.Session.GetString("dept");
				nama ??= HttpContext.Session.GetString("nama");

				if (string.IsNullOrEmpty(kategoriUserId) || string.IsNullOrEmpty(nrp))
				{
					_logger.LogWarning("User tanpa kategori_user_id mencoba akses TravelController.");
					return RedirectToAction("Index", "Login");
				}

				var controllerName = ControllerContext.ActionDescriptor.ControllerName;

				bool punyaAkses = _context.tbl_r_menu.Any(x =>
					x.kategori_user_id == kategoriUserId &&
					(x.comp_code == null || x.comp_code == compCode) &&
					x.link_controller == controllerName);

				if (!punyaAkses)
				{
					_logger.LogWarning($"Akses ditolak: {nrp} ({kategoriUserId}) mencoba akses {controllerName}");
					return RedirectToAction("Index", "Login");
				}

				var menuList = _context.tbl_r_menu
					.Where(x => x.kategori_user_id == kategoriUserId)
					.Where(x => x.comp_code == null || x.comp_code == compCode)
					.OrderBy(x => x.type)
					.ThenBy(x => x.title)
					.ToList();

				ViewBag.MenuMasterCount = menuList.Count(x => x.type == "Master");
				ViewBag.MaOwnerCount = menuList.Count(x => x.type == "MaOwner");
				ViewBag.HROwnerCount = menuList.Count(x => x.type == "HROwner");
				ViewBag.AdminOwnerCount = menuList.Count(x => x.type == "AdminOwner");
				ViewBag.AdminKonCount = menuList.Count(x => x.type == "AdminKon");

				ViewBag.Title = "Travel Authorization";
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
				_logger.LogError(ex, "Error saat load TravelController");
				return RedirectToAction("Index", "Login");
			}
		}

		[Authorize]
		[HttpGet]
		public async Task<IActionResult> GetAll()
		{
			try
			{
				var data = await _context.tbl_m_travel_authorization
					.OrderByDescending(x => x.created_at)
					.Select(x => new
					{
						x.id,
						x.nik,
						nama = _context.vw_m_karyawan_indexim
							.Where(k => k.no_nik == x.nik)
							.Select(k => k.nama_lengkap)
							.FirstOrDefault(),
						departemen = _context.vw_m_karyawan_indexim
							.Where(k => k.no_nik == x.nik)
							.Select(k => k.depart)
							.FirstOrDefault(),
						kd_depart = _context.vw_m_karyawan_indexim
							.Where(k => k.no_nik == x.nik)
							.Select(k => k.kd_depart)
							.FirstOrDefault(),
						x.out_site,
						x.on_site,
						x.wilayah,
						x.poh,
						x.nominal,
						x.nomor_ta,
						x.created_at,
						x.created_by
					})
					.ToListAsync();

				return Json(new { data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching travel authorization data.");
				return Json(new { success = false, message = "Terjadi kesalahan saat mengambil data travel." });
			}
		}

		[Authorize]
		[HttpGet]
		public async Task<IActionResult> Get(string id)
		{
			try
			{
				var data = await _context.tbl_m_travel_authorization.FirstOrDefaultAsync(x => x.id == id);
				if (data == null)
				{
					return Json(new { success = false, message = "Data tidak ditemukan." });
				}

				return Json(new { success = true, data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat mengambil data travel by id.");
				return Json(new { success = false, message = "Gagal mengambil data travel." });
			}
		}

		[Authorize]
		[HttpGet]
		public async Task<IActionResult> Employees(string? keyword)
		{
			try
			{
				var query = _context.vw_m_karyawan_indexim.AsQueryable();

				if (!string.IsNullOrWhiteSpace(keyword))
				{
					query = query.Where(k =>
						(k.no_nik ?? "").Contains(keyword) ||
						(k.nama_lengkap ?? "").Contains(keyword));
				}

				var data = await query
					.OrderBy(k => k.no_nik)
					.Take(25)
					.Select(k => new
					{
						k.no_nik,
						k.nama_lengkap,
						k.depart,
						k.posisi,
						k.section,
						k.lokterima,
						k.poh,
						k.nominal
					})
					.ToListAsync();

				return Json(new { data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat mengambil data karyawan untuk travel.");
				return Json(new { success = false, message = "Gagal mengambil data karyawan." });
			}
		}

		[Authorize]
		[HttpGet]
		public async Task<IActionResult> OnsiteStatus(string nik, DateTime? outDate)
		{
			if (string.IsNullOrWhiteSpace(nik))
			{
				return Json(new { success = false, message = "NIK wajib diisi." });
			}

			try
			{
				var normalizedNik = NormalizeNik(nik);
				var lastTravel = await _context.tbl_m_travel_authorization
					.Where(x => x.nik == normalizedNik)
					.OrderByDescending(x => x.on_site ?? x.out_site)
					.FirstOrDefaultAsync();

				DateTime? lastOnSite = lastTravel?.on_site ?? lastTravel?.out_site;
				int? gapDays = null;
				if (lastOnSite.HasValue && outDate.HasValue)
				{
					gapDays = (int)(outDate.Value.Date - lastOnSite.Value.Date).TotalDays;
				}

				return Json(new
				{
					success = true,
					last_on_site = lastOnSite,
					gap_days = gapDays,
					required_days = 42
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat mengecek status onsite.");
				return Json(new { success = false, message = "Gagal mengambil status onsite." });
			}
		}

		[Authorize]
		[HttpPost]
		public async Task<IActionResult> Create([FromBody] TravelAuthorizationRequest request)
		{
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
				return Json(new { success = false, message = string.Join("; ", errors) });
			}

			try
			{
				var outDate = request.out_site!.Value.Date;
				var warnings = new List<string>();

				if (!IsAllowedTravelDay(outDate.DayOfWeek))
				{
					warnings.Add("Out Site di luar jadwal Senin/Kamis/Sabtu.");
				}

				if (IsSpecialNik(request.nik) && outDate.Day % 2 != 0)
				{
					warnings.Add("Out Site untuk NIK ini idealnya jatuh pada tanggal genap.");
				}

				var leadDaysOut = (outDate - DateTime.Now.Date).TotalDays;
				var isSusulan = leadDaysOut < 14;
				if (isSusulan)
				{
					warnings.Add("Pengajuan kurang dari 14 hari dan akan dimasukkan ke TA susulan.");
				}

				if (request.on_site.HasValue)
				{
					var leadDaysOn = (request.on_site.Value.Date - DateTime.Now.Date).TotalDays;
					if (leadDaysOn < 14)
					{
						warnings.Add("On Site kurang dari 14 hari dari hari ini.");
					}
				}

				var nikNormalized = NormalizeNik(request.nik);

				var nomorTa = string.IsNullOrWhiteSpace(request.nomor_ta)
					? await GenerateNomorTaAsync(outDate, isSusulan)
					: request.nomor_ta!.Trim();

				var entity = new tbl_m_travel_authorization
				{
					id = Guid.NewGuid().ToString(),
					nik = nikNormalized,
					out_site = outDate,
					on_site = request.on_site?.Date,
					wilayah = request.wilayah?.Trim(),
					poh = request.poh?.Trim(),
					nominal = request.nominal?.Trim(),
					nomor_ta = nomorTa,
					created_at = DateTime.Now,
					created_by = User.Identity?.Name
				};

				_context.tbl_m_travel_authorization.Add(entity);
				await _context.SaveChangesAsync();

				return Json(new
				{
					success = true,
					message = "Travel authorization berhasil ditambahkan.",
					warning = warnings.Any() ? string.Join(" | ", warnings) : null
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat membuat travel authorization.");
				return Json(new { success = false, message = "Terjadi kesalahan saat menyimpan data travel." });
			}
		}

		[Authorize]
		[HttpPost]
		public async Task<IActionResult> Update([FromBody] TravelAuthorizationRequest request)
		{
			if (string.IsNullOrWhiteSpace(request.id))
			{
				return Json(new { success = false, message = "ID travel tidak ditemukan." });
			}

			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
				return Json(new { success = false, message = string.Join("; ", errors) });
			}

			try
			{
				var data = await _context.tbl_m_travel_authorization.FirstOrDefaultAsync(x => x.id == request.id);
				if (data == null)
				{
					return Json(new { success = false, message = "Data tidak ditemukan." });
				}

				var outDate = request.out_site!.Value.Date;
				var warnings = new List<string>();

				if (!IsAllowedTravelDay(outDate.DayOfWeek))
				{
					warnings.Add("Out Site di luar jadwal Senin/Kamis/Sabtu.");
				}

				if (IsSpecialNik(request.nik) && outDate.Day % 2 != 0)
				{
					warnings.Add("Out Site untuk NIK ini idealnya jatuh pada tanggal genap.");
				}

				var leadDaysOut = (outDate - DateTime.Now.Date).TotalDays;
				var isSusulan = leadDaysOut < 14;
				if (isSusulan)
				{
					warnings.Add("Pengajuan kurang dari 14 hari dan akan dimasukkan ke TA susulan.");
				}

				if (request.on_site.HasValue)
				{
					var leadDaysOn = (request.on_site.Value.Date - DateTime.Now.Date).TotalDays;
					if (leadDaysOn < 14)
					{
						warnings.Add("On Site kurang dari 14 hari dari hari ini.");
					}
				}

				var nikNormalized = NormalizeNik(request.nik);

				var nomorTa = string.IsNullOrWhiteSpace(request.nomor_ta)
					? await GenerateNomorTaAsync(outDate, isSusulan, request.id)
					: request.nomor_ta!.Trim();

				data.nik = nikNormalized;
				data.out_site = outDate;
				data.on_site = request.on_site?.Date;
				data.wilayah = request.wilayah?.Trim();
				data.poh = request.poh?.Trim();
				data.nominal = request.nominal?.Trim();
				data.nomor_ta = nomorTa;
				data.update_at = DateTime.Now;
				data.update_by = User.Identity?.Name;

				await _context.SaveChangesAsync();

				return Json(new
				{
					success = true,
					message = "Travel authorization berhasil diperbarui.",
					warning = warnings.Any() ? string.Join(" | ", warnings) : null
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat memperbarui travel authorization.");
				return Json(new { success = false, message = "Terjadi kesalahan saat memperbarui data travel." });
			}
		}

		[Authorize]
		[HttpPost]
		public async Task<IActionResult> Delete(string id)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				return Json(new { success = false, message = "ID travel tidak ditemukan." });
			}

			try
			{
				var data = await _context.tbl_m_travel_authorization.FirstOrDefaultAsync(x => x.id == id);
				if (data == null)
				{
					return Json(new { success = false, message = "Data tidak ditemukan." });
				}

				_context.tbl_m_travel_authorization.Remove(data);
				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "Travel authorization berhasil dihapus." });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat menghapus travel authorization.");
				return Json(new { success = false, message = "Terjadi kesalahan saat menghapus data travel." });
			}
		}

		[Authorize]
		public IActionResult Report()
		{
			return View();
		}

		[Authorize]
		[HttpGet]
		public async Task<IActionResult> GetReportData(string? mode, DateTime? start, DateTime? end, string? nomorTa)
		{
			try
			{
				var today = DateTime.Today;

				var query = from t in _context.tbl_m_travel_authorization
							join k in _context.vw_m_karyawan_indexim on t.nik equals k.no_nik into gj
							from k in gj.DefaultIfEmpty()
							select new
							{
								t.id,
								t.nik,
								t.nomor_ta,
								t.out_site,
								t.on_site,
								t.wilayah,
								t.poh,
								t.nominal,
								t.created_at,
								t.created_by,
								nama = k.nama_lengkap,
								depart = k.depart,
								kd_depart = k.kd_depart
							};

				if (mode == "upcoming")
				{
					query = query.Where(x => x.out_site >= today);
				}
				else if (mode == "past")
				{
					query = query.Where(x => x.on_site < today);
				}
				else if (mode == "period" && start.HasValue && end.HasValue)
				{
					var s = start.Value.Date;
					var e = end.Value.Date;
					query = query.Where(x => x.out_site >= s && x.out_site <= e);
				}
				else if (mode == "latest")
				{
					query = query.GroupBy(x => x.nik)
						.Select(g => g.OrderByDescending(x => x.out_site).FirstOrDefault()!);
				}

				if (!string.IsNullOrWhiteSpace(nomorTa))
				{
					query = query.Where(x => x.nomor_ta == nomorTa);
				}

				var data = await query
					.OrderByDescending(x => x.out_site)
					.ToListAsync();

				return Json(new { success = true, data });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat mengambil data report TA.");
				return Json(new { success = false, message = "Gagal mengambil data report TA." });
			}
		}

		[Authorize]
		[HttpGet]
		public async Task<IActionResult> GetNomorTaOptions()
		{
			try
			{
				var options = await _context.tbl_m_travel_authorization
					.Where(x => !string.IsNullOrWhiteSpace(x.nomor_ta))
					.GroupBy(x => x.nomor_ta!)
					.Select(g => new
					{
						nomor_ta = g.Key,
						start_out = g.Min(x => x.out_site),
						end_on = g.Max(x => x.on_site)
					})
					.OrderByDescending(x => x.start_out)
					.ToListAsync();

				return Json(new { success = true, data = options });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat mengambil daftar Nomor TA.");
				return Json(new { success = false, message = "Gagal memuat daftar Nomor TA." });
			}
		}

		[Authorize]
		[HttpGet]
		public async Task<IActionResult> NextNomorTa(DateTime? outDate, string? excludeId)
		{
			if (!outDate.HasValue)
			{
				return Json(new { success = false, message = "Tanggal Out Site wajib diisi." });
			}

			try
			{
				var leadDaysOut = (outDate.Value.Date - DateTime.Now.Date).TotalDays;
				var isSusulan = leadDaysOut < 14;
				var nomorTa = await GenerateNomorTaAsync(outDate.Value.Date, isSusulan, excludeId);
				return Json(new { success = true, nomorTa });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saat menentukan nomor TA berikutnya.");
				return Json(new { success = false, message = "Gagal menentukan nomor TA." });
			}
		}

		private async Task<string> GenerateNomorTaAsync(DateTime outDate, bool isSusulan, string? excludeId = null)
		{
			var weekStart = GetWeekStartMonday(outDate);
			var yearStart = new DateTime(outDate.Year, 1, 1);
			if (weekStart < yearStart)
			{
				weekStart = yearStart;
			}

			var weekEnd = weekStart.AddDays(6);

			var baseQuery = _context.tbl_m_travel_authorization
				.Where(x => x.out_site.HasValue && x.out_site >= weekStart && x.out_site <= weekEnd);

			if (!string.IsNullOrWhiteSpace(excludeId))
			{
				baseQuery = baseQuery.Where(x => x.id != excludeId);
			}

			var existingNomors = await baseQuery
				.Where(x => !string.IsNullOrWhiteSpace(x.nomor_ta))
				.Select(x => x.nomor_ta!)
				.ToListAsync();

			var maxSequence = existingNomors
				.Select(ParseSequence)
				.DefaultIfEmpty(0)
				.Max();

			var sequence = maxSequence + 1;

			if (isSusulan && maxSequence == 0)
			{
				var lastSequence = await GetLastSequenceAsync(excludeId);
				if (lastSequence > 0)
				{
					sequence = lastSequence + 1;
				}
			}
			var runningNumber = sequence.ToString("D3", CultureInfo.InvariantCulture);
			var romanMonth = MonthToRoman(outDate.Month);
			return $"{runningNumber}/HC/TA/{romanMonth}/{outDate:yyyy}";
		}

		private async Task<int> GetLastSequenceAsync(string? excludeId = null)
		{
			var query = _context.tbl_m_travel_authorization.AsQueryable();
			if (!string.IsNullOrWhiteSpace(excludeId))
			{
				query = query.Where(x => x.id != excludeId);
			}

			var latestNomorTa = await query
				.Where(x => !string.IsNullOrWhiteSpace(x.nomor_ta))
				.OrderByDescending(x => x.created_at ?? DateTime.MinValue)
				.Select(x => x.nomor_ta!)
				.FirstOrDefaultAsync();

			return ParseSequence(latestNomorTa);
		}

		private static DateTime GetWeekStartMonday(DateTime date)
		{
			var offset = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
			return date.Date.AddDays(-offset);
		}

		private static bool IsAllowedTravelDay(DayOfWeek day)
		{
			return day == DayOfWeek.Monday || day == DayOfWeek.Thursday || day == DayOfWeek.Saturday;
		}

		private static bool IsSpecialNik(string? nik)
		{
			return string.Equals(nik?.Trim(), "23051670785", StringComparison.OrdinalIgnoreCase);
		}

		private static string NormalizeNik(string? nik)
		{
			return nik?.Trim() ?? string.Empty;
		}

		private static int ParseSequence(string nomorTa)
		{
			if (string.IsNullOrWhiteSpace(nomorTa)) return 0;
			var parts = nomorTa.Split('/', 2);
			if (parts.Length == 0) return 0;
			return int.TryParse(parts[0], out var seq) ? seq : 0;
		}

		private static string MonthToRoman(int month)
		{
			return month switch
			{
				1 => "I",
				2 => "II",
				3 => "III",
				4 => "IV",
				5 => "V",
				6 => "VI",
				7 => "VII",
				8 => "VIII",
				9 => "IX",
				10 => "X",
				11 => "XI",
				12 => "XII",
				_ => string.Empty
			};
		}
	}
}
