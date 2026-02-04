using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models.NewEmployeeModels
{
    [Table("tbl_employee_address")]
    public class EmployeeAddress
    {
        [Key]
        [Column("id")]
        public Guid id { get; set; }

        [Column("employee_id")]
        public Guid employee_id { get; set; }

        [Column("address_type")]  // KTP atau Domisili
        public string address_type { get; set; }

        [Column("address")]
        public string address { get; set; }

        [Column("rt")]
        public string rt { get; set; }

        [Column("rw")]
        public string rw { get; set; }

        [Column("kelurahan")]
        public string kelurahan { get; set; }

        [Column("kecamatan")]
        public string kecamatan { get; set; }

        [Column("kota")]
        public string kota { get; set; }

        [Column("provinsi")]
        public string provinsi { get; set; }

        [Column("kode_pos")]
        public string kode_pos { get; set; }

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