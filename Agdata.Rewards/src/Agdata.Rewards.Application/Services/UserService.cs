using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agdata.Rewards.Application.DTOs.Common;
using Agdata.Rewards.Application.DTOs.Users;
using Agdata.Rewards.Application.Interfaces;
using Agdata.Rewards.Application.Interfaces.Repositories;
using Agdata.Rewards.Application.Interfaces.Services;
using Agdata.Rewards.Application.Services.Shared;
using Agdata.Rewards.Domain.Entities;
using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Domain.ValueObjects;
using Agdata.Rewards.Domain.Extensions;

namespace Agdata.Rewards.Application.Services;

public class UserService : IUserService
{
    private const int MaxPageSize = 100;

    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<User> CreateNewUserAsync(string firstName, string? middleName, string lastName, string email, string employeeId)
    {
        var emailAddress = new Email(email);
        var employeeIdentifier = new EmployeeId(employeeId);

        if (await _userRepository.GetUserByEmailAsync(emailAddress) is not null)
        {
            throw new DomainException(DomainErrors.User.EmailExists);
        }

        if (await _userRepository.GetUserByEmployeeIdAsync(employeeIdentifier) is not null)
        {
            throw new DomainException(DomainErrors.User.EmployeeIdExists);
        }

        var newUser = User.CreateNew(firstName, middleName, lastName, email, employeeId);

        _userRepository.AddUser(newUser);
        await _unitOfWork.SaveChangesAsync();

        return newUser;
    }

    public Task<User?> GetUserByIdAsync(Guid userId)
    {
        return _userRepository.GetUserByIdAsync(userId);
    }

    public Task<User?> GetByEmailAsync(string email)
    {
        var emailAddress = new Email(email);
        return _userRepository.GetUserByEmailAsync(emailAddress);
    }

    public Task<User?> GetByEmployeeIdAsync(string employeeId)
    {
        var employeeIdentifier = new EmployeeId(employeeId);
        return _userRepository.GetUserByEmployeeIdAsync(employeeIdentifier);
    }

    public Task<IReadOnlyList<User>> ListUsersAsync()
    {
        return _userRepository.ListUsersAsync();
    }

    public async Task<User> UpdateUserAsync(Guid userId, string? firstName, string? middleName, string? lastName, string? email, string? employeeId, bool? isActive)
    {
        var user = await _userRepository.GetUserByIdAsync(userId)
            ?? throw new DomainException(DomainErrors.User.NotFound);

        if (firstName is not null || middleName is not null || lastName is not null)
        {
            var updatedFirst = firstName ?? user.Name.FirstName;
            var updatedMiddle = middleName ?? user.Name.MiddleName;
            var updatedLast = lastName ?? user.Name.LastName;

            user.Rename(PersonName.Create(updatedFirst, updatedMiddle, updatedLast));
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            var desiredEmail = new Email(email);
            var existing = await _userRepository.GetUserByEmailAsync(desiredEmail);
            if (existing is not null && existing.Id != user.Id)
            {
                throw new DomainException(DomainErrors.User.EmailExists);
            }

            user.ChangeEmail(desiredEmail);
        }

        if (!string.IsNullOrWhiteSpace(employeeId))
        {
            var desiredEmployeeId = new EmployeeId(employeeId);
            var existingEmployee = await _userRepository.GetUserByEmployeeIdAsync(desiredEmployeeId);
            if (existingEmployee is not null && existingEmployee.Id != user.Id)
            {
                throw new DomainException(DomainErrors.User.EmployeeIdExists);
            }

            user.ChangeEmployeeId(desiredEmployeeId);
        }

        if (isActive.HasValue)
        {
            if (isActive.Value)
            {
                user.Activate();
            }
            else
            {
                user.Deactivate();
            }
        }

        _userRepository.UpdateUser(user);
        await _unitOfWork.SaveChangesAsync();

        return user;
    }

    public async Task<User> ActivateUserAsync(Guid userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId)
            ?? throw new DomainException(DomainErrors.User.NotFound);

        if (!user.IsActive)
        {
            user.Activate();
            _userRepository.UpdateUser(user);
            await _unitOfWork.SaveChangesAsync();
        }

        return user;
    }

    public async Task<User> DeactivateUserAsync(Guid userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId)
            ?? throw new DomainException(DomainErrors.User.NotFound);

        if (user.IsActive)
        {
            user.Deactivate();
            _userRepository.UpdateUser(user);
            await _unitOfWork.SaveChangesAsync();
        }

        return user;
    }

    public async Task<PagedResult<User>> QueryUsersAsync(UserQueryOptions queryOptions)
    {
        if (queryOptions is null)
        {
            throw new ArgumentNullException(nameof(queryOptions));
        }

        // Streamlined validation using Guard
        Guard.AgainstNegativeSkip(queryOptions.Skip);
        Guard.AgainstInvalidTake(queryOptions.Take, MaxPageSize);

        var users = await _userRepository.ListUsersAsync();

        var filtered = users.AsEnumerable();

        if (queryOptions.IsActive.HasValue)
        {
            filtered = filtered.Where(user => user.IsActive == queryOptions.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(queryOptions.Search))
        {
            var term = queryOptions.Search.NormalizeText();
            filtered = filtered.Where(user => MatchesSearch(user, term));
        }

        var ordered = filtered
            .OrderBy(user => user.Name.LastName)
            .ThenBy(user => user.Name.FirstName)
            .ThenBy(user => user.Email.Value)
            .ToList();

        var pageItems = ordered
            .Skip(queryOptions.Skip)
            .Take(queryOptions.Take)
            .ToList();

        return new PagedResult<User>(pageItems, ordered.Count, queryOptions.Skip, queryOptions.Take);
    }

    private static bool MatchesSearch(User user, string term)
    {
        return user.Name.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || user.Email.Value.Contains(term, StringComparison.OrdinalIgnoreCase)
            || user.EmployeeId.Value.Contains(term, StringComparison.OrdinalIgnoreCase);
    }
}