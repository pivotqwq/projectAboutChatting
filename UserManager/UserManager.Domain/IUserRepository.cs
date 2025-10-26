using UserManager.Domain.Entities;
using UserManager.Domain.ValueObjects;
using UserManager.Domain.Events;

namespace UserManager.Domain;
public interface IUserRepository
{
    Task<User?> FindOneAsync(UserBasic userBasic);
    Task<User?> FindAllAsync(UserBasic userBasic);
    Task<User?> FindOneAsync(Guid userId);
    Task AddNewLoginHistoryAsync(UserBasic userBasic, string msg);
    Task PublishEventAsync(UserAccessResultEvent eventData);
}