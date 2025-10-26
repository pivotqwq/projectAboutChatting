using MediatR;
using UserManager.Domain.ValueObjects;

namespace UserManager.Domain.Events
{
    public record class UserAccessResultEvent(UserBasic UserBasic, UserAccessResult Result) : INotification;
}
