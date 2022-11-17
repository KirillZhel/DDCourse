using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
	public class Replie
	{
		public Guid CommentId { get; set; }
		public Guid AuthorId { get; set; }
		public string Text { get; set; } = null!;
	}
}
