using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models.NewEmployeeModels
{
    [Table("tbl_employee")]
    public class Employee
    {
        [Key]
        [Column("id")]
        public Guid id { get; set; }

        [Column("indexim_id")]
        public string indexim_id { get; set; }

        [Column("nrp")]
        public string nrp { get; set; }

        [Column("nama_lengkap")]
        public string nama_lengkap { get; set; }

        [Column("tempat_lahir")]
        public string tempat_lahir { get; set; }

        [Column("tanggal_lahir")]
        public DateTime tanggal_lahir { get; set; }

        [Column("jenis_kelamin")]
        public string jenis_kelamin { get; set; }

        [Column("agama")]
        public string agama { get; set; }

        [Column("status_pernikahan")]
        public string status_pernikahan { get; set; }

        [Column("no_ktp")]
        public string no_ktp { get; set; }

        [Column("no_npwp")]
        public string no_npwp { get; set; }

        [Column("no_bpjs_kt")]
        public string no_bpjs_kt { get; set; }

        [Column("no_bpjs_kes")]
        public string no_bpjs_kes { get; set; }

        [Column("email")]
        public string email { get; set; }

        [Column("no_telp")]
        public string no_telp { get; set; }

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