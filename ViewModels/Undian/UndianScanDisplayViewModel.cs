namespace one_db.ViewModels.Undian
{
	public class UndianScanDisplayViewModel
	{
		public LatestScanViewModel? LatestScan { get; set; }
		public int TotalHadiah { get; set; }
		public int TotalPesertaHadir { get; set; }
    }

    public class LatestScanViewModel
    {
        public string NoNik { get; set; } = string.Empty;
        public string Nama { get; set; } = string.Empty;
        public string Departemen { get; set; } = string.Empty;
        public string Perusahaan { get; set; } = string.Empty;
        public DateTime ScannedAt { get; set; }
    }
}
