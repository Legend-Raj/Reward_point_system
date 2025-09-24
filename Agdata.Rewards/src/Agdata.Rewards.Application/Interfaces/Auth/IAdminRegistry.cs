namespace Agdata.Rewards.Application.Interfaces.Auth;

public interface IAdminRegistry
{
    bool IsAdmin(string email, string employeeId);
    void AddAdmin(string emailOrEmployeeId);
    void RemoveAdmin(string emailOrEmployeeId);
}