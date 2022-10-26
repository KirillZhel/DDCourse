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
		public DbSet<User> Users => Set<User>();

		public DataContext(DbContextOptions<DataContext> options) : base(options)
		{

		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			//Api - имя проекта
			optionsBuilder.UseNpgsql(b => b.MigrationsAssembly("Api"));
		}
	}
}
