using MediatR;
using UserManager.Domain;
using UserManager.Domain.Events;

namespace UserManager.WebAPI.Events
{
    public class UserAccessResultEventHandler
        : INotificationHandler<UserAccessResultEvent>
    {
        private readonly IUserRepository repository;

        public UserAccessResultEventHandler(IUserRepository repository)
        {
            this.repository = repository;
        }

        public Task Handle(UserAccessResultEvent notification, CancellationToken cancellationToken)
        {
            var result = notification.Result;
            var userBasic = notification.UserBasic;
            string msg;
            switch(result)
            {
                case UserAccessResult.OK:
                    msg = $"{userBasic}登陆成功";
                    break;
                case UserAccessResult.PhoneNumberNotFound:
                    msg = $"{userBasic}登陆失败，因为用户不存在";
                    break;
                case UserAccessResult.PasswordError:
                    msg = $"{userBasic}登陆失败，密码错误";
                    break;
                case UserAccessResult.NoPassword:
                    msg = $"{userBasic}登陆失败，没有设置密码";
                    break;
                case UserAccessResult.Lockout:
                    msg = $"{userBasic}登陆失败，被锁定";
                    break;
                default:
                    throw new NotImplementedException();
            }
            return repository.AddNewLoginHistoryAsync(userBasic,msg);
        }
    }
}
