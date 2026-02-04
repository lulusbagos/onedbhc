using System;

namespace one_db.Dto
{
	public class AddReferenceRequest
	{
		public string? Entity { get; set; }
		public string? Name { get; set; }
		public Guid? ParentId { get; set; }
	}
}
