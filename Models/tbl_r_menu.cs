using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace one_db.Models
{
    [Table("tbl_r_menu")]
    public class tbl_r_menu
    {
        [Key]
        [Column("id")]
        public int id { get; set; }

        [Column("kategori_user_id")]
        public string? kategori_user_id { get; set; }

        [Column("title")]
        public string? title { get; set; }

        [Column("type")]
        public string? type { get; set; }
		[Column("link_controller")]
		public string? link_controller { get; set; }
		[Column("comp_code ")]
        public string? comp_code { get; set; }

        [Column("link_function")]
        public string? link_function { get; set; }

        [Column("hidden")]
        public string? hidden { get; set; }

        [Column("new_tab")]
        public string? new_tab { get; set; }

        [Column("insert_by")]
        public string? insert_by { get; set; }

        [Column("ip")]
        public string? ip { get; set; }

        [Column("created_at")]
        public string? created_at { get; set; }

        [Column("updated_at")]
        public string? updated_at { get; set; }
    }
}
