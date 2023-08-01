using jumwebapi.Data.ef;

namespace jumwebapi.Features.Users.Services;

public interface IUserService
{
    Task<IEnumerable<JustinUser>> AllUsersList();
    Task<JustinUser> GetUserById(long id);
    Task<JustinUser>GetUserByUserName(string username);
    Task<JustinUser> GetUserByPartId(decimal partId);
    Task<JustinUser> AddUser(JustinUser user);
    Task<JustinUser> UpdateUser(JustinUser user);
    Task<long> DeleteUser(JustinUser user);
}
