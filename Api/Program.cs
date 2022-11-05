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
		
		//������ ���������� � ������ ���� ������������ �� ������� (�������� ������ �� �������)
		var authSection = builder.Configuration.GetSection(AuthConfig.Position);
		//� ��� ���� ��� ������ � ������ ��� �� ������ ����� �� DI ������ (�������� ��� ������)
		var authConfig = authSection.Get<AuthConfig>();
		// Add services to the container.
		
		builder.Services.Configure<AuthConfig>(authSection);//��� ����� ������ ������

		builder.Services.AddControllers();
		// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen(c =>
		{
			c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
			{
				Description = "������� ����� ������������",
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
			// sql - ���.���������? ��� ����� ���� ��� ���.���������
			options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSql"), sql => { });
		});

		builder.Services.AddAutoMapper(typeof(MapperProfile).Assembly);

		builder.Services.AddScoped<UserService>();
		//������������ ICollectionServices: var services = builder.Services
		//������� ����� ���������� �������� � ��������� ������ (��� � partial ��� Program)
		//������ �� MindBox ��� �������� �� ����� (��� ���� �����)

		builder.Services.AddAuthentication(ao => {
			ao.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
			}).AddJwtBearer(jbo =>
			{
				//��������� �������� �� ssl (���� ��� ����������� ssl)
				jbo.RequireHttpsMetadata = false;
				//��������� ���������
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

		//��������?
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