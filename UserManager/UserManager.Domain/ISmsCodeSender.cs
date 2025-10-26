using UserManager.Domain.ValueObjects;

namespace UserManager.Domain
{
    public interface ISmsCodeSender
    {
        Task SendCodeAsync(UserBasic userBasic,string code);
    }
}
