using Agdata.Rewards.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Agdata.Rewards.Infrastructure.SqlServer;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<LedgerEntry> LedgerEntries { get; set; } = null!;
    public DbSet<RedemptionRequest> RedemptionRequests { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

