using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models.NewEmployeeModels
{
    [Table("tbl_employee_document")]
    public class EmployeeDocument
    {
        [Key]
        [Column("id")]
        public Guid id { get; set; }

        [Column("employee_id")]
        public Guid employee_id { get; set; }

        [Column("document_type")]
        public string document_type { get; set; }

        [Column("document_number")]
        public string document_number { get; set; }

        [Column("file_path")]
        public string file_path { get; set; }

        [Column("file_name")]
        public string file_name { get; set; }

        [Column("upload_date")]
        public DateTime upload_date { get; set; }

        [Column("expiry_date")]
        public DateTime? expiry_date { get; set; }

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