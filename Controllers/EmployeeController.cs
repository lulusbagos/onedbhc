using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using one_db.Data;
using one_db.Models.NewEmployeeModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace one_db.Controllers
{
    [Authorize]
    public class EmployeeController : Controller
    {
        private readonly AppDBContext _context;
        private readonly ILogger<EmployeeController> _logger;

        public EmployeeController(AppDBContext context, ILogger<EmployeeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Create employee with related lists (work histories, addresses, documents)
        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeDto dto)
        {
            if (dto == null)
                return BadRequest("Payload kosong");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // normalize / set IDs if missing
                var employee = dto.Employee ?? new Employee();
                if (employee.id == Guid.Empty) employee.id = Guid.NewGuid();
                employee.created_at = DateTime.UtcNow;
                employee.created_by ??= User.Identity?.Name ?? "system";

                // Save employee
                await _context.Employees.AddAsync(employee);
                await _context.SaveChangesAsync();

                // Work histories
                if (dto.WorkHistories != null && dto.WorkHistories.Any())
                {
                    foreach (var wh in dto.WorkHistories)
                    {
                        if (wh.id == Guid.Empty) wh.id = Guid.NewGuid();
                        wh.employee_id = employee.id; // link by id at app level
                        wh.created_at = DateTime.UtcNow;
                        wh.created_by ??= User.Identity?.Name ?? "system";
                    }
                    await _context.EmployeeWorkHistories.AddRangeAsync(dto.WorkHistories);
                }

                // Addresses
                if (dto.Addresses != null && dto.Addresses.Any())
                {
                    foreach (var a in dto.Addresses)
                    {
                        if (a.id == Guid.Empty) a.id = Guid.NewGuid();
                        a.employee_id = employee.id;
                        a.created_at = DateTime.UtcNow;
                        a.created_by ??= User.Identity?.Name ?? "system";
                    }
                    await _context.EmployeeAddresses.AddRangeAsync(dto.Addresses);
                }

                // Documents
                if (dto.Documents != null && dto.Documents.Any())
                {
                    foreach (var d in dto.Documents)
                    {
                        if (d.id == Guid.Empty) d.id = Guid.NewGuid();
                        d.employee_id = employee.id;
                        d.upload_date = d.upload_date == default ? DateTime.UtcNow : d.upload_date;
                        d.created_at = DateTime.UtcNow;
                        d.created_by ??= User.Identity?.Name ?? "system";
                    }
                    await _context.EmployeeDocuments.AddRangeAsync(dto.Documents);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, employee_id = employee.id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Gagal membuat employee");
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Get employee detail including related lists (no FK enforced)
        [HttpGet]
        public async Task<IActionResult> GetEmployee(Guid id)
        {
            if (id == Guid.Empty) return BadRequest("ID kosong");

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.id == id);
            if (employee == null) return NotFound();

            var workHistories = await _context.EmployeeWorkHistories.Where(w => w.employee_id == id).ToListAsync();
            var addresses = await _context.EmployeeAddresses.Where(a => a.employee_id == id).ToListAsync();
            var documents = await _context.EmployeeDocuments.Where(d => d.employee_id == id).ToListAsync();

            return Json(new { success = true, employee, workHistories, addresses, documents });
        }

        // Update (replace related lists if provided)
        [HttpPost]
        public async Task<IActionResult> UpdateEmployee([FromBody] UpdateEmployeeDto dto)
        {
            if (dto == null || dto.Employee == null) return BadRequest("Payload invalid");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var emp = await _context.Employees.FirstOrDefaultAsync(e => e.id == dto.Employee.id);
                if (emp == null) return NotFound("Employee tidak ditemukan");

                // Update simple fields (whitelist)
                emp.nama_lengkap = dto.Employee.nama_lengkap ?? emp.nama_lengkap;
                emp.nrp = dto.Employee.nrp ?? emp.nrp;
                emp.updated_at = DateTime.UtcNow;
                emp.updated_by = User.Identity?.Name ?? emp.updated_by;

                _context.Employees.Update(emp);

                // Replace work histories if provided: remove existing, add new
                if (dto.WorkHistories != null)
                {
                    var existingWh = _context.EmployeeWorkHistories.Where(w => w.employee_id == emp.id);
                    _context.EmployeeWorkHistories.RemoveRange(existingWh);

                    foreach (var wh in dto.WorkHistories)
                    {
                        if (wh.id == Guid.Empty) wh.id = Guid.NewGuid();
                        wh.employee_id = emp.id;
                        wh.created_at = DateTime.UtcNow;
                        wh.created_by ??= User.Identity?.Name ?? "system";
                    }
                    await _context.EmployeeWorkHistories.AddRangeAsync(dto.WorkHistories);
                }

                // Replace addresses if provided
                if (dto.Addresses != null)
                {
                    var existingAddr = _context.EmployeeAddresses.Where(a => a.employee_id == emp.id);
                    _context.EmployeeAddresses.RemoveRange(existingAddr);

                    foreach (var a in dto.Addresses)
                    {
                        if (a.id == Guid.Empty) a.id = Guid.NewGuid();
                        a.employee_id = emp.id;
                        a.created_at = DateTime.UtcNow;
                        a.created_by ??= User.Identity?.Name ?? "system";
                    }
                    await _context.EmployeeAddresses.AddRangeAsync(dto.Addresses);
                }

                // Documents replacement if provided
                if (dto.Documents != null)
                {
                    var existingDoc = _context.EmployeeDocuments.Where(d => d.employee_id == emp.id);
                    _context.EmployeeDocuments.RemoveRange(existingDoc);

                    foreach (var d in dto.Documents)
                    {
                        if (d.id == Guid.Empty) d.id = Guid.NewGuid();
                        d.employee_id = emp.id;
                        d.upload_date = d.upload_date == default ? DateTime.UtcNow : d.upload_date;
                        d.created_at = DateTime.UtcNow;
                        d.created_by ??= User.Identity?.Name ?? "system";
                    }
                    await _context.EmployeeDocuments.AddRangeAsync(dto.Documents);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Gagal update employee");
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEmployee([FromBody] Guid id)
        {
            if (id == Guid.Empty) return BadRequest("ID kosong");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var emp = await _context.Employees.FirstOrDefaultAsync(e => e.id == id);
                if (emp == null) return NotFound();

                // Remove related
                var wh = _context.EmployeeWorkHistories.Where(w => w.employee_id == id);
                _context.EmployeeWorkHistories.RemoveRange(wh);
                var addr = _context.EmployeeAddresses.Where(a => a.employee_id == id);
                _context.EmployeeAddresses.RemoveRange(addr);
                var docs = _context.EmployeeDocuments.Where(d => d.employee_id == id);
                _context.EmployeeDocuments.RemoveRange(docs);

                _context.Employees.Remove(emp);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Gagal hapus employee");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

    // DTOs to keep controller surface small
    public class CreateEmployeeDto
    {
        public Employee Employee { get; set; }
        public List<EmployeeWorkHistory> WorkHistories { get; set; }
        public List<EmployeeAddress> Addresses { get; set; }
        public List<EmployeeDocument> Documents { get; set; }
    }

    public class UpdateEmployeeDto
    {
        public Employee Employee { get; set; }
        public List<EmployeeWorkHistory> WorkHistories { get; set; }
        public List<EmployeeAddress> Addresses { get; set; }
        public List<EmployeeDocument> Documents { get; set; }
    }
}
