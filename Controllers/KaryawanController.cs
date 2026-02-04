using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using one_db.Data;
using one_db.Models;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentFTP;
using System.Linq.Dynamic.Core;
using System.Collections.Generic;

// --- TAMBAHKAN USING BARU UNTUK STREAMING EXCEL ---
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace one_db.Controllers
{
	// Model to receive filter data from the client for Excel export
	public class ExcelExportFilter
	{
		public List<string> ParentFilter { get; set; }
		public List<string> CompanyFilter { get; set; }
		public string SearchValue { get; set; }
		public string NikSearch { get; set; }
		public string DownloadToken { get; set; } // Token for UI loading indicator
	}

	[Authorize]
	public class KaryawanController : Controller
	{
		private readonly AppDBContext _context;
		private readonly ILogger<KaryawanController> _logger;
		private readonly FtpConfigg _ftpConfig;

		private readonly string controller_name = "Karyawan";
		private readonly string title_name = "Data Karyawan";

		public KaryawanController(
			AppDBContext context,
			ILogger<KaryawanController> logger,
			IOptions<FtpConfigg> ftpConfig)
		{
			_context = context;
			_logger = logger;
			_ftpConfig = ftpConfig.Value;
		}

		// --- FILTER LOGIC (IMPROVED) ---
		private IQueryable<vw_m_karyawan> GetFilteredKaryawanQuery(List<string>? parentFilter, List<string>? companyFilter, string? searchValue, string? nikSearch)
		{
			var query = _context.vw_m_karyawan.AsQueryable();

			// Global search from DataTable's search box
			if (!string.IsNullOrEmpty(searchValue))
			{
				query = query.Where(m =>
					(m.nama_lengkap != null && m.nama_lengkap.Contains(searchValue)) ||
					(m.posisi != null && m.posisi.Contains(searchValue)) ||
					(m.depart != null && m.depart.Contains(searchValue)) ||
					(m.level != null && m.level.Contains(searchValue)) ||
					(m.nama_perusahaan != null && m.nama_perusahaan.Contains(searchValue)) ||
					(m.hp_1 != null && m.hp_1.Contains(searchValue)) ||
					(m.email_kantor != null && m.email_kantor.Contains(searchValue))
				);
			}

			// Dedicated search for NIK / No. NIK / No. KTP
			if (!string.IsNullOrEmpty(nikSearch))
			{
				query = query.Where(m =>
					(m.no_ktp != null && m.no_ktp.Contains(nikSearch)) ||
					(m.no_nik != null && m.no_nik.Contains(nikSearch)) ||
					(m.no_ktp != null && m.no_ktp.Contains(nikSearch))
				);
			}

			var hasCompanyFilter = companyFilter != null && companyFilter.Any();
			var hasParentFilter = parentFilter != null && parentFilter.Any();

			// Parent & Company filter logic (this part was correct)
			if (hasCompanyFilter)
			{
				query = query.Where(m => m.nama_perusahaan != null && companyFilter.Contains(m.nama_perusahaan));
				if (hasParentFilter)
				{
					query = query.Where(m =>
						(m.parent != null && parentFilter.Contains(m.parent)) ||
						(m.nama_perusahaan != null && parentFilter.Contains(m.nama_perusahaan))
					);
				}
			}
			else if (hasParentFilter)
			{
				query = query.Where(m =>
					(m.parent != null && parentFilter.Contains(m.parent)) ||
					(m.nama_perusahaan != null && parentFilter.Contains(m.nama_perusahaan))
				);
			}

			return query;
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


		[Authorize]
		public IActionResult Detail()
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


		[HttpPost]
		public IActionResult GetAll()
		{
			try
			{
				var draw = Request.Form["draw"].FirstOrDefault();
				var start = Request.Form["start"].FirstOrDefault();
				var length = Request.Form["length"].FirstOrDefault();
				var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
				var sortColumnDirection = Request.Form["order[0][dir]"].FirstOrDefault();
				var searchValue = Request.Form["search[value]"].FirstOrDefault();

				var nikSearch = Request.Form["nikSearch"].FirstOrDefault();
				var parentFilter = Request.Form["parentFilter[]"].ToList();
				var companyFilter = Request.Form["companyFilter[]"].ToList();

				int pageSize = length != null ? Convert.ToInt32(length) : 0;
				int skip = start != null ? Convert.ToInt32(start) : 0;

				var query = GetFilteredKaryawanQuery(parentFilter, companyFilter, searchValue, nikSearch);

				int recordsTotal = _context.vw_m_karyawan.Count();
				int recordsFiltered = query.Distinct().Count();

				if (!string.IsNullOrEmpty(sortColumn) && !string.IsNullOrEmpty(sortColumnDirection))
				{
					query = query.OrderBy(sortColumn + " " + sortColumnDirection);
				}
				else
				{
					query = query.OrderBy(x => x.nama_lengkap);
				}

				var data = query.Distinct().Skip(skip).Take(pageSize).ToList();

				var jsonData = new { draw = draw, recordsFiltered = recordsFiltered, recordsTotal = recordsTotal, data = data };
				return Ok(jsonData);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fetching server-side employee data.");
				return BadRequest();
			}
		}

		[HttpGet]
		public async Task<IActionResult> GetCompanyFilterData()
		{
			try
			{
				var data = await _context.tbl_m_karyawan_old
					.Where(k => !string.IsNullOrEmpty(k.parent) && !string.IsNullOrEmpty(k.nama_perusahaan))
					.Select(k => new
					{
						Parent = k.parent.Trim(),
						Company = k.nama_perusahaan.Trim()
					})
					.Distinct()
					.ToListAsync();

				if (!data.Any())
				{
					return Json(new { success = false, message = "Tidak ada data perusahaan ditemukan." });
				}

				var grouped = data
					.GroupBy(d => d.Parent)
					.OrderBy(g => g.Key)
					.Select(g => new
					{
						parent = g.Key,
						companies = g.Select(x => x.Company)
									.Distinct()
									.OrderBy(x => x)
									.ToList()
					})
					.ToList();

				return Json(new { success = true, data = grouped });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting company filter data.");
				return Json(new { success = false, message = "Gagal memuat data filter." });
			}
		}

		// --- NEW STREAMING EXPORT METHOD FOR LARGE DATA ---
		[HttpPost]
		public async Task ExportToExcel([FromForm] ExcelExportFilter model)
		{
			_logger.LogInformation("Starting streaming export process...");

			// Set cookie to notify the UI that the server process has started
			if (!string.IsNullOrEmpty(model.DownloadToken))
			{
				Response.Cookies.Append(model.DownloadToken, "true", new CookieOptions
				{
					HttpOnly = false,
					Expires = DateTimeOffset.UtcNow.AddMinutes(5), // Cookie is valid for 5 minutes
					Path = "/"
				});
			}

			// Prepare the query without executing it (no ToListAsync)
			var query = GetFilteredKaryawanQuery(
				model.ParentFilter,
				model.CompanyFilter,
				model.SearchValue,
				model.NikSearch
			).AsNoTracking().Distinct().OrderBy(k => k.nama_lengkap);

			// Prepare the HTTP response for streaming
			var fileName = $"DataKaryawan_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
			Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
			Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";

			try
			{
				// Use OpenXmlWriter to stream directly to the Response.Body stream
				await using (var stream = Response.Body)
				{
					using (var spreadsheetDocument = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
					{
						var workbookPart = spreadsheetDocument.AddWorkbookPart();
						workbookPart.Workbook = new Workbook();
						var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
						var sheetData = new SheetData();
						worksheetPart.Worksheet = new Worksheet(sheetData);
						var sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());
						var sheet = new Sheet() { Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Data Karyawan" };
						sheets.Append(sheet);

						// Create Header Row
						var headerRow = new Row();
						headerRow.Append(
							ConstructCell("ID", CellValues.String),
							ConstructCell("Nama Lengkap", CellValues.String),
							ConstructCell("NIK", CellValues.String),
							ConstructCell("No. NIK", CellValues.String),
							ConstructCell("No. KTP", CellValues.String),
							ConstructCell("Posisi", CellValues.String),
							ConstructCell("Departemen", CellValues.String),
							ConstructCell("Level", CellValues.String),
							ConstructCell("Perusahaan", CellValues.String),
							ConstructCell("Parent", CellValues.String),
							ConstructCell("Kontak (HP)", CellValues.String),
							ConstructCell("Email", CellValues.String),
							ConstructCell("Tgl Nonaktif", CellValues.String)
						);
						sheetData.Append(headerRow);

						// STREAMING: Loop through data from DB row by row
						// AsAsyncEnumerable() is the key. It doesn't load all data into memory.
						int rowCount = 0;
						await foreach (var emp in query.AsAsyncEnumerable())
						{
							var dataRow = new Row();
							dataRow.Append(
								ConstructCell(emp.id ?? "-", CellValues.String),
								ConstructCell(emp.nama_lengkap ?? "-", CellValues.String),
								ConstructCell(emp.no_ktp ?? "-", CellValues.String),
								ConstructCell(emp.no_nik ?? "-", CellValues.String),
								ConstructCell(emp.no_ktp ?? "-", CellValues.String),
								ConstructCell(emp.posisi ?? "-", CellValues.String),
								ConstructCell(emp.depart ?? "-", CellValues.String),
								ConstructCell(emp.level ?? "-", CellValues.String),
								ConstructCell(emp.nama_perusahaan ?? "-", CellValues.String),
								ConstructCell(emp.parent ?? "-", CellValues.String),
								ConstructCell(emp.hp_1 ?? "-", CellValues.String),
								ConstructCell(emp.email_kantor ?? "-", CellValues.String),
								ConstructCell(emp.tgl_nonaktif?.ToString("dd-MM-yyyy") ?? "-", CellValues.String)
							);
							sheetData.Append(dataRow);

							rowCount++;
							// Periodically flush the stream to avoid buffering on the server
							if (rowCount % 1000 == 0)
							{
								await stream.FlushAsync();
								_logger.LogInformation($"Streaming... {rowCount} rows exported.");
							}
						}
					}
				}
				_logger.LogInformation($"Streaming export finished. Total rows.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error during OpenXML streaming");
				// If an error occurs, we can't send the file.
			}
		}

		// Helper function to construct an OpenXML cell
		private Cell ConstructCell(string value, CellValues dataType)
		{
			return new Cell()
			{
				CellValue = new CellValue(value),
				DataType = new EnumValue<CellValues>(dataType)
			};
		}

		// --- NEW ACTION TO CHECK DOWNLOAD STATUS VIA POLLING ---
		[HttpGet]
		public IActionResult CheckExportStatus(string token)
		{
			if (string.IsNullOrEmpty(token))
			{
				return BadRequest();
			}

			// Check if the cookie with the token name exists
			if (Request.Cookies.TryGetValue(token, out _))
			{
				// Delete the cookie after reading it
				Response.Cookies.Delete(token, new CookieOptions { Path = "/" });
				return Ok(new { ready = true });
			}

			return Ok(new { ready = false });
		}


		[HttpGet("/reports/karyawan/foto/{filename}")]
		public async Task<IActionResult> GetPhoto(string filename, [FromQuery] string? id_personal)
		{
			if (string.IsNullOrEmpty(filename)) return NotFound();

			var decodedFilename = System.Net.WebUtility.UrlDecode(filename);
			var safeFilename = Path.GetFileName(decodedFilename);

			string hashedFolder = "";
			if (!string.IsNullOrEmpty(id_personal))
			{
				using var md5 = MD5.Create();
				var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(id_personal));
				hashedFolder = string.Concat(hashBytes.Select(b => b.ToString("x2")));
			}

			byte[]? fileBytes = null;
			using (var client = new AsyncFtpClient(_ftpConfig.Host, _ftpConfig.User, _ftpConfig.Password, _ftpConfig.Port))
			{
				client.Config.EncryptionMode = FtpEncryptionMode.None;
				client.Config.ValidateAnyCertificate = true;

				try
				{
					await client.Connect();
					var path1 = $"/home/onedb_kary/foto/1/{safeFilename}";
					if (await client.FileExists(path1))
					{
						fileBytes = await client.DownloadBytes(path1, 0);
					}
					else if (!string.IsNullOrEmpty(hashedFolder))
					{
						var path2 = $"/home/onedb_kary/karyawan/{hashedFolder}/{safeFilename}";
						if (await client.FileExists(path2))
						{
							fileBytes = await client.DownloadBytes(path2, 0);
						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "FTP Error retrieving photo: {FileName}", safeFilename);
				}
				finally
				{
					if (client.IsConnected) await client.Disconnect();
				}
			}

			if (fileBytes == null)
			{
				var defaultPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "default-avatar.png");
				if (!System.IO.File.Exists(defaultPath)) return NotFound("Default avatar not found.");

				var defaultBytes = await System.IO.File.ReadAllBytesAsync(defaultPath);
				return File(defaultBytes, "image/png");
			}

			new FileExtensionContentTypeProvider().TryGetContentType(safeFilename, out var contentType);
			contentType ??= "application/octet-stream";

			Response.Headers["Cache-Control"] = "public,max-age=86400";
			return File(fileBytes, contentType);
		}
	}
}
