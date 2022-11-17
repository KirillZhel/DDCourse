using Api.Models.Attach;
using Api.Models.Post;
using Api.Models.User;
using AutoMapper;
using Common;
using DAL.Entities;

namespace Api
{
    public class MapperProfile : Profile
	{
		public MapperProfile()
		{
			CreateMap<CreateUserModel, User>()
				.ForMember(d => d.Id, m => m.MapFrom(s => Guid.NewGuid()))
				.ForMember(d => d.PasswordHash, m => m.MapFrom(s => HashHelper.GetHash(s.Password)))
				.ForMember(d => d.BirthDate, m => m.MapFrom(s => s.BirthDate.UtcDateTime)); // указываем, что BirthDate должен быть записан в базе с учётом часового пояса

			CreateMap<User, UserModel>(); //доп.условий нет, так как то, что взяли из базы то и отдаём
            CreateMap<User, UserAvatarModel>();

            CreateMap<Avatar, AttachModel>();

			CreateMap<PostContent, AttachModel>();
            CreateMap<PostContent, AttachExternalModel>();

            CreateMap<CreatePostRequest, CreatePostModel>();
            CreateMap<MetadataModel, MetadataLinkModel>();
			CreateMap<MetadataLinkModel, PostContent>();
			CreateMap<CreatePostModel, Post>()
				.ForMember(d => d.PostContents, m => m.MapFrom(s => s.Contents))
				.ForMember(d => d.Created, m => m.MapFrom(s => DateTime.UtcNow));
        } 
	}
}
