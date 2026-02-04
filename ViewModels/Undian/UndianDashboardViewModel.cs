using System;
using System.Collections.Generic;

namespace one_db.ViewModels.Undian
{
	public class UndianDashboardViewModel
	{
		public int TotalParticipants { get; set; }
		public int TotalCoupons { get; set; }
		public int TotalPrizes { get; set; }
		public int TotalWinners { get; set; }
		public List<UndianWinnerItem> RecentWinners { get; set; } = new();
	}

	public class UndianWinnerItem
	{
		public string? NoNik { get; set; }
		public string? NamaPeserta { get; set; }
		public string? NamaHadiah { get; set; }
		public string? Periode { get; set; }
		public DateTime TanggalMenang { get; set; }
	}
}
