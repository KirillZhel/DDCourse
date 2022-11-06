using AutoMapper;
using Common;

namespace Api
{
	public class MapperProfile : Profile
	{
		public MapperProfile()
		{
			CreateMap<Models.CreateUserModel, DAL.Entities.User>()
				.ForMember(d => d.Id, m => m.MapFrom(s => Guid.NewGuid()))
				.ForMember(d => d.PasswordHash, m => m.MapFrom(s => HashHelper.GetHash(s.Password)))
				.ForMember(d => d.BirthDate, m => m.MapFrom(s => s.BirthDate.UtcDateTime)); // указываем, что BirthDate должен быть записан в базе с учётом часового пояса

			CreateMap<DAL.Entities.User, Models.UserModel>(); //доп.условий нет, так как то, что взяли из базы то и отдаём

			CreateMap<DAL.Entities.Avatar, Models.AttachModel>();
		}
	}
}
