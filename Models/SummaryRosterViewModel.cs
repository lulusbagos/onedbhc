using one_db.Models;
using System;
using System.Collections.Generic;

namespace one_db.Models.ViewModels
{
	// Model utama yang dikirim ke View (sebagai JSON)
	public class SummaryRosterViewModel
	{
		public List<DateTime> DateHeaders { get; set; } = new List<DateTime>();
		public List<EmployeeRosterRow> EmployeeRows { get; set; } = new List<EmployeeRosterRow>();
		public Dictionary<string, string> StatusColors { get; set; } = new Dictionary<string, string>();
		public List<tbl_m_roster_keterangan> Legend { get; set; } = new List<tbl_m_roster_keterangan>();
		public DateTime CurrentMonthYear { get; set; }
	}

	// Mewakili satu baris karyawan
	public class EmployeeRosterRow
	{
		public string NIK { get; set; }
		public string Nama { get; set; }
		public string Departemen { get; set; }
		public string Posisi { get; set; }
		public Dictionary<DateTime, string> RosterCells { get; set; } = new Dictionary<DateTime, string>();
	}
}