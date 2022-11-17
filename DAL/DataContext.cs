using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
	public class DataContext : DbContext
	{
		public DataContext(DbContextOptions<DataContext> options) : base(options) { }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			//проверка на уникальность по полю Email
			modelBuilder
				.Entity<User>()
				.HasIndex(u => u.Email)
				.IsUnique();

			modelBuilder.Entity<Avatar>().ToTable(nameof(Avatars));
			modelBuilder.Entity<PostContent>().ToTable(nameof(PostContent));
		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
			=> optionsBuilder.UseNpgsql(b => b.MigrationsAssembly("Api")); //Api - имя проекта

		public DbSet<User> Users => Set<User>();
		public DbSet<UserSession> UserSessions => Set<UserSession>();
		public DbSet<Attach> Attaches => Set<Attach>();
		public DbSet<Avatar> Avatars => Set<Avatar>();

		public DbSet<Post> Posts => Set<Post>();
		public DbSet<PostContent> PostContents => Set<PostContent>();

	}
}
