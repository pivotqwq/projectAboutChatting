using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserManager.Domain.Entities;
using UserManager.Domain.ValueObjects;
using UserManager.Domain;
using UserManager.Domain.Events;

namespace UserManager.Infrastracture
{
    public class UserRepository : IUserRepository
    {
        private readonly UserDBContext _dbContext;
        private readonly IMediator _mediator;
        
        public UserRepository(UserDBContext dbContext, IMediator mediator)
        {
            _dbContext = dbContext;
            _mediator = mediator;
        }

        public async Task AddNewLoginHistoryAsync(UserBasic userBasic, string message)
        {
            User? user = await FindOneAsync(userBasic);
            Guid? userId = user?.Id;
            _dbContext.UserLoginHistories.Add(new UserLoginHistory
                (userId, userBasic, message));
            await _dbContext.SaveChangesAsync();
        }

        public async Task<User?> FindOneAsync(UserBasic userBasic)
        {
            // 支持通过手机号或邮箱进行查找
            return await _dbContext.Users
                .Include(u => u.UserAccessFail)
                .Where(u => u.UserBasic != null)
                .Where(u => 
                    (!string.IsNullOrEmpty(userBasic.PhoneNumber) && u.UserBasic.PhoneNumber == userBasic.PhoneNumber) ||
                    (!string.IsNullOrEmpty(userBasic.Email) && u.UserBasic.Email == userBasic.Email)
                )
                .FirstOrDefaultAsync();
        }
        public async Task<User?> FindAllAsync(UserBasic userBasic)
        {
            // 通过手机号和邮箱进行查找
            return await _dbContext.Users
                .Include(u => u.UserAccessFail)
                .Where(u => u.UserBasic != null)
                .Where(u =>
                    (!string.IsNullOrEmpty(userBasic.PhoneNumber) && u.UserBasic.PhoneNumber == userBasic.PhoneNumber) &&
                    (!string.IsNullOrEmpty(userBasic.Email) && u.UserBasic.Email == userBasic.Email)
                )
                .FirstOrDefaultAsync();
        }

        public async Task<User?> FindOneAsync(Guid userId)
        {
            return await _dbContext.Users
                .Include(u => u.UserAccessFail)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public Task PublishEventAsync(UserAccessResultEvent eventData)
        {
            return _mediator.Publish(eventData);
        }
    }
}
