using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Logging;

using System;

using Microsoft.AspNetCore.Authorization;

using Microsoft.EntityFrameworkCore;

using one_db.Data;

using one_db.Models;

using System.Linq;

using System.Threading.Tasks;

using System.Collections.Generic;

using Microsoft.AspNetCore.Http;

using System.IO;

using Microsoft.AspNetCore.Hosting;

using NPOI.SS.UserModel;

using NPOI.XSSF.UserModel;

using NPOI.HSSF.UserModel;

using System.Text.Json;



namespace one_db.Controllers

{

	[Authorize]

	public class UploadRosterController : Controller

	{

		private readonly AppDBContext _context;

		private readonly ILogger<UploadRosterController> _logger;

		private readonly IWebHostEnvironment _hostingEnvironment;

		private const string controller_name = "UploadRoster";

		private const string title_name = "Upload Roster";



		// --- DTOs ---

		// (Kelas DTO RosterUploadDto, ValidationErrorDto, RosterPreviewDto, EmployeeInfoDto tetap sama)

		// ... existing DTOs ...

		private class RosterUploadDto

		{

			public int Row { get; set; }

			public string Nik { get; set; }

			public DateTime WorkingDate { get; set; }

			public string RosterCode { get; set; }

		}



		public class ValidationErrorDto

		{

			public int Row { get; set; }

			public string OffendingValue { get; set; }

			public string Message { get; set; }

		}



		public class RosterPreviewDto

		{

			public List<DateTime> Dates { get; set; } = new List<DateTime>();

			public List<EmployeeInfoDto> Employees { get; set; } = new List<EmployeeInfoDto>();

			public Dictionary<string, Dictionary<string, string>> RosterMap { get; set; } = new Dictionary<string, Dictionary<string, string>>();

			public string RosterDataJson { get; set; }

		}



		public class EmployeeInfoDto

		{

			public string Nik { get; set; }

			public string Nama { get; set; }

		}





		public UploadRosterController(AppDBContext context, ILogger<UploadRosterController> logger, IWebHostEnvironment hostingEnvironment)

		{

			_context = context;

			_logger = logger;

			_hostingEnvironment = hostingEnvironment;

		}



		// --- Action Index ---

		// (Action Index tetap sama)

		// ... existing Index action ...

		[HttpGet]

		public async Task<IActionResult> Index()

		{

			try

			{

				var kategoriUserId = HttpContext.Session.GetString("kategori_user_id");

				var compCode = HttpContext.Session.GetString("company");



				if (string.IsNullOrEmpty(kategoriUserId))

				{

					return RedirectToAction("Index", "Login");

				}



				var cekAkses = await _context.tbl_r_menu

				.AnyAsync(x => x.kategori_user_id == kategoriUserId &&

				(x.comp_code == null || x.comp_code == compCode) &&

				x.link_controller == controller_name);



				if (!cekAkses)

				{

					return RedirectToAction("Index", "Login");

				}



				var settingMenu = await _context.tbl_m_setting_menu.FirstOrDefaultAsync();

				ViewBag.SettingMenu = settingMenu;



				bool isUploadAllowed = false;

				if (settingMenu != null && settingMenu.awal.HasValue && settingMenu.akhir.HasValue)

				{

					var today = DateTime.Now.Date;

					isUploadAllowed = today >= settingMenu.awal.Value.Date && today <= settingMenu.akhir.Value.Date;

				}

				ViewBag.IsUploadAllowed = isUploadAllowed;



				var menuList = await _context.tbl_r_menu

				.Where(x => x.kategori_user_id == kategoriUserId)

				.Where(x => x.comp_code == null || x.comp_code == compCode)

				.OrderBy(x => x.type)

				.ThenBy(x => x.title)

				.ToListAsync();



				ViewBag.Title = title_name;

				ViewBag.Controller = controller_name;

				ViewBag.Setting = await _context.tbl_m_setting_aplikasi.FirstOrDefaultAsync();

				ViewBag.Menu = menuList;

				ViewBag.MenuMasterCount = menuList.Count(x => x.type == "Master");

				ViewBag.HROwnerCount = menuList.Count(x => x.type == "HROwner");



				ViewBag.AdminOwnerCount = menuList.Count(x => x.type == "AdminOwner");

				ViewBag.insert_by = HttpContext.Session.GetString("nrp");

				ViewBag.departemen = HttpContext.Session.GetString("dept");

				ViewBag.company = compCode;

				ViewBag.RosterKeterangan = await _context.tbl_m_roster_keterangan.OrderBy(k => k.kode).ToListAsync();



				return View();

			}

			catch (Exception ex)

			{

				_logger.LogError(ex, "Error saat memuat halaman Index.");

				return RedirectToAction("Index", "Login");

			}

		}





		// --- Action Upload ---

		// (Action Upload tetap sama)

		// ... existing Upload action ...

		[HttpPost]

		[ValidateAntiForgeryToken]

		public async Task<IActionResult> Upload(IFormFile file)

		{

			var settingMenu = await _context.tbl_m_setting_menu.FirstOrDefaultAsync();

			if (settingMenu == null || !settingMenu.awal.HasValue || !settingMenu.akhir.HasValue || !(DateTime.Now.Date >= settingMenu.awal.Value.Date && DateTime.Now.Date <= settingMenu.akhir.Value.Date))

			{

				TempData["ErrorMessage"] = "Periode upload roster saat ini sedang ditutup.";

				return RedirectToAction(nameof(Index));

			}



			if (file == null || file.Length == 0)

			{

				TempData["ErrorMessage"] = "File tidak boleh kosong!";

				return RedirectToAction(nameof(Index));

			}



			try

			{

				var (rosterData, parsingErrors) = await ExtractRosterDataFromExcel(file);

				if (parsingErrors.Any())

				{

					TempData["ErrorMessage"] = "Format file Excel tidak sesuai.";

					TempData["ErrorDetails"] = JsonSerializer.Serialize(parsingErrors);

					return RedirectToAction(nameof(Index));

				}

				if (!rosterData.Any())

				{

					TempData["ErrorMessage"] = "Tidak ada data yang dapat dibaca dari file Excel.";

					return RedirectToAction(nameof(Index));

				}



				var (validationErrors, validKaryawan) = await ValidateRosterDataAsync(rosterData);

				if (validationErrors.Any())

				{

					TempData["ErrorMessage"] = $"Ditemukan {validationErrors.Count} data tidak valid yang perlu diperbaiki.";

					TempData["ErrorDetails"] = JsonSerializer.Serialize(validationErrors);

					return RedirectToAction(nameof(Index));

				}



				var previewData = new RosterPreviewDto

				{

					Dates = rosterData.Select(d => d.WorkingDate).Distinct().OrderBy(d => d).ToList(),

					Employees = validKaryawan.Select(kvp => new EmployeeInfoDto { Nik = kvp.Key, Nama = kvp.Value }).OrderBy(e => e.Nik).ToList(),

					// Filter data SEBELUM serialisasi untuk JSON

					RosterDataJson = JsonSerializer.Serialize(rosterData.Where(d => validKaryawan.ContainsKey(d.Nik)).ToList())

				};



				// Populate RosterMap hanya dari data valid

				foreach (var data in rosterData.Where(d => validKaryawan.ContainsKey(d.Nik)))

				{

					if (!previewData.RosterMap.ContainsKey(data.Nik))

					{

						previewData.RosterMap[data.Nik] = new Dictionary<string, string>();

					}

					previewData.RosterMap[data.Nik][data.WorkingDate.ToString("yyyy-MM-dd")] = data.RosterCode;

				}





				TempData["PreviewData"] = JsonSerializer.Serialize(previewData);

			}

			catch (Exception ex)

			{

				_logger.LogError(ex, "Terjadi error tak terduga saat proses upload.");

				TempData["ErrorMessage"] = $"Terjadi kesalahan sistem: {ex.Message}.";

			}



			return RedirectToAction(nameof(Index));

		}



		// --- Action ConfirmAndSave ---

		// (Action ConfirmAndSave tetap sama)

		// ... existing ConfirmAndSave action ...

		[HttpPost]

		[ValidateAntiForgeryToken]

		public async Task<IActionResult> ConfirmAndSave(string rosterDataJson)

		{

			if (string.IsNullOrEmpty(rosterDataJson))

			{

				TempData["ErrorMessage"] = "Data pratinjau tidak ditemukan. Silakan unggah ulang.";

				return RedirectToAction(nameof(Index));

			}



			try

			{

				var rosterData = JsonSerializer.Deserialize<List<RosterUploadDto>>(rosterDataJson);

				var savedCount = await SaveRosterDataAsync(rosterData);

				TempData["SuccessMessage"] = $"{savedCount} data roster berhasil diproses dan disimpan.";

			}

			catch (Exception ex)

			{

				_logger.LogError(ex, "Gagal saat konfirmasi dan simpan roster.");

				TempData["ErrorMessage"] = $"Terjadi kesalahan sistem: {ex.Message}.";

			}



			return RedirectToAction(nameof(Index));

		}





		// --- Method ExtractRosterDataFromExcel ---

		// (Method ExtractRosterDataFromExcel tetap sama)

		// ... existing ExtractRosterDataFromExcel method ...

		private async Task<(List<RosterUploadDto> Data, List<ValidationErrorDto> Errors)> ExtractRosterDataFromExcel(IFormFile file)

		{

			var data = new List<RosterUploadDto>();

			var errors = new List<ValidationErrorDto>();



			using var stream = new MemoryStream();

			await file.CopyToAsync(stream);

			stream.Position = 0;



			IWorkbook workbook;

			string fileExt = Path.GetExtension(file.FileName).ToLower();

			if (fileExt == ".xlsx") workbook = new XSSFWorkbook(stream);

			else if (fileExt == ".xls") workbook = new HSSFWorkbook(stream);

			else

			{

				errors.Add(new ValidationErrorDto { Row = 0, OffendingValue = fileExt, Message = "Format file tidak didukung. Harap gunakan .xlsx atau .xls" });

				return (data, errors);

			}



			ISheet sheet = workbook.GetSheetAt(0);

			IRow headerRow = sheet.GetRow(0);

			if (headerRow == null)

			{

				errors.Add(new ValidationErrorDto { Row = 1, OffendingValue = "N/A", Message = "Baris header (baris pertama) tidak ditemukan." });

				return (data, errors);

			}



			var dates = new Dictionary<int, DateTime>();

			for (int col = 1; col < headerRow.LastCellNum; col++)

			{

				ICell cell = headerRow.GetCell(col);

				if (cell == null || cell.CellType == CellType.Blank || string.IsNullOrWhiteSpace(cell.ToString())) continue;



				if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))

				{

					DateTime cellDate = (DateTime)cell.DateCellValue;

					dates.Add(col, cellDate.Date); // Ambil hanya bagian tanggal

				}

				// Coba parsing manual jika format tidak dikenali NPOI

				else if (DateTime.TryParse(cell.ToString(), out DateTime dateValue))

				{

					dates.Add(col, dateValue.Date); // Ambil hanya bagian tanggal

				}

				else

				{

					errors.Add(new ValidationErrorDto { Row = 1, OffendingValue = cell.ToString(), Message = $"Format tanggal di header kolom ini tidak valid." });

				}

			}

			if (errors.Any()) return (data, errors);

			if (!dates.Any()) // Tambah cek jika tidak ada kolom tanggal valid

			{

				errors.Add(new ValidationErrorDto { Row = 1, OffendingValue = "Header Tanggal", Message = "Tidak ada kolom tanggal yang valid ditemukan di header." });

				return (data, errors);

			}





			for (int row = 1; row <= sheet.LastRowNum; row++)

			{

				IRow currentRow = sheet.GetRow(row);

				if (currentRow == null) continue;



				// Ambil NIK dan Nama (Nama ditambahkan untuk validasi nanti)

				var nik = currentRow.GetCell(0)?.ToString()?.Trim();

				// Jika NIK kosong, lewati baris ini

				if (string.IsNullOrEmpty(nik)) continue;





				foreach (var dateEntry in dates)

				{

					var rosterCode = currentRow.GetCell(dateEntry.Key)?.ToString()?.Trim();

					// Hanya tambahkan jika rosterCode tidak kosong (untuk mencegah data NULL)

					if (!string.IsNullOrEmpty(rosterCode))

					{

						data.Add(new RosterUploadDto { Row = row + 1, Nik = nik, WorkingDate = dateEntry.Value, RosterCode = rosterCode });

					}

					// Opsi: Tambahkan else jika Anda ingin menangani kode kosong secara eksplisit, misal sbg 'OFF'

					// else {

					//     data.Add(new RosterUploadDto { Row = row + 1, Nik = nik, WorkingDate = dateEntry.Value, RosterCode = "OFF" }); // Contoh

					// }

				}

			}



			return (data, errors);

		}





		// --- Method ValidateRosterDataAsync ---

		// (Method ValidateRosterDataAsync tetap sama)

		// ... existing ValidateRosterDataAsync method ...

		private async Task<(List<ValidationErrorDto> Errors, Dictionary<string, string> ValidKaryawan)> ValidateRosterDataAsync(List<RosterUploadDto> rosterData)

		{

			var errors = new List<ValidationErrorDto>();

			// Ambil NIK unik DARI DATA YANG DIUPLOAD (bukan dari database)

			var niksFromFile = rosterData.Select(d => d.Nik).Where(nik => !string.IsNullOrEmpty(nik)).Distinct().ToList();

			// Ambil Kode Roster unik DARI DATA YANG DIUPLOAD

			var codesFromFile = rosterData.Select(d => d.RosterCode).Where(code => !string.IsNullOrEmpty(code)).Distinct().ToList();



			// Cek NIK yang valid di database berdasarkan NIK dari file

			var validKaryawan = await _context.vw_m_karyawan_indexim

			.Where(k => niksFromFile.Contains(k.no_nik)) // Hanya cek NIK yang ada di file

			.Select(k => new { k.no_nik, k.nama_lengkap })

			.ToDictionaryAsync(k => k.no_nik, k => k.nama_lengkap);



			// Cek Kode Roster yang valid di database berdasarkan kode dari file

			var validRosterCodesDb = await _context.tbl_m_roster_keterangan

			.Where(r => codesFromFile.Contains(r.kode)) // Hanya cek kode yang ada di file

			.Select(r => r.kode)

			.ToHashSetAsync();



			// Gunakan HashSet untuk mencegah pesan error duplikat

			var uniqueErrors = new HashSet<string>();



			foreach (var data in rosterData)

			{

				// Hanya validasi jika NIK dan Kode Roster tidak kosong

				if (string.IsNullOrEmpty(data.Nik) || string.IsNullOrEmpty(data.RosterCode)) continue;





				// Cek NIK

				if (!validKaryawan.ContainsKey(data.Nik))

				{

					var errorKey = $"NIK-{data.Nik}";

					if (uniqueErrors.Add(errorKey)) // Jika error ini belum ditambahkan

					{

						errors.Add(new ValidationErrorDto { Row = data.Row, OffendingValue = data.Nik, Message = "NIK tidak terdaftar di sistem." });

					}

				}



				// Cek Kode Roster

				if (!validRosterCodesDb.Contains(data.RosterCode))

				{

					var errorKey = $"CODE-{data.RosterCode}";

					if (uniqueErrors.Add(errorKey)) // Jika error ini belum ditambahkan

					{

						errors.Add(new ValidationErrorDto { Row = data.Row, OffendingValue = data.RosterCode, Message = "Kode Roster tidak valid." });

					}

				}

				// Tambahkan validasi tanggal jika perlu (misal, tidak boleh tanggal default)

				if (data.WorkingDate == default(DateTime))

				{

					var errorKey = $"DATE-{data.Nik}-{data.Row}";

					if (uniqueErrors.Add(errorKey))

					{

						errors.Add(new ValidationErrorDto { Row = data.Row, OffendingValue = data.WorkingDate.ToString(), Message = "Tanggal kerja tidak valid." });

					}

				}



			}

			return (errors, validKaryawan);

		}





		// --- Method SaveRosterDataAsync (Diperbarui) ---

		private async Task<int> SaveRosterDataAsync(List<RosterUploadDto> rosterData)

		{

			// PERBAIKAN 1: Filter data tidak valid SEBELUM diproses

			var validRosterData = rosterData

			.Where(data => !string.IsNullOrEmpty(data.Nik) && data.WorkingDate != default(DateTime) && !string.IsNullOrEmpty(data.RosterCode))

			.ToList();



			if (!validRosterData.Any()) return 0; // Jika tidak ada data valid sama sekali



			var userIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

			const string remarksText = "Uploaded from Excel";



			using var transaction = await _context.Database.BeginTransactionAsync();

			try

			{

				var niksInFile = validRosterData.Select(d => d.Nik).Distinct().ToList();

				var minDate = validRosterData.Min(d => d.WorkingDate).Date; // Ambil .Date

				var maxDate = validRosterData.Max(d => d.WorkingDate).Date; // Ambil .Date



				// PERBAIKAN 2: Gunakan .Date saat memfilter data yang ada

				var existingData = await _context.tbl_m_roster_detail

				.Where(d => niksInFile.Contains(d.nik)

				&& d.working_date.HasValue // Pastikan tidak null

				&& d.working_date.Value.Date >= minDate // Bandingkan .Date

				&& d.working_date.Value.Date <= maxDate) // Bandingkan .Date

				.ToListAsync();



				// [REVISED FIX]: Use .Value explicitly when formatting the key for the dictionary

				// This ensures we are not accidentally calling ToString() on a nullable DateTime.

				// Since 'existingData' was filtered by 'HasValue', this is safe.

				var existingCreationDates = existingData

				.ToDictionary(d => $"{d.nik}:{d.working_date.Value:yyyy-MM-dd}", d => d.created_at);



				var rosterDetailsToInsert = new List<tbl_m_roster_detail>();

				foreach (var data in validRosterData) // Loop HANYA pada data yang valid

				{

					var key = $"{data.Nik}:{data.WorkingDate:yyyy-MM-dd}";

					var newDetail = new tbl_m_roster_detail

					{

						id = Guid.NewGuid().ToString(),

						nik = data.Nik,

						working_date = data.WorkingDate, // Simpan sebagai DateTime

						status = data.RosterCode,

						ip = userIpAddress,

						remarks = remarksText

					};



					if (existingCreationDates.TryGetValue(key, out var originalCreationDate))

					{

						newDetail.created_at = originalCreationDate ?? DateTime.Now; // Jaga created_at jika sudah ada

						newDetail.updated_at = DateTime.Now; // Set updated_at karena ini adalah update

					}

					else

					{

						newDetail.created_at = DateTime.Now; // Baru, set created_at

						newDetail.updated_at = null; // updated_at null untuk data baru

					}

					rosterDetailsToInsert.Add(newDetail);

				}



				if (existingData.Any())

				{

					_context.tbl_m_roster_detail.RemoveRange(existingData);

					// Tambahkan logging jika perlu:

					// _logger.LogInformation($"Menghapus {existingData.Count} data roster lama untuk NIK: {string.Join(",", niksInFile)} rentang {minDate:yyyy-MM-dd} - {maxDate:yyyy-MM-dd}");

				}



				await _context.tbl_m_roster_detail.AddRangeAsync(rosterDetailsToInsert);

				// Tambahkan logging jika perlu:

				// _logger.LogInformation($"Menambahkan {rosterDetailsToInsert.Count} data roster baru.");



				var processedCount = await _context.SaveChangesAsync();

				await transaction.CommitAsync();



				// Kembalikan jumlah data VALID yang diproses, bukan SaveChanges()

				return rosterDetailsToInsert.Count;

			}

			catch (Exception ex)

			{

				await transaction.RollbackAsync();

				_logger.LogError(ex, "Gagal saat menyimpan data roster.");

				throw; // Lemparkan lagi agar error bisa ditangkap di action ConfirmAndSave

			}

		}





		// --- Action DownloadTemplate ---

		// (Action DownloadTemplate tetap sama)

		// ... existing DownloadTemplate action ...

		[HttpGet]

		public IActionResult DownloadTemplate()

		{

			string fileName = "template.xlsx";

			string filePath = Path.Combine(_hostingEnvironment.WebRootPath, "template", fileName);



			if (!System.IO.File.Exists(filePath))

			{

				TempData["ErrorMessage"] = "File template tidak ditemukan di server.";

				return RedirectToAction(nameof(Index));

			}



			byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);

			return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);

		}



	}

}

