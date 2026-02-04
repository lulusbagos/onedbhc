using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace one_db.Models
{
	[Table("tbl_r_comp")]
	public class tbl_r_comp
	{
		[Key]
		[Column("id")]
		public int id { get; set; }
		[Column("comp_code")]
		public string? comp_code { get; set; }
		[Column("company")]
		public string? company { get; set; }
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
