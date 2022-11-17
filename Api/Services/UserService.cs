using Api.Configs;
using Api.Models.Attach;
using Api.Models.Post;
using Api.Models.Token;
using Api.Models.User;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Common;
using DAL;
using DAL.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Api.Services
{
	public partial class UserService
	{
		private readonly IMapper _mapper;
		private readonly DataContext _context;
		private readonly AuthConfig _config;
		// фабрика контекстов
		// private readonly IDbContextFactory<DataContext> _dbContextFactory;
		private Func<User, string?>? _linkGenerator;

		public UserService(IMapper mapper, DataContext context, IOptions<AuthConfig> config)
		{
			_mapper = mapper;
			_context = context;
			_config = config.Value;
			//_dbContextFactory = dbContextFactory;
			// _context = dbContextFactory.CreateDbContext(); - можно и так, но тогда следует:
			// класс UserService должен реализовывать IDisposeble и реализовывать метод Dispose,
			// который должен диспозить этот глобальный контекст _context.Dispose();
		}

		public void SetLinkGenerator(Func<User, string?> linkGenerator)
		{
			_linkGenerator = linkGenerator;
		}

		public async Task<bool> CheckUserExist(string email)
		{
			//using (var dbc = _dbContextFactory.CreateDbContext())
			//{
			//	return await dbc.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
			//}
			return await _context.Users.AnyAsync(u => u.Email.ToLower() == email.ToLower());
		}

		public async Task AddAvatarToUser(Guid userId, MetadataModel meta, string filePath)
		{
			var user = await _context.Users.Include(u => u.Avatar).FirstOrDefaultAsync(u => u.Id == userId);
			if (user != null)
			{
				var avatar = new Avatar
				{
					Author = user,
					MimeType = meta.MimeType,
					FilePath = filePath,
					Name = meta.Name,
					Size = meta.Size,
				};
				user.Avatar = avatar;

				await _context.SaveChangesAsync();
			}
		}

		public async Task<AttachModel> GetUserAvatar(Guid userId)
		{
			var user = await GetUserById(userId);
			var attach = _mapper.Map<AttachModel>(user.Avatar);
			return attach;
		}

		// обычно так сущности из базы не удаляются. Они помечаются, что они не активны (вроде создаётся поле в таблице юзеров)
		// и спустя время все "неактивные" сущности удаляются одним махом (так и легче в случае, если требуется восстановить и легче базе удалять, так как это времязатратная операция)
		public async Task Delete(Guid id)
		{
			var dbUser = await GetUserById(id);
			if (dbUser != null)
			{
				_context.Users.Remove(dbUser); // пометка на удаление
				await _context.SaveChangesAsync(); // а тут уже и удаляем
			}
		}

		//вроде как асинхронные методы принято называть с префиксом Async
		public async Task<Guid> CreateUser(CreateUserModel model)
		{
			//прописывать логику проверки модели здесь бессмысленно, так как это можно сделать в самой модели CreateUserModel
			var dbUser = _mapper.Map<DAL.Entities.User>(model);
			var t = await _context.Users.AddAsync(dbUser);
			await _context.SaveChangesAsync();
			return t.Entity.Id;
		}

		public async Task<IEnumerable<UserAvatarModel>> GetUsers() =>
			(await _context.Users.AsNoTracking().Include(x => x.Avatar).ToListAsync())
				.Select(x => _mapper.Map<User, UserAvatarModel>(x, o => o.AfterMap(FixAvatar)));

		private async Task<User> GetUserById(Guid id)
		{
			var user = await _context.Users.Include(u => u.Avatar).FirstOrDefaultAsync(u => u.Id == id);

			if (user == null || user == default)
				throw new Exception("user not found");

			return user;
		}

		public async Task<UserAvatarModel> GetUser(Guid id) => 
			_mapper.Map<User, UserAvatarModel>(await GetUserById(id), o => o.AfterMap(FixAvatar));

		private void FixAvatar(User s, UserAvatarModel d)
		{
			d.AvatarLink = s.Avatar == null ? null : _linkGenerator?.Invoke(s);
        }
	}
}
