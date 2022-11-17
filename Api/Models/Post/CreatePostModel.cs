using Api.Models.Attach;
using Api.Models.User;
using DAL.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;

namespace Api.Models.Post
{
    public class CreatePostModel
    {
        public Guid Id { get; set; }
        public string? Description { get; set; }
        public Guid AuthorId { get; set; }

        public virtual List<MetadataLinkModel> Contents { get; set; } = new List<MetadataLinkModel>();

    }
}
