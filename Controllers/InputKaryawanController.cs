using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using one_db.Data;
using one_db.Dto;
using one_db.Models;
using one_db.Models.InputKaryawan;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace one_db.Controllers
{
    [Authorize]
    public class InputKaryawanController : Controller
    {
        private readonly AppDBContext _context;
        private readonly ILogger<InputKaryawanController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };
        private readonly Random _random = new Random();
        private readonly IHttpClientFactory _httpClientFactory;

        public InputKaryawanController(
            AppDBContext context,
            ILogger<InputKaryawanController> logger,
            IWebHostEnvironment environment,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
            _httpClientFactory = httpClientFactory;
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

				ViewBag.Title = "InputKaryawan";
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

		[HttpGet]
        public async Task<IActionResult> Lookup(string entity, string? q, string? parentId)
        {
            if (string.IsNullOrWhiteSpace(entity))
                return Json(new { success = false, message = "Entity wajib diisi." });

            var term = (q ?? string.Empty).Trim().ToLower();
            var results = new List<object>();

            switch (entity.ToLower())
            {
                case "perusahaan":
                    results = await _context.tbl_r_mitra_pengajuan
                        .Where(x => x.status_pengajuan == "Disetujui")
                        .Where(x => string.IsNullOrEmpty(term) || (x.nama_pt ?? string.Empty).ToLower().Contains(term))
                        .OrderBy(x => x.nama_pt)
                        .Select(x => new { id = x.id.ToString(), text = x.nama_pt })
                        .ToListAsync<object>(); // Cast to object
                    break;
                case "departemen":
                    results = await _context.tbl_r_dept
                        .Where(x => string.IsNullOrEmpty(term) || (x.departemen ?? string.Empty).ToLower().Contains(term))
                        .OrderBy(x => x.departemen)
                        .Select(x => new { id = x.id.ToString(), text = x.departemen })
                        .ToListAsync<object>(); // Cast to object
                    break;
                case "section":
                    if (!int.TryParse(parentId, out var deptId))
                        return Json(new { success = true, data = results });
                    results = await _context.tbl_r_section
                        .Where(x => x.dept_id == deptId)
                        .Where(x => string.IsNullOrEmpty(term) || (x.nama_section ?? string.Empty).ToLower().Contains(term))
                        .OrderBy(x => x.nama_section)
                        .Select(x => new { id = x.id.ToString(), text = x.nama_section })
                        .ToListAsync<object>(); // Cast to object
                    break;
                case "posisi":
                    if (!int.TryParse(parentId, out var sectionId))
                        return Json(new { success = true, data = results });
                    results = await _context.tbl_r_position
                        .Where(x => x.section_id == sectionId)
                        .Where(x => string.IsNullOrEmpty(term) || (x.nama_position ?? string.Empty).ToLower().Contains(term))
                        .OrderBy(x => x.nama_position)
                        .Select(x => new { id = x.id.ToString(), text = x.nama_position })
                        .ToListAsync<object>(); // Cast to object
                    break;
                case "provinsi":
                    results = await FetchWilayahAsync("https://wilayah.id/api/provinces.json", term);
                    break;
                case "kabupaten":
                case "kota":
                    if (string.IsNullOrWhiteSpace(parentId))
                        break;
                    results = await FetchWilayahAsync($"https://wilayah.id/api/regencies/{parentId}.json", term);
                    break;
                case "kecamatan":
                    if (string.IsNullOrWhiteSpace(parentId))
                        break;
                    results = await FetchWilayahAsync($"https://wilayah.id/api/districts/{parentId}.json", term);
                    break;
                case "kelurahan":
                case "desa":
                    if (string.IsNullOrWhiteSpace(parentId))
                        break;
                    results = await FetchWilayahAsync($"https://wilayah.id/api/villages/{parentId}.json", term);
                    break;
                default:
                    break;
            }

            return Json(new { success = true, data = results });
        }

        [HttpGet]
        public async Task<IActionResult> GenerateIndeximId(string? nik)
        {
            var id = await ResolveEmployeeIndeximIdAsync(nik, reuseExisting: true);
            if (string.IsNullOrEmpty(id))
                return Json(new { success = false, message = "Tidak dapat menghasilkan ID baru." });

            return Json(new { success = true, data = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInvite([FromBody] CreateInviteRequest request)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Data invite tidak valid." });

            var company = await _context.tbl_m_company.FirstOrDefaultAsync(c => c.id == request.company_id);
            if (company == null)
                return Json(new { success = false, message = "Perusahaan tidak ditemukan." });

            var token = Guid.NewGuid().ToString("N");
            var (_, _, nrp, _, _) = ResolveUserContext();

            var entity = new tbl_t_karyawan_invite
            {
                id = Guid.NewGuid(),
                invite_token = token,
                company_id = company.id,
                company_level = company.kode_level,
                recipient_email = request.email,
                nik = request.nik,
                nama_lengkap = request.nama_lengkap,
                expires_at = request.expires_at,
                created_by = nrp
            };

            _context.tbl_t_karyawan_invite.Add(entity);
            await _context.SaveChangesAsync();

            var link = Url.Action("Index", "InputKaryawan", new { invite = token }, Request.Scheme);
            return Json(new { success = true, data = new { token, link } });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitWizard([FromForm] string payload_json)
        {
            if (string.IsNullOrWhiteSpace(payload_json))
                return Json(new { success = false, message = "Payload kosong." });

            InputKaryawanWizardPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<InputKaryawanWizardPayload>(payload_json, _jsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Payload tidak valid.");
                return Json(new { success = false, message = "Format payload tidak valid." });
            }

            if (payload?.Profile == null)
                return Json(new { success = false, message = "Data profil wajib diisi." });

            var normalizedNik = NormalizeNik(payload.Profile.no_identitas ?? payload.Job?.nik);
            if (string.IsNullOrWhiteSpace(normalizedNik))
                return Json(new { success = false, message = "Nomor identitas (KTP/Paspor) wajib diisi untuk membuat ID karyawan." });

            var existingByNik = await _context.tbl_m_karyawan_profile.AsNoTracking()
                .FirstOrDefaultAsync(x => x.nik == normalizedNik || x.no_identitas == normalizedNik);
            if (existingByNik != null)
            {
                return Json(new
                {
                    success = false,
                    message = $"Karyawan dengan identitas {normalizedNik} sudah memiliki ID {existingByNik.indexim_id}. Gunakan data tersebut untuk pembaruan."
                });
            }

            var generatedIndex = await ResolveEmployeeIndeximIdAsync(normalizedNik, reuseExisting: false);
            payload.Profile.indexim_id = generatedIndex;
            if (string.IsNullOrWhiteSpace(payload.Profile.no_identitas))
                payload.Profile.no_identitas = normalizedNik;

            var jobStatusReciden = payload.Job?.status_reciden ?? payload.Profile.status_reciden;
            payload.Job ??= new JobPayload();
            payload.Job.nik = normalizedNik;
            payload.Job.status_reciden = jobStatusReciden;

            tbl_t_karyawan_invite? inviteEntity = null;
            if (!string.IsNullOrWhiteSpace(payload.InviteToken))
            {
                inviteEntity = await _context.tbl_t_karyawan_invite.FirstOrDefaultAsync(i => i.invite_token == payload.InviteToken);
            }

            Guid? targetCompanyId = null;
            if (Guid.TryParse(payload.Job.perusahaan_id, out var parsedCompany))
            {
                targetCompanyId = parsedCompany;
            }
            else if (inviteEntity?.company_id != null)
            {
                targetCompanyId = inviteEntity.company_id;
            }
            var hierarchy = await ResolveCompanyHierarchy(targetCompanyId);
            var companyLevel = (hierarchy.CurrentLevel ??
                payload.Job.job_level ??
                inviteEntity?.company_level ??
                "OWNER").ToUpperInvariant();

            var (_, _, nrp, _, _) = ResolveUserContext();
            var approvalSnapshot = GetInitialApprovalSnapshot(companyLevel, nrp);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var employee = new tbl_m_karyawan_profile
                {
                    id = Guid.NewGuid(),
                    indexim_id = payload.Profile.indexim_id!,
                    nrp = payload.Profile.indexim_id!,
                    nik = payload.Job.nik,
                    kewarganegaraan = payload.Profile.kewarganegaraan,
                    no_identitas = payload.Profile.no_identitas,
                    no_kk = payload.Profile.no_kk,
                    nama_lengkap = payload.Profile.nama_lengkap ?? string.Empty,
                    tempat_lahir = payload.Profile.tempat_lahir,
                    tanggal_lahir = payload.Profile.tanggal_lahir,
                    jenis_kelamin = payload.Profile.jenis_kelamin,
                    gol_darah = payload.Profile.gol_darah,
                    agama = payload.Profile.agama,
                    jabatan = string.IsNullOrWhiteSpace(payload.Job.posisi_text) ? null : payload.Job.posisi_text,
                    status_residence_id = payload.Job.status_residence ?? payload.Profile.status_residence_id,
                    status_reciden = payload.Job.status_reciden,
                    no_telp_pribadi = payload.Profile.no_telp_pribadi,
                    nomor_hp = payload.Profile.no_telp_pribadi,
                    email_perusahaan = payload.Profile.email_perusahaan,
                    email_pribadi = payload.Profile.email_pribadi,
                    email = payload.Profile.email_perusahaan ?? payload.Profile.email_pribadi,
                    no_bpjskes = payload.Profile.no_bpjskes,
                    no_bpjstk = payload.Profile.no_bpjstk,
                    no_npwp = payload.Profile.no_npwp,
                    nama_ibu_kandung = payload.Profile.nama_ibu_kandung,
                    status_ibu = payload.Profile.status_ibu,
                    nama_ayah_kandung = payload.Profile.nama_ayah_kandung,
                    status_ayah = payload.Profile.status_ayah,
                    owner_company_id = hierarchy.OwnerId,
                    main_contractor_company_id = hierarchy.MainId,
                    sub_contractor_company_id = hierarchy.SubId,
                    vendor_company_id = hierarchy.VendorId,
                    submitted_company_id = targetCompanyId,
                    invite_id = inviteEntity?.id,
                    approval_status = approvalSnapshot.Status,
                    company_approved_at = approvalSnapshot.CompanyApprovedAt,
                    company_approved_by = approvalSnapshot.CompanyApprovedBy,
                    main_approved_at = approvalSnapshot.MainApprovedAt,
                    main_approved_by = approvalSnapshot.MainApprovedBy,
                    is_active = approvalSnapshot.Status == "Approved",
                    finalized_at = approvalSnapshot.IsFinal ? DateTime.UtcNow : null,
                    tanggal_masuk = payload.Job.tanggal_aktif,
                    tanggal_selesai = payload.Job.tanggal_resign,
                    created_by = nrp,
                    catatan = string.IsNullOrWhiteSpace(payload.Job.perusahaan_text) ? null : $"Perusahaan pengajuan: {payload.Job.perusahaan_text}"
                };

                _context.tbl_m_karyawan_profile.Add(employee);
                await _context.SaveChangesAsync();

                if (payload.Job != null)
                {
                    _context.tbl_t_karyawan_pekerjaan_history.Add(new tbl_t_karyawan_pekerjaan_history
                    {
                        master_karyawan_id = employee.id,
                        perusahaan_id = targetCompanyId,
                        departemen_id = payload.Job.departemen_id,
                        section_id = payload.Job.section_id,
                        posisi_id = payload.Job.posisi_id,
                        job_level = payload.Job.job_level ?? companyLevel,
                        job_grade = payload.Job.job_grade,
                        roster_code = payload.Job.roster_code,
                        nik = payload.Job.nik,
                        doh = payload.Job.doh,
                        poh = payload.Job.poh,
                        tanggal_aktif = payload.Job.tanggal_aktif,
                        tanggal_resign = payload.Job.tanggal_resign,
                        lokasi_kerja_kode = payload.Job.lokasi_kerja_kode,
                        lokasi_kerja_text = payload.Job.lokasi_kerja_text,
                        lokasi_terima_text = payload.Job.lokasi_terima_text,
                        status_residence = payload.Job.status_residence ?? payload.Profile.status_residence_id,
                        status_reciden = payload.Job.status_reciden,
                        created_by = nrp
                    });
                }

                foreach (var assignment in hierarchy.GetAssignments())
                {
                    _context.tbl_t_karyawan_company.Add(new tbl_t_karyawan_company
                    {
                        id = Guid.NewGuid(),
                        master_karyawan_id = employee.id,
                        company_id = assignment.CompanyId,
                        role_level = assignment.RoleLevel,
                        start_date = payload.Job.tanggal_aktif ?? DateTime.UtcNow.Date,
                        keterangan = "Auto assignment"
                    });
                }

                if (payload.Addresses != null)
                {
                    foreach (var addr in payload.Addresses.Where(a => !string.IsNullOrWhiteSpace(a?.alamat)))
                    {
                        _context.tbl_t_karyawan_alamat_history.Add(new tbl_t_karyawan_alamat_history
                        {
                            master_karyawan_id = employee.id,
                            jenis_alamat = addr?.jenis_alamat ?? "KTP",
                            alamat = addr?.alamat,
                            rt = addr?.rt,
                            rw = addr?.rw,
                            provinsi = addr?.provinsi,
                            kota = addr?.kota,
                            kecamatan = addr?.kecamatan,
                            kelurahan = addr?.kelurahan,
                            kode_pos = addr?.kode_pos,
                            created_by = nrp
                        });
                    }
                }

                if (payload.Bank != null)
                {
                    _context.tbl_t_karyawan_bank_history.Add(new tbl_t_karyawan_bank_history
                    {
                        master_karyawan_id = employee.id,
                        nama_bank = payload.Bank.bank_name,
                        nama_pemilik_rekening = payload.Bank.nama_pemilik_rekening,
                        nomor_rekening = payload.Bank.nomor_rekening,
                        created_by = nrp
                    });
                }

                if (payload.EmergencyContact != null)
                {
                    _context.tbl_t_karyawan_emergency_history.Add(new tbl_t_karyawan_emergency_history
                    {
                        master_karyawan_id = employee.id,
                        nama_kontak_darurat = payload.EmergencyContact.nama,
                        relasi_kontak_darurat = payload.EmergencyContact.relasi,
                        hp_kontak_darurat_1 = payload.EmergencyContact.hp1,
                        hp_kontak_darurat_2 = payload.EmergencyContact.hp2,
                        created_by = nrp
                    });
                }

                if (payload.Families != null)
                {
                    foreach (var fam in payload.Families)
                    {
                        _context.tbl_t_karyawan_keluarga_history.Add(new tbl_t_karyawan_keluarga_history
                        {
                            master_karyawan_id = employee.id,
                            hubungan = fam.hubungan,
                            nama_lengkap = fam.nama,
                            tanggal_lahir = fam.tanggal_lahir,
                            pekerjaan = fam.pekerjaan,
                            nomor_hp = fam.nomor_hp,
                            is_tanggungan = fam.is_tanggungan,
                            created_by = nrp
                        });
                    }
                }

                if (payload.Educations != null)
                {
                    foreach (var edu in payload.Educations)
                    {
                        var entity = new tbl_t_karyawan_pendidikan_history
                        {
                            master_karyawan_id = employee.id,
                            pendidikan_id = edu.pendidikan_id,
                            nama_instansi = edu.nama_instansi,
                            tahun_masuk = edu.tahun_masuk,
                            tahun_lulus = edu.tahun_lulus,
                            created_by = nrp
                        };

                        if (!string.IsNullOrEmpty(edu.file_field))
                        {
                            var file = Request.Form.Files.FirstOrDefault(f => f.Name == edu.file_field);
                            if (file != null && file.Length > 0)
                                entity.path_dokumen = await SaveFileAsync(file, employee.id, "PED");
                        }

                        _context.tbl_t_karyawan_pendidikan_history.Add(entity);
                    }
                }

                if (payload.Certifications != null)
                {
                    foreach (var sert in payload.Certifications)
                    {
                        var entity = new tbl_t_karyawan_sertifikasi_history
                        {
                            master_karyawan_id = employee.id,
                            nama_sertifikasi = sert.nama_sertifikasi,
                            lembaga_penerbit = sert.lembaga,
                            nomor_sertifikat = sert.nomor_sertifikat,
                            tanggal_terbit = sert.tanggal_terbit,
                            tanggal_kadaluarsa = sert.tanggal_kadaluarsa,
                            created_by = nrp
                        };

                        if (!string.IsNullOrEmpty(sert.file_field))
                        {
                            var file = Request.Form.Files.FirstOrDefault(f => f.Name == sert.file_field);
                            if (file != null && file.Length > 0)
                                entity.path_file_sertifikat = await SaveFileAsync(file, employee.id, "SERT");
                        }

                        _context.tbl_t_karyawan_sertifikasi_history.Add(entity);
                    }
                }

                if (payload.Mcus != null)
                {
                    foreach (var mcu in payload.Mcus)
                    {
                        var entity = new tbl_t_karyawan_mcu_history
                        {
                            master_karyawan_id = employee.id,
                            hasil_mcu = mcu.hasil,
                            fasilitas_kesehatan = mcu.fasilitas_kesehatan,
                            tanggal_mcu = mcu.tanggal,
                            created_by = nrp
                        };

                        if (!string.IsNullOrEmpty(mcu.file_field))
                        {
                            var file = Request.Form.Files.FirstOrDefault(f => f.Name == mcu.file_field);
                            if (file != null && file.Length > 0)
                                entity.path_file_mcu = await SaveFileAsync(file, employee.id, "MCU");
                        }

                        _context.tbl_t_karyawan_mcu_history.Add(entity);
                    }
                }

                if (payload.Vaccines != null)
                {
                    foreach (var vak in payload.Vaccines)
                    {
                        _context.tbl_t_karyawan_vaksin_history.Add(new tbl_t_karyawan_vaksin_history
                        {
                            master_karyawan_id = employee.id,
                            jenis_vaksin = vak.jenis_vaksin,
                            dosis_ke = vak.dosis_ke,
                            tanggal_vaksin = vak.tanggal_vaksin,
                            fasilitas_kesehatan = vak.fasilitas_kesehatan,
                            keterangan = vak.catatan,
                            created_by = nrp
                        });
                    }
                }

                if (payload.Documents != null)
                {
                    foreach (var doc in payload.Documents)
                    {
                        if (string.IsNullOrEmpty(doc.input_name))
                            continue;

                        var file = Request.Form.Files.FirstOrDefault(f => f.Name == doc.input_name);
                        if (file == null || file.Length == 0)
                            continue;

                        var relativePath = await SaveFileAsync(file, employee.id, doc.kode_dokumen ?? doc.input_name);
                        _context.tbl_t_karyawan_dokumen_history.Add(new tbl_t_karyawan_dokumen_history
                        {
                            master_karyawan_id = employee.id,
                            kode_dokumen = doc.kode_dokumen,
                            nama_dokumen = doc.nama_dokumen ?? file.FileName,
                            path_file = relativePath,
                            created_by = nrp
                        });
                    }
                }

                if (inviteEntity != null)
                {
                    inviteEntity.master_karyawan_id = employee.id;
                    inviteEntity.status = "Completed";
                    inviteEntity.completed_at = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Data karyawan berhasil disimpan.", data = new { employee.id, employee.indexim_id } });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Gagal menyimpan data karyawan.");
                return Json(new { success = false, message = "Gagal menyimpan data: " + ex.Message });
            }
        }

        // BLOK DUPLIKAT YANG BERMASALAH (BARIS 592-678) TELAH DIHAPUS DARI SINI

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveCompany(Guid id)
        {
            var employee = await _context.tbl_m_karyawan_profile.FirstOrDefaultAsync(x => x.id == id);
            if (employee == null)
                return Json(new { success = false, message = "Data karyawan tidak ditemukan." });

            if (employee.approval_status != "PendingCompanyApproval")
                return Json(new { success = false, message = "Status tidak valid untuk approval perusahaan." });

            var (_, _, nrp, _, _) = ResolveUserContext();
            employee.approval_status = employee.main_contractor_company_id.HasValue ? "PendingMainApproval" : "Approved";
            employee.company_approved_at = DateTime.UtcNow;
            employee.company_approved_by = nrp;
            if (employee.approval_status == "Approved")
            {
                employee.main_approved_at = DateTime.UtcNow;
                employee.main_approved_by = nrp;
                employee.is_active = true;
                employee.finalized_at = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Approval perusahaan berhasil direkam.", data = new { employee.approval_status } });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveMain(Guid id)
        {
            var employee = await _context.tbl_m_karyawan_profile.FirstOrDefaultAsync(x => x.id == id);
            if (employee == null)
                return Json(new { success = false, message = "Data karyawan tidak ditemukan." });

            if (employee.approval_status != "PendingMainApproval")
                return Json(new { success = false, message = "Status tidak valid untuk approval main contractor." });

            var (_, _, nrp, _, _) = ResolveUserContext();
            employee.approval_status = "Approved";
            employee.main_approved_at = DateTime.UtcNow;
            employee.main_approved_by = nrp;
            employee.is_active = true;
            employee.finalized_at = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Approval main contractor berhasil direkam." });
        }

        private List<DocumentRequirement> GetDefaultDocumentRequirements()
        {
            return new List<DocumentRequirement>
            {
                new DocumentRequirement("file_pasfoto", "PAS_PHOTO", "Pas Foto", "image"),
                new DocumentRequirement("file_ktp", "KTP", "Scan KTP", "pdf"),
                new DocumentRequirement("file_kk", "KK", "Kartu Keluarga", "pdf"),
                new DocumentRequirement("file_cv", "CV", "Curriculum Vitae", "pdf"),
                new DocumentRequirement("file_ijazah", "IJAZAH", "Ijazah Pendidikan", "pdf"),
                new DocumentRequirement("file_skck", "SKCK", "SKCK", "pdf"),
                new DocumentRequirement("file_pkwt", "PKWT", "Perjanjian PKWT", "pdf"),
                new DocumentRequirement("file_pkwtt", "PKWTT", "Perjanjian PKWTT", "pdf"),
                new DocumentRequirement("file_sk_promosi", "SK_PROMOSI", "SK Promosi", "pdf"),
                new DocumentRequirement("file_sk_mutasi", "SK_MUTASI", "SK Mutasi", "pdf")
            };
        }

        private record DocumentRequirement(string InputName, string KodeDokumen, string NamaDokumen, string FileType);

        private async Task<CompanyHierarchy> ResolveCompanyHierarchy(Guid? companyId)
        {
            var hierarchy = new CompanyHierarchy();
            if (!companyId.HasValue)
                return hierarchy;

            var visited = new HashSet<Guid>();
            Guid? currentId = companyId;
            while (currentId.HasValue && !visited.Contains(currentId.Value))
            {
                visited.Add(currentId.Value);
                var entity = await _context.tbl_m_company.AsNoTracking().FirstOrDefaultAsync(c => c.id == currentId.Value);
                if (entity == null)
                    break;

                var level = (entity.kode_level ?? string.Empty).ToUpperInvariant();
                hierarchy.CurrentLevel ??= level;
                switch (level)
                {
                    case "OWNER":
                        hierarchy.OwnerId ??= entity.id;
                        currentId = entity.parent_company_id;
                        break;
                    case "MAIN_CONTRACTOR":
                        hierarchy.MainId ??= entity.id;
                        currentId = entity.parent_company_id;
                        break;
                    case "SUB_CONTRACTOR":
                        hierarchy.SubId ??= entity.id;
                        currentId = entity.parent_company_id;
                        break;
                    case "VENDOR":
                        hierarchy.VendorId ??= entity.id;
                        currentId = entity.parent_company_id;
                        break;
                    default:
                        currentId = entity.parent_company_id;
                        break;
                }
            }

            return hierarchy;
        }

        private ApprovalSnapshot GetInitialApprovalSnapshot(string companyLevel, string? approver)
        {
            var now = DateTime.UtcNow;
            return companyLevel switch
            {
                "OWNER" => new ApprovalSnapshot("Approved", now, approver, now, approver, true),
                "MAIN_CONTRACTOR" => new ApprovalSnapshot("Approved", now, approver, now, approver, true),
                _ => new ApprovalSnapshot("PendingCompanyApproval", null, null, null, null, false)
            };
        }

        private record ApprovalSnapshot(
            string Status,
            DateTime? CompanyApprovedAt,
            string? CompanyApprovedBy,
            DateTime? MainApprovedAt,
            string? MainApprovedBy,
            bool IsFinal);

        private class CompanyHierarchy
        {
            public Guid? OwnerId { get; set; }
            public Guid? MainId { get; set; }
            public Guid? SubId { get; set; }
            public Guid? VendorId { get; set; }
            public string? CurrentLevel { get; set; }
            public bool HasMain => MainId.HasValue;

            public IEnumerable<(Guid CompanyId, string RoleLevel)> GetAssignments()
            {
                if (OwnerId.HasValue) yield return (OwnerId.Value, "OWNER");
                if (MainId.HasValue) yield return (MainId.Value, "MAIN_CONTRACTOR");
                if (SubId.HasValue) yield return (SubId.Value, "SUB_CONTRACTOR");
                if (VendorId.HasValue) yield return (VendorId.Value, "VENDOR");
            }
        }

        private async Task<string> SaveFileAsync(IFormFile file, Guid employeeId, string prefix)
        {
            var folder = Path.Combine(_environment.WebRootPath, "uploads", "karyawan", employeeId.ToString("N"));
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var safeFileName = prefix + "_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "_" + Path.GetFileName(file.FileName);
            var fullPath = Path.Combine(folder, safeFileName);
            using (var stream = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(stream);
            }
            var relative = Path.Combine("uploads", "karyawan", employeeId.ToString("N"), safeFileName)
                .Replace("\\", "/");
            return relative;
        }

        private async Task<List<object>> FetchWilayahAsync(string url, string term)
        {
            var result = new List<object>();
            try
            {
                var client = _httpClientFactory.CreateClient();
                using var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                await using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                        var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                            continue;
                        if (!string.IsNullOrEmpty(term) && !name.ToLowerInvariant().Contains(term))
                            continue;
                        result.Add(new { id, text = name });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gagal mengambil data wilayah dari {Url}", url);
            }

            return result;
        }

        private async Task<string> ResolveEmployeeIndeximIdAsync(string? nikOrIdentifier, bool reuseExisting)
        {
            var normalized = NormalizeNik(nikOrIdentifier);
            if (!string.IsNullOrEmpty(normalized))
            {
                if (reuseExisting)
                {
                    var existing = await _context.tbl_m_karyawan_profile.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.nik == normalized || x.no_identitas == normalized);
                    if (existing != null)
                        return existing.indexim_id;
                }

                var candidate = $"EK{normalized}";
                if (candidate.Length > 30)
                    candidate = candidate.Substring(0, 30);
                var exists = await _context.tbl_m_karyawan_profile.AnyAsync(x => x.indexim_id == candidate);
                if (!exists)
                    return candidate;
            }

            return await GenerateSequentialIndexAsync();
        }

        private async Task<string> GenerateSequentialIndexAsync()
        {
            for (int i = 0; i < 15; i++)
            {
                var guidSegment = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpperInvariant();
                var candidate = $"IC{DateTime.UtcNow:yyyyMMddHHmm}{_random.Next(100, 999)}{guidSegment}";
                var exists = await _context.tbl_m_karyawan_profile.AnyAsync(x => x.indexim_id == candidate);
                if (!exists)
                    return candidate;
            }

            // Fallback: shortened GUID to guarantee uniqueness even under heavy concurrency
            return $"IC{Guid.NewGuid():N}".Substring(0, 14).ToUpperInvariant();
        }

        private static string NormalizeNik(string? nik)
        {
            if (string.IsNullOrWhiteSpace(nik))
                return string.Empty;
            var alphanumeric = new string(nik.Where(char.IsLetterOrDigit).ToArray());
            if (string.IsNullOrWhiteSpace(alphanumeric))
                return string.Empty;

            var hasLetter = alphanumeric.Any(char.IsLetter);
            if (!hasLetter)
                return alphanumeric;

            return alphanumeric.ToUpperInvariant();
        }

        private (string? kategoriUserId, string? compCode, string? nrp, string? dept, string? nama) ResolveUserContext()
        {
            var kategoriUserId = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            var compCode = User.FindFirst("comp_code")?.Value;
            var nrp = User.Identity?.Name;
            var dept = User.FindFirst("dept_code")?.Value;
            var nama = User.FindFirst("nama")?.Value;

            kategoriUserId ??= HttpContext.Session.GetString("kategori_user_id");
            compCode ??= HttpContext.Session.GetString("company");
            nrp ??= HttpContext.Session.GetString("nrp");
            dept ??= HttpContext.Session.GetString("dept");
            nama ??= HttpContext.Session.GetString("nama");

            return (kategoriUserId, compCode, nrp, dept, nama);
        }
    }
}
