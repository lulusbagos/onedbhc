using System;
using System.ComponentModel.DataAnnotations;

namespace one_db.Models
{
	public class TravelAuthorizationRequest
	{
		public string? id { get; set; }

		[Required(ErrorMessage = "NIK wajib diisi")]
		[StringLength(50, ErrorMessage = "NIK maksimal 50 karakter")]
		public string? nik { get; set; }

		[Required(ErrorMessage = "Tanggal out site wajib diisi")]
		public DateTime? out_site { get; set; }

		[Required(ErrorMessage = "Tanggal on site wajib diisi")]
		public DateTime? on_site { get; set; }

		[StringLength(50)]
		public string? wilayah { get; set; }

		[StringLength(50)]
		public string? poh { get; set; }

		[StringLength(50)]
		public string? nominal { get; set; }

		[StringLength(50)]
		public string? nomor_ta { get; set; }
	}
}
