using System.ComponentModel.DataAnnotations;

namespace one_db.Models
{
	// Data Transfer Object (DTO) untuk menangani
	// penambahan data referensi baru secara dinamis
	public class AddReferenceDto
	{
		[Required]
		public string? entity_type { get; set; } // Cth: "Posisi", "Bank", "Departemen"

		[Required]
		public string? nama_baru { get; set; }

		// Opsional, jika perlu data tambahan
		public string? data_tambahan_1 { get; set; }
		public string? data_tambahan_2 { get; set; }
	}
}
