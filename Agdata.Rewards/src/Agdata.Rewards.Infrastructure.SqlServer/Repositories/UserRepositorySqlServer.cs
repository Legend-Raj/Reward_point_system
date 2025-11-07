using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.ValueObjects;
using Agdata.Rewards.Infrastructure.SqlServer;
using Microsoft.EntityFrameworkCore;

namespace Agdata.Rewards.Infrastructure.SqlServer.Repositories;

public class UserRepositorySqlServer : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepositorySqlServer(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public Task<User?> GetUserByIdForUpdateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    public Task<User?> GetUserByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.Value == email.Value, cancellationToken);
    }

    public Task<User?> GetUserByEmployeeIdAsync(EmployeeId employeeId, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.EmployeeId.Value == employeeId.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _context.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return users;
    }

    public async Task<IReadOnlyList<User>> GetTop3EmployeesWithHighestRewardsAsync(CancellationToken cancellationToken = default)
    {
        var users = await _context.Users
            .FromSqlRaw("EXEC [dbo].[sp_GetTop3EmployeesWithHighestRewards]")
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return users;
    }

    public void AddUser(User user)
    {
        _context.Users.Add(user);
    }

    public void UpdateUser(User user)
    {
        _context.Users.Update(user);
    }
}

