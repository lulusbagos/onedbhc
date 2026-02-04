using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
    [Table("tbl_r_kategori_user")]
    public class tbl_r_kategori_user
    {
        [Key]
        [Column("id")]
        public int id { get; set; }

        [Column("kategori")]
        public string? kategori { get; set; }

        [Column("login_controller")]
        public string? login_controller { get; set; }

        [Column("login_function")]
        public string? login_function { get; set; }

        [Column("insert_by")]
        public string? insert_by { get; set; }

        [Column("ip")]
        public string? ip { get; set; }

        [Column("created_at")]
        public DateTime? created_at { get; set; }

        [Column("updated_at")]
        public DateTime? updated_at { get; set; }
    }
}
