using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Api.Models.Attach
{
    public class AttachExternalModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string MimeType { get; set; } = null!;
        public string? ContentLink { get; set; } = null!;

        //public AttachExternakModel(AttachModel model, Func<AttachModel, string?>? linkGenerator)
        //{
        //    Id = model.Id;
        //    Name = model.Name;
        //    MimeType = model.MimeType;
        //    ContentLink = linkGenerator?.Invoke(model);
        //}
    }
}
