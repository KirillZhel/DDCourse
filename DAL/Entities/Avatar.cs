namespace DAL.Entities
{
	public class Avatar : Attach
	{
        public virtual User Owner { get; set; } = null!;
        public Guid OwnerId { get; set; }
    }
}
