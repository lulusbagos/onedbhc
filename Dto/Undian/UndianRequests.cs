using System;
using System.ComponentModel.DataAnnotations;

namespace one_db.Dto.Undian
{
	public class GenerateCouponRequest
	{
		[Required]
		[StringLength(50)]
		public string NoNik { get; set; } = string.Empty;

		[Range(1, 1000)]
		public int JumlahKupon { get; set; } = 1;

		[StringLength(20)]
		public string? Prefix { get; set; }

		[StringLength(50)]
		public string? Periode { get; set; }

		[StringLength(255)]
		public string? Keterangan { get; set; }
	}

	public class UpsertPrizeRequest
	{
		public Guid? Id { get; set; }

		[Required]
		[StringLength(200)]
		public string NamaHadiah { get; set; } = string.Empty;

		[StringLength(500)]
		public string? Deskripsi { get; set; }

		[StringLength(50)]
		public string Kategori { get; set; } = "doorprize";

		[Range(1, 1000)]
		public int Urutan { get; set; } = 1;

		[Range(1, 1000)]
		public int JumlahUnit { get; set; } = 1;

		public bool IsActive { get; set; } = true;
	}

	public class UpsertDrawRequest
	{
		public Guid? Id { get; set; }

		[Required]
		[StringLength(200)]
		public string NamaEvent { get; set; } = string.Empty;

		public DateTime? TanggalEvent { get; set; }

		[StringLength(500)]
		public string? Keterangan { get; set; }

		[StringLength(25)]
		public string Status { get; set; } = "draft";
	}

	public class ExecuteUndianRequest
	{
		[Required]
		public Guid PengundianId { get; set; }

		[Required]
		public Guid HadiahId { get; set; }

		[Range(1, 100)]
		public int JumlahPemenang { get; set; } = 1;

		[StringLength(50)]
		public string? Periode { get; set; }
	}

	public class ScanNikRequest
	{
		[Required]
		[StringLength(50)]
		public string NoNik { get; set; } = string.Empty;
	}
}
