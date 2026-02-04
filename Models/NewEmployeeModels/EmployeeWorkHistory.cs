using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models.NewEmployeeModels
{
    [Table("tbl_employee_work_history")]
    public class EmployeeWorkHistory
    {
        [Key]
        [Column("id")]
        public Guid id { get; set; }

        [Column("employee_id")]
        public Guid employee_id { get; set; }

        [Column("company_code")]
        public string company_code { get; set; }

        [Column("dept_code")]
        public string dept_code { get; set; }

        [Column("position")]
        public string position { get; set; }

        [Column("start_date")]
        public DateTime start_date { get; set; }

        [Column("end_date")]
        public DateTime? end_date { get; set; }

        [Column("status_karyawan")]
        public string status_karyawan { get; set; }

        [Column("lokasi_terima_text")]
        public string lokasi_terima_text { get; set; }

        [Column("lokasi_kerja_text")]
        public string lokasi_kerja_text { get; set; }

        [Column("status_reciden")]
        public string status_reciden { get; set; }

        // Audit fields
        [Column("created_by")]
        public string created_by { get; set; }

        [Column("created_at")]
        public DateTime created_at { get; set; }

        [Column("updated_by")]
        public string updated_by { get; set; }

        [Column("updated_at")]
        public DateTime? updated_at { get; set; }

        [Column("is_active")]
        public bool is_active { get; set; } = true;
    }
}