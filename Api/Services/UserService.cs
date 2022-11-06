using Api.Configs;
using Api.Models;
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
	public class UserService
	{
		private readonly IMapper _mapper;
		private readonly DAL.DataContext _context;
		private readonly AuthConfig _config;
		// фабрика контекстов
		// private readonly IDbContextFactory<DataContext> _dbContextFactory;


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
					MineType = meta.MimeType,
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

		public async Task<List<UserModel>> GetUsers()
		{
			return await _context.Users.AsNoTracking().ProjectTo<UserModel>(_mapper.ConfigurationProvider).ToListAsync();
		}

		private async Task<User> GetUserById(Guid id)
		{
			var user = await _context.Users.Include(u => u.Avatar).FirstOrDefaultAsync(u => u.Id == id);

			if (user == null)
				throw new Exception("user not found");

			return user;
		}

		public async Task<UserModel> GetUser(Guid id)
		{
			var user = await GetUserById(id);

			return _mapper.Map<UserModel>(user);
		}

		private async Task<User> GetUserByCredention(string login, string password)
		{
			var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == login.ToLower());
			if (user == null)
				throw new Exception("user not found");

			if(!HashHelper.Verify(password, user.PasswordHash))
				throw new Exception("password is incorrect");
			//можно сделать как на большинстве сайтов "неверный логин или пароль"
			//в будущем можно к пользователю прикрутить помимо почты для логина имя пользователя, телефон и т.п.

			return user;
		}

		private TokenModel GenerateTokens(UserSession session)
		{
			//Claim - ?
			/*
			var claims = new Claim[]
			{
				new Claim(ClaimsIdentity.DefaultNameClaimType, user.Name),
				//new Claim("displayName", user.Name),
				new Claim("id", user.Id.ToString()),
			};
			*/

			var jwt = new JwtSecurityToken(
				issuer: _config.Issuer,
				audience: _config.Audience,
				notBefore: DateTime.Now,
				claims: new Claim[] {
					new Claim(ClaimsIdentity.DefaultNameClaimType, session.User.Name),
					new Claim("sessionId", session.Id.ToString()), //метка по которой мы можем проверить актуальность сессии
					new Claim("id", session.User.Id.ToString())
				},
				expires: DateTime.Now.AddMinutes(_config.LifeTime),
				signingCredentials: new SigningCredentials(_config.SymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

			var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

			//в refreshToken нет issuer и audience для того, что бы нельзя было авторизоваться этим токеном
			var refreshToken = new JwtSecurityToken(
				notBefore: DateTime.Now,
				claims: new Claim[] {new Claim("refreshToken", session.RefreshToken.ToString()) }, // метка по которой можем понять, что сессия используется
				expires: DateTime.Now.AddHours(_config.LifeTime), //наверное стоит в appsetings своё собственное lifetime для refreshToken
				signingCredentials: new SigningCredentials(_config.SymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

			var encodedRefresh = new JwtSecurityTokenHandler().WriteToken(refreshToken);

			return new TokenModel(encodedJwt, encodedRefresh);
		}

		public async Task<TokenModel> GetToken(string login, string password)
		{
			var user = await GetUserByCredention(login, password);
			var session = await _context.UserSessions.AddAsync(new UserSession
			{
				Id = Guid.NewGuid(),
				User = user,
				RefreshToken = Guid.NewGuid(),
				Created = DateTime.UtcNow
			});
			await _context.SaveChangesAsync();
			return GenerateTokens(session.Entity);
		}

		public async Task<UserSession> GetSessionById(Guid id)
		{
			var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.Id == id);
			if (session == null)
			{
				throw new Exception("session is not found 1");
			}
			return session;
		}

		private async Task<UserSession> GetSessionByRefreshToken(Guid id)
		{
			var session = await _context.UserSessions
				.Include(s => s.User) // тут привязываем к сессии её пользователя
				.FirstOrDefaultAsync(s => s.RefreshToken == id);
			
			if (session == null)
			{
				throw new Exception("session is not found 2");
			}
			
			return session;
		}

		public async Task<TokenModel> GetTokenByRefreshToken(string refreshToken)
		{
			var validParams = new TokenValidationParameters
			{
				ValidateAudience = false,
				ValidateIssuer = false,
				ValidateIssuerSigningKey = true,
				ValidateLifetime = true,
				IssuerSigningKey = _config.SymmetricSecurityKey()
			};

			var principal = new JwtSecurityTokenHandler().ValidateToken(refreshToken, validParams, out var securityToken);

			if (securityToken is not JwtSecurityToken jwtToken 
				|| !jwtToken.Header.Alg.Equals(
					SecurityAlgorithms.HmacSha256,
					StringComparison.InvariantCultureIgnoreCase))
			{
				throw new SecurityTokenException("Invalid token");
			}

			if (principal.Claims.FirstOrDefault(c => c.Type == "refreshToken")?.Value is String refreshIdString
				&& Guid.TryParse(refreshIdString, out var refreshId))
			{
				var session = await GetSessionByRefreshToken(refreshId);

				if (!session.IsActive)
				{
					throw new Exception("sessions is not active");
				}

				session.RefreshToken = Guid.NewGuid(); //обновляем refresh token при обновлении сессии
				await _context.SaveChangesAsync();

				return GenerateTokens(session);
			}
			else
			{
				throw new SecurityTokenException("Invalid token");
			}
		}
	}
}
