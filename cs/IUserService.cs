using System.Collections.Generic;
using RapidRents.Web.Domain;
using RapidRents.Web.Models.Requests.Users;
using RapidRents.Web.Models;
using System.Threading.Tasks;
using RapidRents.Web.Models.Requests.SignIn;
using Microsoft.AspNet.Identity.EntityFramework;
using RapidRents.Web.Models.Requests.Register;
using RapidRents.Web.Domain.Users;

namespace RapidRents.Web.Services
{
    public interface IUserService
    {
        IdentityUser GetCurrentUser();
        IdentityUser CreateUser(RegisterRequest model); 

        Task<bool> Signin(SignInRequest model);
        Task<bool> ChangePassword(string Id, string Code, string Password);
        Task<bool> RecoverPassword(string userId);
        Task<ApplicationUser> GetUserById(string userId);
        List<CustomUIRequest> GetCustomUI();
        List<CustomUICondensedRequest> GetCustomUICondensed();
        List<User> Get();
        List<UserRole> GetRoleId();
        List<UserAvatar> GetAllUsers();
        List<UserRole> GetUserRoles(int id);
        User GetUserById(int Id);
        UserAvatar GetUser(string UserId);
        bool IsLoggedIn();
        bool IsUser(string emailaddress);
        bool Logout();
        int Insert(UserAddRequest model);
        string GetCurrentUserId();
        void InsertCustomUI(CustomUIRequest model);
        void UpdateCustomUI(CustomUIRequest model);
        void Update(UserUpdateRequest model);
        void DeleteUserById(int Id);
        List<UserRole>GetUserRoles(string aspNetUserId);
        void DeleteCustomUI(CustomUIRequest model);
        void DeleteRoleFromUser(int id, string roleId);
        void AddRoleToUser(string id, string roleId);
        PagedList<UserAvatar> AdminDashboardPagedList(int pageIndex, int pageSize);
        int InsertLegacy(UserAddLegacyRequest model);
        int InsertByAvatarProfile(AddUserbyAvatar model);
    }
}