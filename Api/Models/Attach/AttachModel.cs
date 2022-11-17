using DAL.Entities;

namespace Api.Models.Attach
{
    public class AttachModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string MimeType { get; set; } = null!;
        public string FilePath { get; set; } = null!;
    }
}
