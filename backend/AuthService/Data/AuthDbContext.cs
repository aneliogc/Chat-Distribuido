using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(u => u.Id);
            b.HasIndex(u => u.Username).IsUnique();
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.Username).HasColumnName("username");
            b.Property(u => u.Email).HasColumnName("email");
            b.Property(u => u.PasswordHash).HasColumnName("password_hash");
            b.Property(u => u.CreatedAt).HasColumnName("created_at");
            b.Property(u => u.Id).HasColumnName("id");
        });
    }
}
