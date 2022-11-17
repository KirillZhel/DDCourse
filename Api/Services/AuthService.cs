using Api.Configs;
using Api.Models.Token;
using AutoMapper;
using Common;
using Common.Consts;
using DAL;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Api.Services
{
    public class AuthService
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly AuthConfig _config;

        public AuthService(IMapper mapper, DataContext context, IOptions<AuthConfig> config)
        {
            _mapper = mapper;
            _context = context;
            _config = config.Value;
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

        public async Task<UserSession> GetSessionById(Guid id)
        {
            var session = await _context.UserSessions.FirstOrDefaultAsync(s => s.Id == id);
            if (session == null)
            {
                throw new Exception("session is not found 1");
            }
            return session;
        }

        private async Task<User> GetUserByCredention(string login, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == login.ToLower());
            if (user == null)
                throw new Exception("user not found");

            if (!HashHelper.Verify(password, user.PasswordHash))
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
                    new Claim(ClaimNames.SessionId, session.Id.ToString()), //метка по которой мы можем проверить актуальность сессии
					new Claim(ClaimNames.Id, session.User.Id.ToString())
                },
                expires: DateTime.Now.AddMinutes(_config.LifeTime),
                signingCredentials: new SigningCredentials(_config.SymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            //в refreshToken нет issuer и audience для того, что бы нельзя было авторизоваться этим токеном
            var refreshToken = new JwtSecurityToken(
                notBefore: DateTime.Now,
                claims: new Claim[] { new Claim(ClaimNames.RefreshToken, session.RefreshToken.ToString()) }, // метка по которой можем понять, что сессия используется
                expires: DateTime.Now.AddHours(_config.LifeTime), //наверное стоит в appsetings своё собственное lifetime для refreshToken
                signingCredentials: new SigningCredentials(_config.SymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));

            var encodedRefresh = new JwtSecurityTokenHandler().WriteToken(refreshToken);

            return new TokenModel(encodedJwt, encodedRefresh);
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
    }
}
