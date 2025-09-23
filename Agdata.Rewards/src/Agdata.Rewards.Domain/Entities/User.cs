using Agdata.Rewards.Domain.Exceptions;
using Agdata.Rewards.Domain.ValueObjects;

namespace Agdata.Rewards.Domain.Entities;

public class User
{
    public Guid Id { get; }
    public Email Email { get; }
    public EmployeeId EmployeeId { get; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }
    public int PointsBalance { get; private set; }

    public User(Guid id, string name, Email email, EmployeeId employeeId, bool isActive = true, int pointsBalance = 0)
    {
        if (id == Guid.Empty) 
        {
            throw new DomainException("User Id cannot be empty.");
        }
        if (string.IsNullOrWhiteSpace(name)) 
        {
            throw new DomainException("Name is required.");
        }
        if (pointsBalance < 0) 
        {
            throw new DomainException("Points balance cannot be negative.");
        }

        Id = id;
        Name = name.Trim();
        Email = email ?? throw new DomainException("Email is required.");
        EmployeeId = employeeId ?? throw new DomainException("EmployeeId is required.");
        IsActive = isActive;
        PointsBalance = pointsBalance;
    }
    public static User CreateNewEmployee(string name, string email, string employeeId)
    {
        return new User(Guid.NewGuid(), name, new Email(email), new EmployeeId(employeeId));
    }
    public void ActivateAccount() 
    {
        IsActive = true;
    }
    public void DeactivateAccount() 
    {
        IsActive = false;
    }
    public void AddPoints(int points)
    {
        if (points <= 0)
            throw new DomainException("Credit points must be a positive number.");
        
        checked
        {
            PointsBalance += points;
        }
    }
    public void DeductPoints(int points)
    {
        if (points <= 0)
            throw new DomainException("Debit points must be a positive number.");
        if (PointsBalance < points)
            throw new DomainException("Insufficient balance.");
        
        PointsBalance -= points;
    }
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new DomainException("Name cannot be empty.");
        }
        
        Name = newName.Trim();
    }
    public override string ToString() 
    {
        return $"{Name} ({Email}) [{EmployeeId}] - Balance: {PointsBalance}, Active: {IsActive}";
    }
}
