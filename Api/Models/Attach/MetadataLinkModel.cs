using System.Drawing;
using System.Xml.Linq;

namespace Api.Models.Attach
{
    public class MetadataLinkModel : MetadataModel
    {
        public string FilePath { get; set; } = null!;
        public Guid AuthorId { get; set; }

        //public MetaWithPath(MetadataModel model, Func<MetadataModel, string> pathGenerator, Guid authorId)
        //{
        //    TempId = model.TempId;
        //    Name = model.Name;
        //    MimeType = model.MimeType;
        //    Size = model.Size;
        //    FilePath = pathGenerator(model);
        //    AuthorId = authorId;
        //}
    }
}
