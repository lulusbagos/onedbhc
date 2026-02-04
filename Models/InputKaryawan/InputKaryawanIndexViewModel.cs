using System.Collections.Generic;

namespace one_db.Models.InputKaryawan
{
	public class InputKaryawanIndexViewModel
	{
		public List<tbl_r_company_level> CompanyLevels { get; set; } = new();
		public List<tbl_m_company> Companies { get; set; } = new();
		public List<tbl_m_karyawan_profile> RecentEmployees { get; set; } = new();
	}
}
