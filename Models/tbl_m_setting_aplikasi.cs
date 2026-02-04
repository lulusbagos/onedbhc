using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace one_db.Models
{
	[Table("tbl_m_setting_aplikasi", Schema = "dbo")]

	public class tbl_m_setting_aplikasi
    {
        [Key]
        [Column("id")]
        public int id { get; set; }

        [Column("nama")]
        public string? nama { get; set; }

        [Column("description")]
        public string? description { get; set; }

        [Column("icon")]
        public string? icon { get; set; }

		[Column("theme")]
		public string? theme { get; set; }

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
