using Api;
using Api.Configs;
using Api.Middlewares;
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
		//а тут берЄм наш конфиг и дальше так же сможем брать из DI конфиг (получаем сам конфиг)
		var authConfig = authSection.Get<AuthConfig>();
		// Add services to the container.
		
		builder.Services.Configure<AuthConfig>(authSection);//тут нужна именно секци€

		builder.Services.AddControllers();
		// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen(c =>
		{
			c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
			{
				Description = "¬ведите токен пользовател€",
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

		// можно добавл€ть и фабрику контекстов дл€ более глубокого управлени€ инициализацией экземпл€ров контекста
		// это нужно, например, в случае добавлени€ в бд большого объЄма данных
		builder.Services.AddDbContext<DAL.DataContext>(options =>
		{
			// sql - доп.параметры? чем могут быть эти доп.параметры
			options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSql"), sql => { });
		});
		// можно добавл€ть и фабрику контекстов дл€ более глубокого управлени€ инициализацией экземпл€ров контекста
		// это нужно, например, в случае добавлени€ в бд большого объЄма данных
		//builder.Services.AddDbContextFactory<DAL.DataContext>(options =>
		//{
		//	options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSql"), sql => { });
		//});


		builder.Services.AddAutoMapper(typeof(MapperProfile).Assembly);

		builder.Services.AddScoped<UserService>();
        builder.Services.AddScoped<PostService>();
		builder.Services.AddScoped<AuthService>();
        //сформировать ICollectionServices: var services = builder.Services
        //сделать метод добавлени€ серфисов в отдельном классе (или в partial дл€ Program)
        //пример от MindBox мог остатьс€ на ноуте (там есть такое)

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
		app.UseTokenValidator();
		app.MapControllers();

		app.Run();
	}
}