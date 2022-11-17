using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Entities
{
	public class Post
	{
		public Guid Id { get; set; }
		public string? Description { get; set; }
		public DateTimeOffset Created { get; set; }
		public Guid AuthorId { get; set; }

		public virtual User Author { get; set; } = null!;
		public virtual ICollection<PostContent> PostContents { get; set; } = new List<PostContent>();


		// фото
		// реплаи
		// лайки?
		//public ICollection<Replie> Replies { get; set; }
		//public bool IsDeleted { get; set; } = false; // метка на удаление
	}
}
