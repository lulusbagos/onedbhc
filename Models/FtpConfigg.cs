namespace one_db.Models
{
	public class FtpConfigg
	{
		public string Host { get; set; } = "";
		public int Port { get; set; } = 21;
		public string User { get; set; } = "";
		public string Password { get; set; } = "";
		public bool Secure { get; set; } = false;
	}
}
