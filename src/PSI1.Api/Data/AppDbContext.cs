using Microsoft.EntityFrameworkCore;
using PSI1.Api.Models;

namespace PSI1.Api.Data;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
	{
	}
	
	public DbSet<User> Users => Set<User>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// usernames must be unique - also lets Register() check for duplicates efficiently
		modelBuilder.Entity<User>()
		    .HasIndex(u => u.Username)
		    .IsUnique();
	}
}
