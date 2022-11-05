using Api;
using Api.Configs;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

public  class Program
{
	private static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);
		
		//видимо закидываем в билдер нашу конфигурацию из конфига (получаем секцию из конфига)
		var authSection = builder.Configuration.GetSection(AuthConfig.Position);
		//а тут берём наш конфиг и дальше так же сможем брать из DI конфиг (получаем сам конфиг)
		var authConfig = authSection.Get<AuthConfig>();
		// Add services to the container.
		
		builder.Services.Configure<AuthConfig>(authSection);//тут нужна именно секция

		builder.Services.AddControllers();
		// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen(c =>
		{
			c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
			{
				Description = "Введите токен пользователя",
				Name = "Authorization",
				In = ParameterLocation.Header,
				Type = SecuritySchemeType.ApiKey,
				Scheme = JwtBearerDefaults.AuthenticationScheme,
			});

			c.AddSecurityRequirement(new OpenApiSecurityRequirement()
			{
				
				{
					new OpenApiSecurityScheme
					{
						Reference = new OpenApiReference
						{
							Type = ReferenceType.SecurityScheme,
							Id = JwtBearerDefaults.AuthenticationScheme,
						},
						Scheme = "oauth2",
						Name = JwtBearerDefaults.AuthenticationScheme,
						In = ParameterLocation.Header,
					},
					new List<string>()
				}
			});
		});

		builder.Services.AddDbContext<DAL.DataContext>(options =>
		{
			// sql - доп.параметры? чем могут быть эти доп.параметры
			options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSql"), sql => { });
		});

		builder.Services.AddAutoMapper(typeof(MapperProfile).Assembly);

		builder.Services.AddScoped<UserService>();
		//сформировать ICollectionServices: var services = builder.Services
		//сделать метод добавления серфисов в отдельном классе (или в partial для Program)
		//пример от MindBox мог остаться на ноуте (там есть такое)

		builder.Services.AddAuthentication(ao => {
			ao.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
			}).AddJwtBearer(jbo =>
			{
				//отключаем проверку на ssl (пока нет сертификата ssl)
				jbo.RequireHttpsMetadata = false;
				//параметры валидации
				jbo.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuer = true,
					ValidIssuer = authConfig.Issuer,
					ValidateAudience = true,
					ValidAudience = authConfig.Audience,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = authConfig.SymmetricSecurityKey(),
					ClockSkew = TimeSpan.Zero
				};
			});

		builder.Services.AddAuthorization(ao =>
		{
			ao.AddPolicy("validAccessToken", ap =>
			{
				ap.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
				ap.RequireAuthenticatedUser();
			});
		});

		var app = builder.Build();

		//миграции?
		using (var serviceScope = ((IApplicationBuilder)app).ApplicationServices.GetService<IServiceScopeFactory>()?.CreateScope())
		{
			if (serviceScope != null)
			{
				var context = serviceScope.ServiceProvider.GetRequiredService<DAL.DataContext>();
				context.Database.Migrate();
			}
		}

		// Configure the HTTP request pipeline.
		//if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
		}

		app.UseHttpsRedirection();

		app.UseAuthentication();
		app.UseAuthorization();

		app.MapControllers();

		app.Run();
	}
}