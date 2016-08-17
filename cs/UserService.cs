using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using RapidRents.Data;
using RapidRents.Web.Domain;
using RapidRents.Web.Exceptions;
using RapidRents.Web.Models;
using RapidRents.Web.Models.Requests.Users;
using System.Web.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Web;
using RapidRents.Web.Models.Requests.SignIn;
using System.Threading.Tasks;
using RapidRents.Web.Services.Messaging;
using RapidRents.Web.Models.Requests.Register;
using System.Web.Routing;
using RapidRents.Web.Domain.Users;

namespace RapidRents.Web.Services
{
    public class UserService : BaseService, IUserService
    {
        private readonly ApplicationUserManager _userManager;
        private readonly IAuthenticationManager _authenticationManager;
        private readonly IMessagingService _messagingService;

        public UserService(ApplicationUserManager userManager, IAuthenticationManager authenticationManager, IMessagingService messagingService)
        {
            _userManager = userManager;
            _authenticationManager = authenticationManager;
            _messagingService = messagingService;

        }

        private static ApplicationUserManager GetUserManager()
        {
            return HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>();
        }

        public IdentityUser CreateUser(RegisterRequest model)
        {
            ApplicationUser newUser = new ApplicationUser { UserName = model.Email, Email = model.Email, LockoutEnabled = false };
            IdentityResult result = null;
            try
            {
                result = _userManager.Create(newUser, model.Password);
            }
            catch (Exception)
            {
                throw;
            }

            if (!result.Succeeded)
            {
                throw new IdentityResultException(result);
            }

            EmailConfirmation(newUser.Id, newUser.Email);

            Insert(model, newUser.Id);

            return newUser;
        }

        public async Task<bool> Signin(SignInRequest model)
        {
            ApplicationUser user = await _userManager.FindAsync(model.EmailAddress, model.Password);
            if (user == null)
            {
                throw new Exception("The Email/ Password is incorrect");
            }
            else if (user.EmailConfirmed == false)
            { 
                throw new Exception("Login Credentials is not valid");
            }
            await SignInAsync(user, model.RememberMe);

            return true;
        }

        private async Task SignInAsync(ApplicationUser user, bool isPersistent)
        {
            _authenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            ClaimsIdentity identity = await _userManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
            _authenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, identity);
        }

        public bool IsUser(string emailaddress)
        {
            ApplicationUser user = _userManager.FindByEmail(emailaddress);
            return user != null;
        }

        public async Task<ApplicationUser> GetUserById(string userId)
        {
            ApplicationUser user = await _userManager.FindByIdAsync(userId);
            return user;
        }

        public async Task<bool> RecoverPassword(string emailaddress)
        {
            bool result = false;

            ApplicationUser user = await _userManager.FindByEmailAsync(emailaddress);

            if (user != null)
            {
                string code = await _userManager.GeneratePasswordResetTokenAsync(user.Id);
                string confirmationEmailCode = await _userManager.GenerateEmailConfirmationTokenAsync(user.Id);
                code = HttpUtility.UrlEncode(code);

                var requestContext = HttpContext.Current.Request.RequestContext;
                string callbackUrl = new UrlHelper(requestContext).Action("NewPassword", "Login",
                new { userId = user.Id, code = code, confirmationEmailCode },
                protocol: HttpContext.Current.Request.Url.Scheme);

                _messagingService.PasswordResetToken(user.Email, callbackUrl);

                result = true;
            }
            else
            {
                result = false;
            }

            return result;
        }

        public async Task<bool> ChangePassword(string UserId, string Token, string Password)
        {
            var PasswordReset = await _userManager.ResetPasswordAsync(UserId, Token, Password);
            if (string.IsNullOrEmpty(UserId) || string.IsNullOrEmpty(Password))
            {
                throw new Exception("You must provide a userId and a password");
            }

            ApplicationUser user = await GetUserById(UserId);
            if (user == null)
            {
                return false;
            }

            await _userManager.RemovePasswordAsync(UserId);
            IdentityResult res = await _userManager.AddPasswordAsync(UserId, Password);

            if (res.Succeeded)
            {
                return res.Succeeded;
            }
            else
            {
                throw new IdentityResultException(res);
            }

        }

        public bool Logout()
        {
            _authenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return true;
        }

        public IdentityUser GetCurrentUser()
        {
            if (!IsLoggedIn())
            {
                return null;
            }

            else { 
            IdentityUser currentUserId = _userManager.FindById(GetCurrentUserId());
            return currentUserId;
        }
        }

        public string GetCurrentUserId()
        {
            return HttpContext.Current.User.Identity.GetUserId();
        }

        public bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(GetCurrentUserId());
        }

        public bool EmailConfirmation(string newUserId, string email)
        {
            string ConfirmationCode = _userManager.GenerateEmailConfirmationToken(newUserId);

            RequestContext requestContext = HttpContext.Current.Request.RequestContext;

            string callbackURL = new UrlHelper(requestContext).
                
                Action("ConfirmEmail", "Register", new { userId = newUserId, code = ConfirmationCode }
                
                , protocol: HttpContext.Current.Request.Url.Scheme);

            _messagingService.SendEmailConfirmation(email, callbackURL);

            return true;
        }

        public int Insert(RegisterRequest model, string userId)
        {
            int id = 0;

            DataProvider.ExecuteNonQuery(GetConnection, "dbo.Users_Insert_V2"
               , inputParamMapper: delegate (SqlParameterCollection paramCollection)
               {
                   if (model != null)
                   {
                   paramCollection.AddWithValue("@FirstName", model.FirstName);
                   paramCollection.AddWithValue("@LastName", model.LastName);
                       paramCollection.AddWithValue("@Email", model.Email);
                       paramCollection.AddWithValue("@UserRole", model.UserRole);
                   paramCollection.AddWithValue("@AspNetUserId", userId);
                   }

                   SqlParameter p = new SqlParameter("@Id", SqlDbType.Int);
                   p.Direction = ParameterDirection.Output;

                   paramCollection.Add(p);
               }, returnParameters: delegate (SqlParameterCollection param)
               {
                   int.TryParse(param["@Id"].Value.ToString(), out id);
               }
               );

            return id;
        }

        public void Update(UserUpdateRequest model)
        {
            DataProvider.ExecuteNonQuery(GetConnection, "dbo.Users_Update"
               , inputParamMapper: delegate (SqlParameterCollection paramCollection)
               {
                   paramCollection.AddWithValue("@Id", model.Id);
                   paramCollection.AddWithValue("@SalutationId", model.SalutationId);
                   paramCollection.AddWithValue("@FirstName", model.FirstName);
                   paramCollection.AddWithValue("@MiddleInitial", model.MiddleInitial);
                   paramCollection.AddWithValue("@LastName", model.LastName);
                   paramCollection.AddWithValue("@Email", model.Email);
                   paramCollection.AddWithValue("@PhoneNumber", model.PhoneNumber);
                   paramCollection.AddWithValue("@DateOfBirth", model.DateOfBirth);
                   paramCollection.AddWithValue("@AspNetUserId", GetCurrentUserId());
               }, returnParameters: delegate (SqlParameterCollection param)
               { }
               );
        }

        public List<User> Get()
        {
            List<User> list = null;

            DataProvider.ExecuteCmd(GetConnection, "dbo.Users_Select"
               , inputParamMapper: null
               , map: delegate (IDataReader reader, short set)
               {
                   User p = new User();
                   int startingIndex = 0;

                   p.Id = reader.GetSafeInt32(startingIndex++);
                   p.SalutationId = reader.GetSafeInt32(startingIndex++);
                   p.FirstName = reader.GetSafeString(startingIndex++);
                   p.MiddleInitial = reader.GetSafeString(startingIndex++);
                   p.LastName = reader.GetSafeString(startingIndex++);
                   p.Email = reader.GetSafeString(startingIndex++);
                   p.PhoneNumber = reader.GetSafeString(startingIndex++);
                   p.DateOfBirth = reader.GetSafeDateTime(startingIndex++);
                   p.DateAdded = reader.GetSafeDateTime(startingIndex++);
                   p.DateModified = reader.GetSafeDateTime(startingIndex++);
                   if (list == null)
                   {
                       list = new List<User>();
                   }

                   list.Add(p);
               }
               );

            return list;
        }

        public User GetUserById(int Id)
        {
            User p = null;

            DataProvider.ExecuteCmd(GetConnection, "dbo.Users_SelectById"
               , inputParamMapper: delegate (SqlParameterCollection paramCollection)
               {
                   paramCollection.AddWithValue("@Id", Id);
               }, map: delegate (IDataReader reader, short set)
               {
                   p = new User();
                   int startingIndex = 0;

                   p.Id = reader.GetSafeInt32(startingIndex++);
                   p.SalutationId = reader.GetSafeInt32(startingIndex++);
                   p.FirstName = reader.GetSafeString(startingIndex++);
                   p.MiddleInitial = reader.GetSafeString(startingIndex++);
                   p.LastName = reader.GetSafeString(startingIndex++);
                   p.Email = reader.GetSafeString(startingIndex++);
                   p.PhoneNumber = reader.GetSafeString(startingIndex++);
                   p.DateOfBirth = reader.GetSafeDateTime(startingIndex++);
                   p.DateAdded = reader.GetSafeDateTime(startingIndex++);
                   p.DateModified = reader.GetSafeDateTime(startingIndex++);
               });

            return p;
        }

        public void DeleteUserById(int Id)
        {
            DataProvider.ExecuteNonQuery(GetConnection, "dbo.Users_DeleteById"
               , inputParamMapper: delegate (SqlParameterCollection paramCollection)
               {
                   paramCollection.AddWithValue("@Id", Id);
               }, returnParameters: delegate (SqlParameterCollection param)
               { }
               );
        }

        public int Insert(UserAddRequest model)
        {
            throw new NotImplementedException();
        }

        public UserAvatar GetUser(string userId)
        {
            UserAvatar p = null;

            DataProvider.ExecuteCmd(GetConnection, "dbo.Users_SelectByAspNetUserId"
            , inputParamMapper: delegate (SqlParameterCollection paramCollection)
            {
                paramCollection.AddWithValue("@AspNetUserId", userId);
            }
            , map: delegate (IDataReader reader, short set)
            {
                p = new UserAvatar();
                int startingIndex = 0;

                p.Id = reader.GetSafeInt32(startingIndex++);
                p.SalutationId = reader.GetSafeInt32(startingIndex++);
                p.FirstName = reader.GetSafeString(startingIndex++);
                p.MiddleInitial = reader.GetSafeString(startingIndex++);
                p.LastName = reader.GetSafeString(startingIndex++);
                p.Email = reader.GetSafeString(startingIndex++);
                p.PhoneNumber = reader.GetSafeString(startingIndex++);
                p.DateOfBirth = reader.GetSafeDateTime(startingIndex++);
                p.DateAdded = reader.GetSafeDateTime(startingIndex++);
                p.DateModified = reader.GetSafeDateTime(startingIndex++);
                p.AvatarFilePath = reader.GetSafeString(startingIndex++);
            });

            return p;
        }

        public List<UserRole> GetRoleId()
        {
            List<UserRole> list = null;

            DataProvider.ExecuteCmd(GetConnection, "dbo.Users_GetRoleId"
               , inputParamMapper: null
               , map: delegate (IDataReader reader, short set)
               {
                   UserRole p = new UserRole();
                   int startingIndex = 0;

                   p.RoleId = reader.GetSafeString(startingIndex++);
                   p.RoleName = reader.GetSafeString(startingIndex++);

                   if (list == null)
                   {
                       list = new List<UserRole>();
    }

                   list.Add(p);

}
               );

            return list;
        }

        public List<UserAvatar> GetAllUsers()
        {
            List<UserAvatar> list = null;

            DataProvider.ExecuteCmd(GetConnection, "dbo.Users_SelectAllUsers"
               , inputParamMapper: null
               , map: delegate (IDataReader reader, short set)
               {
                   UserAvatar p = new UserAvatar();
                   int startingIndex = 0;

                   p.Id = reader.GetSafeInt32(startingIndex++);
                   p.SalutationId = reader.GetSafeInt32(startingIndex++);
                   p.FirstName = reader.GetSafeString(startingIndex++);
                   p.MiddleInitial = reader.GetSafeString(startingIndex++);
                   p.LastName = reader.GetSafeString(startingIndex++);
                   p.DateOfBirth = reader.GetSafeDateTime(startingIndex++);
                   p.AvatarFilePath = reader.GetSafeString(startingIndex++);
                   p.AspNetUserId = reader.GetSafeString(startingIndex++);
                   p.Email = reader.GetSafeString(startingIndex++);

                   if (list == null)
                   {
                       list = new List<UserAvatar>();
                   }

                   list.Add(p);
               }
               );

            return list;
        }

        public List<UserRole> GetUserRoles(int id)
        {
            List<UserRole> list = null;
            DataProvider.ExecuteCmd(GetConnection, "dbo.Users_SelectUserRoles"
                , inputParamMapper: delegate (SqlParameterCollection paramCollection)
                {
                    paramCollection.AddWithValue("@Id", id);
                }
                , map: delegate (IDataReader reader, short set)
                {
                    UserRole u = new UserRole();
                    int startingIndex = 0;

                    u.RoleId = reader.GetSafeString(startingIndex++);
                    u.RoleName = reader.GetSafeString(startingIndex++);

                    if (list == null)
                    {
                        list = new List<UserRole>();
                    }

                    list.Add(u);
                }
                );
            return list;
        }
        //
        public List<UserRole> GetUserRoles(string aspNetUserId)
        {
            List<UserRole> list = null;
            DataProvider.ExecuteCmd(GetConnection, "dbo.Users_SelectUserRolesByStrId"
                , inputParamMapper: delegate (SqlParameterCollection paraCollection)
                {
                    paraCollection.AddWithValue("@AspUserId", aspNetUserId);
                }
                , map: delegate (IDataReader reader, short set)
                {
                    UserRole u = new UserRole();
                    int startingIndex = 0;

                    u.RoleId = reader.GetSafeString(startingIndex++);
                    u.RoleName = reader.GetSafeString(startingIndex++);

                    if (list == null)
                    {
                        list = new List<UserRole>();
                    }

                    list.Add(u);
                }

                );
            return list;
        }

        //
        public void DeleteRoleFromUser(int id, string roleId)
        {
            DataProvider.ExecuteNonQuery(GetConnection, "dbo.Users_DeleteUserRole"
                , inputParamMapper: delegate (SqlParameterCollection paraCollection)
                {
                    paraCollection.AddWithValue("@Id", id);
                    paraCollection.AddWithValue("@RoleId", roleId);
                });
        }

        public int InsertLegacy(UserAddLegacyRequest model)
        {
            int id = 0;

            DataProvider.ExecuteNonQuery(GetConnection, "dbo.Users_InsertByEditProfile"
               , inputParamMapper: delegate (SqlParameterCollection paramCollection)
               {
                   if (model != null)
                   {
                       paramCollection.AddWithValue("@SalutationId", model.SalutationId);
                       paramCollection.AddWithValue("@FirstName", model.FirstName);
                       paramCollection.AddWithValue("@MiddleInitial", model.MiddleInitial);
                       paramCollection.AddWithValue("@LastName", model.LastName);
                       paramCollection.AddWithValue("@AspNetUserId", model.userId);
                       paramCollection.AddWithValue("@PhoneNumber", model.PhoneNumber);
                       paramCollection.AddWithValue("@DateOfBirth", model.DateOfBirth);
                       paramCollection.AddWithValue("@Email", model.Email);
                   }

                   SqlParameter p = new SqlParameter("@Id", System.Data.SqlDbType.Int);
                   p.Direction = System.Data.ParameterDirection.Output;

                   paramCollection.Add(p);
               }, returnParameters: delegate (SqlParameterCollection param)
               {
                   int.TryParse(param["@Id"].Value.ToString(), out id);
               }
               );

            return id;
        }

        public void AddRoleToUser(string id, string roleId)
        {
            DataProvider.ExecuteNonQuery(GetConnection, "dbo.Users_InsertUserRole"
                , inputParamMapper: delegate (SqlParameterCollection paraCollection)
                {
                    paraCollection.AddWithValue("@UserId", id);
                    paraCollection.AddWithValue("@RoleId", roleId);
                });
        }

        public PagedList<UserAvatar> AdminDashboardPagedList(int pageIndex, int pageSize)
        {
            List<UserAvatar> list = null;
            PagedList<UserAvatar> page = null;
            int totalCount = 0;

            DataProvider.ExecuteCmd(GetConnection, "dbo.Users_SelectAllUsers"
              , inputParamMapper: delegate (SqlParameterCollection paramCollection)
              {
                  paramCollection.AddWithValue("@pageindex", pageIndex);
                  paramCollection.AddWithValue("@pagesize", pageSize);

              }, map: delegate (IDataReader reader, short set)
              {
                  UserAvatar p = new UserAvatar();

                  int startingIndex = 0;

                  p.Id = reader.GetSafeInt32(startingIndex++);
                  p.SalutationId = reader.GetSafeInt32(startingIndex++);
                  p.FirstName = reader.GetSafeString(startingIndex++);
                  p.MiddleInitial = reader.GetSafeString(startingIndex++);
                  p.LastName = reader.GetSafeString(startingIndex++);
                  p.DateOfBirth = reader.GetSafeDateTime(startingIndex++);
                  p.AvatarFilePath = reader.GetSafeString(startingIndex++);
                  p.AspNetUserId = reader.GetSafeString(startingIndex++);
                  p.Email = reader.GetSafeString(startingIndex++);

                  startingIndex = 9;

                  if (totalCount == 0)
                  {
                      totalCount = reader.GetSafeInt32(startingIndex++);
                  }

                  if (list == null)
                  {
                      list = new List<UserAvatar>();
                      page = new PagedList<UserAvatar>(list, pageIndex, pageSize, totalCount);

                      page.PagedItems = list;

                  }

                  list.Add(p);

              });

            return page;
        }


        public int InsertByAvatarProfile(AddUserbyAvatar model)
        {
            int id = 0;

            DataProvider.ExecuteNonQuery(GetConnection, "dbo.Users_InsertAvatarFilePath"
               , inputParamMapper: delegate (SqlParameterCollection paramCollection)
               {
                   if (model != null)
                   {
                       paramCollection.AddWithValue("@AspNetUserId", model.userId);
                      // paramCollection.AddWithValue("@AvatarFilePath", model.avatarFilePath);
                   }

                   SqlParameter p = new SqlParameter("@Id", System.Data.SqlDbType.Int);
                   p.Direction = System.Data.ParameterDirection.Output;

                   paramCollection.Add(p);
               }, returnParameters: delegate (SqlParameterCollection param)
               {
                   int.TryParse(param["@Id"].Value.ToString(), out id);
               }
               );

            return id;
        }


        public void InsertCustomUI(CustomUIRequest model)
        {
            DataProvider.ExecuteNonQuery(GetConnection, "dbo.CustomUI_Insert"
               , inputParamMapper: delegate (SqlParameterCollection paramCollection)
               {
                   if (model != null)
                   {
                       paramCollection.AddWithValue("@UserId", GetCurrentUserId());
                       paramCollection.AddWithValue("@CustomizationId", model.CustomizationId);
                       paramCollection.AddWithValue("@Value", model.Value);
                   }
    }
               );
}

        public List<CustomUIRequest> GetCustomUI()
        {
            List<CustomUIRequest> list = null;

            DataProvider.ExecuteCmd(GetConnection, "dbo.CustomUI_SelectByUserId"
            , inputParamMapper: delegate (SqlParameterCollection paramCollection)
            {
                paramCollection.AddWithValue("@UserId", GetCurrentUserId());
            }
            , map: delegate (IDataReader reader, short set)
            {
                CustomUIRequest p = new CustomUIRequest();
                int startingIndex = 1;

                p.CustomizationId = reader.GetSafeInt32(startingIndex++);
                p.Value = reader.GetSafeString(startingIndex++);

                if (list == null)
                {
                    list = new List<CustomUIRequest>();
                }
                list.Add(p);
            });
            return list;
        }

        public List<CustomUICondensedRequest> GetCustomUICondensed()
        {
            List<CustomUICondensedRequest> list = null;

            DataProvider.ExecuteCmd(GetConnection, "dbo.CustomUI_SelectByOptionId"
            , inputParamMapper: delegate (SqlParameterCollection paramCollection)
            {
                paramCollection.AddWithValue("@UserId", GetCurrentUserId());
}
            , map: delegate (IDataReader reader, short set)
            {
                CustomUICondensedRequest p = new CustomUICondensedRequest();
                int startingIndex = 1;

                p.JSName = reader.GetSafeString(startingIndex++);
                p.Value = reader.GetSafeString(startingIndex++);

                if (list == null)
                {
                    list = new List<CustomUICondensedRequest>();
                }
                list.Add(p);
            });
            return list;
        }

        public void UpdateCustomUI(CustomUIRequest model)
        {
            DataProvider.ExecuteNonQuery(GetConnection, "dbo.CustomUI_Update"
               , inputParamMapper: delegate (SqlParameterCollection paramCollection)
               {
                   paramCollection.AddWithValue("@UserId", GetCurrentUserId());
                   paramCollection.AddWithValue("@CustomizationId", model.CustomizationId);
                   paramCollection.AddWithValue("@Value", model.Value);
               });
        }

        public void DeleteCustomUI(CustomUIRequest model)
        {
            DataProvider.ExecuteNonQuery(GetConnection, "dbo.CustomUI_Delete"
                , inputParamMapper: delegate (SqlParameterCollection paramCollection)
                {
                    paramCollection.AddWithValue("@UserId", GetCurrentUserId());
                    paramCollection.AddWithValue("@CustomizationId", model.CustomizationId);
                });
        }
    }
}