using Microsoft.EntityFrameworkCore;
using one_db.Models;

namespace one_db.Data
{
	public class MySqlDBContext : DbContext
	{
		public MySqlDBContext(DbContextOptions<MySqlDBContext> options)
			: base(options)
		{
		}

		public DbSet<vw_m_report_hr> vw_m_report_hr { get; set; }

		// 🆕 Tambahkan ini:
		public DbSet<CompanyFilter> CompanyFilters { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<vw_m_report_hr>().HasKey(x => x.id_personal);

			// 📝 Karena CompanyFilter hanya hasil query, tandai NoKey
			modelBuilder.Entity<CompanyFilter>().HasNoKey();
		}
	}
}
