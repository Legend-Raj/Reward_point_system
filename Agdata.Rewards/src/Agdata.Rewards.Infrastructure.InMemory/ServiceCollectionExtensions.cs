using System;
using System.Collections.Generic;
using Agdata.Rewards.Application.Interfaces;
using Agdata.Rewards.Application.Interfaces.Auth;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Application.Interfaces.Services;
using Agdata.Rewards.Application.Services;
using Agdata.Rewards.Infrastructure.InMemory.Auth;
using Agdata.Rewards.Infrastructure.InMemory.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Agdata.Rewards.Infrastructure.InMemory;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRewardsInMemory(
        this IServiceCollection services,
        Action<AdminSeedBuilder>? configureAdmins = null)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        services.AddSingleton<IUserRepository, UserRepositoryInMemory>();
        services.AddSingleton<IProductRepository, ProductRepositoryInMemory>();
        services.AddSingleton<IRedemptionRepository, RedemptionRepositoryInMemory>();
        services.AddSingleton<IEventRepository, EventRepositoryInMemory>();
        services.AddSingleton<IPointsTransactionRepository, PointsTransactionRepositoryInMemory>();
        services.AddSingleton<IUnitOfWork, InMemoryUnitOfWork>();

        services.AddSingleton<IAdminRegistry>(sp =>
        {
            var builder = new AdminSeedBuilder();
            builder.Add("admin@example.com");
            configureAdmins?.Invoke(builder);
            return new InMemoryAdminRegistry(builder.Seeds);
        });

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPointsLedgerService, PointsLedgerService>();
        services.AddScoped<IProductCatalogService, ProductCatalogService>();
        services.AddScoped<IRedemptionService, RedemptionService>();

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
