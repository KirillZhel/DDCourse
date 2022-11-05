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

		public UserService(IMapper mapper, DataContext context, IOptions<AuthConfig> config)
		{
			_mapper = mapper;
			_context = context;
			_config = config.Value;
		}

		//вроде как асинхронные методы принято называть с префиксом Async
		public async Task CreateUser(CreateUserModel model)
		{
			//прописывать логику проверки модели здесь бессмысленно, так как это можно сделать в самой модели CreateUserModel
			var dbUser = _mapper.Map<DAL.Entities.User>(model);
			await _context.Users.AddAsync(dbUser);
			await _context.SaveChangesAsync();
		}

		public async Task<List<UserModel>> GetUsers()
		{
			return await _context.Users.AsNoTracking().ProjectTo<UserModel>(_mapper.ConfigurationProvider).ToListAsync();
		}

		private async Task<DAL.Entities.User> GetUserById(Guid id)
		{
			var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

			if (user == null)
				throw new Exception("user not found");

			return user;
		}

		public async Task<UserModel> GetUser(Guid id)
		{
			var user = await GetUserById(id);

			return _mapper.Map<UserModel>(user);
		}

		private async Task<DAL.Entities.User> GetUserByCredention(string login, string password)
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

		private TokenModel GenerateTokens(DAL.Entities.User user)
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
					new Claim(ClaimsIdentity.DefaultNameClaimType, user.Name),
					new Claim("id", user.Id.ToString()),
				},
				expires: DateTime.Now.AddMinutes(_config.LifeTime),
				signingCredentials: new SigningCredentials(_config.SymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

			var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

			//в refreshToken нет issuer и audience для того, что бы нельзя было авторизоваться этим токеном
			var refreshToken = new JwtSecurityToken(
				notBefore: DateTime.Now,
				claims: new Claim[] { new Claim("id", user.Id.ToString()) },
				expires: DateTime.Now.AddHours(_config.LifeTime), //наверное стоит в appsetings своё собственное lifetime для refreshToken
				signingCredentials: new SigningCredentials(_config.SymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

			var encodedRefresh = new JwtSecurityTokenHandler().WriteToken(refreshToken);

			return new TokenModel(encodedJwt, encodedRefresh);
		}

		public async Task<TokenModel> GetToken(string login, string password)
		{
			var user = await GetUserByCredention(login, password);

			return GenerateTokens(user);
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

			if (principal.Claims.FirstOrDefault(x => x.Type == "id")?.Value is String userIdString 
				&& Guid.TryParse(userIdString, out var userId))
			{
				var user = await GetUserById(userId);
				return GenerateTokens(user);
			}
			else
			{
				throw new SecurityTokenException("Invalid token");
			}
		}
	}
}
