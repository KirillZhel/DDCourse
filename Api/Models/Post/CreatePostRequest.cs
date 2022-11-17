using Api.Models.Attach;

namespace Api.Models.Post
{
    public class CreatePostRequest
    {
        public Guid? AuthorId { get; set; }
        public string? Description { get; set; }

        public virtual List<MetadataModel> Contents { get; set; } = new List<MetadataModel>();
    }
}
