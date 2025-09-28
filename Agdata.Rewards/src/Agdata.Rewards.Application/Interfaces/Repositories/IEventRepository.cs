using Agdata.Rewards.Domain.Entities;

namespace Agdata.Rewards.Application.Interfaces.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id);
    Task<IEnumerable<Event>> GetAllAsync();
    void Add(Event newEvent);
    void Update(Event eventToUpdate);
}