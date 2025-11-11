using Agdata.Rewards.Application.Interfaces;
using Agdata.Rewards.Application.Interfaces.Auth;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Application.Interfaces.Services;
using Agdata.Rewards.Application.Services;
using Agdata.Rewards.Infrastructure.InMemory.Auth;
using Agdata.Rewards.Infrastructure.SqlServer.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Agdata.Rewards.Infrastructure.SqlServer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRewardsSqlServer(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AdminSeedBuilder>? configureAdmins = null)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));
        if (configuration is null) throw new ArgumentNullException(nameof(configuration));

        var connectionString = configuration.GetConnectionString("RewardsDb")
            ?? throw new InvalidOperationException("Connection string 'RewardsDb' not found.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
                sqlOptions.CommandTimeout(30);
            });
            
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);
        });

        services.AddScoped<IUserRepository, UserRepositorySqlServer>();
        services.AddScoped<IProductRepository, ProductRepositorySqlServer>();
        services.AddScoped<IRedemptionRequestRepository, RedemptionRequestRepositorySqlServer>();
        services.AddScoped<IEventRepository, EventRepositorySqlServer>();
        services.AddScoped<ILedgerEntryRepository, LedgerEntryRepositorySqlServer>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddSingleton<IAdminRegistry>(sp =>
        {
            var builder = new AdminSeedBuilder();
            builder.Add("admin@example.com");
            configureAdmins?.Invoke(builder);
            return new InMemoryAdminRegistry(builder.Seeds);
        });

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IPointsService, PointsService>();
        services.AddScoped<IPointsLedgerService, PointsLedgerService>();
        services.AddScoped<IProductCatalogService, ProductCatalogService>();
        services.AddScoped<IRedemptionRequestService, RedemptionRequestService>();

        return services;
    }

    public sealed class AdminSeedBuilder
    {
        private readonly HashSet<string> _seeds = new(StringComparer.OrdinalIgnoreCase);

        internal IEnumerable<string> Seeds => _seeds;

        public void Add(string emailOrEmployeeId)
        {
            if (string.IsNullOrWhiteSpace(emailOrEmployeeId))
            {
                throw new ArgumentException("Admin identifier cannot be blank.", nameof(emailOrEmployeeId));
            }

            _seeds.Add(emailOrEmployeeId.Trim());
        }

        public void AddByEmail(string email) => Add(email);
        public void AddByEmployeeId(string employeeId) => Add(employeeId);
    }
}

