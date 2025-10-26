using UserManager.Domain.Entities;
using UserManager.Domain.ValueObjects;

namespace UserManager.Domain.Entities;
public record UserLoginHistory : IAggregateRoot
{
    public long Id { get; init; }
    public Guid? UserId { get; init; }
    public UserBasic UserBasic { get; init; }
    public DateTime CreatedDateTime { get; init; }
    public string Messsage { get; init; }
    private UserLoginHistory(){}
    public UserLoginHistory(Guid? userId,
        UserBasic userBasic, string message)
    {
        this.UserId = userId;
        this.UserBasic = userBasic;
        this.CreatedDateTime = DateTime.UtcNow;
        this.Messsage = message;
    }
}