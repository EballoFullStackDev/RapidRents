using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using RapidRents.Web.Domain;
using RapidRents.Web.Domain.Users;
using RapidRents.Web.Exceptions;
using RapidRents.Web.Models;
using RapidRents.Web.Models.Requests.ForgotPassword;
using RapidRents.Web.Models.Requests.Register;
using RapidRents.Web.Models.Requests.SignIn;
using RapidRents.Web.Models.Requests.Users;
using RapidRents.Web.Models.Responses;
using RapidRents.Web.Services;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;


namespace RapidRents.Web.Controllers.Api
{
    [RoutePrefix("api/users")]
    public class UsersApiController : ApiController
    {
        private IUserService _userService;

        public UsersApiController(IUserService UserService)
        {
            _userService = UserService;
        }

        [Route, HttpPost]
        public HttpResponseMessage AddUser(UserAddRequest model)
        {
            string userId = _userService.GetCurrentUserId();
            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }

            ItemResponse<int> response = new ItemResponse<int>();

            response.Item = _userService.Insert(model);

            return Request.CreateResponse(response);
        }
        
        [Route("add/legacy"), HttpPost]
        public HttpResponseMessage AddLegacyUser(UserAddLegacyRequest model)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }

            ItemResponse<int> response = new ItemResponse<int>();

            response.Item = _userService.InsertLegacy(model);

            return Request.CreateResponse(response);
        }

        [Route("{id:int}"), HttpPut]
        public HttpResponseMessage UpdateUser(UserUpdateRequest model)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }

            SuccessResponse response = new SuccessResponse();

            _userService.Update(model);

            return Request.CreateResponse(response);
        }

        [Route, HttpGet]
        public HttpResponseMessage GetUsers()
        {
            ItemsResponse<User> response = new ItemsResponse<User>();

            response.Items = _userService.Get();

            return Request.CreateResponse(response);
        }

        [Route("{id:int}"), HttpGet]
        public HttpResponseMessage GetUserById(int Id)
        {
            ItemResponse<User> response = new ItemResponse<User>();

            response.Item = _userService.GetUserById(Id);

            return Request.CreateResponse(response);
        }

        [Route("{id:int}"), HttpDelete]
        public HttpResponseMessage DeleteUserById(int Id)
        {
            SuccessResponse response = new SuccessResponse();

            _userService.DeleteUserById(Id);

            return Request.CreateResponse(response);
        }

        [Route("current"), HttpGet]
        public HttpResponseMessage GetUserByAspNetUserId()
        {
            string userId = User.Identity.GetUserId();

            ItemResponse<User> response = new ItemResponse<User>();
            response.Item = _userService.GetUser(userId);

            return Request.CreateResponse(response);
        }

        [Route("logout"), HttpPost]
        public HttpResponseMessage LogoutUser()
        {
            ItemResponse<bool> response = new ItemResponse<bool>();

            response.Item = _userService.Logout();

            return Request.CreateResponse(response);
        }

        [Route("verify"), HttpPost]
        public async Task<HttpResponseMessage> Login(SignInRequest model)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ModelState);
            }

            ItemResponse<bool> response = new ItemResponse<bool>();
            try
            {
            response.Item = await _userService.Signin(model);
                return Request.CreateResponse(HttpStatusCode.OK, response);
            }
            catch (Exception arg)  
            {
                Results result = new Results();
                result.Message = arg.Message;
                result.Success = false;
                return Request.CreateResponse(HttpStatusCode.Forbidden, result);
            }
        }

        [Route("currentUser"), HttpGet]
        public HttpResponseMessage GetCurrentUser()
        {

            if (!ModelState.IsValid)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ModelState);
            }
            ItemResponse<IdentityUser> response = new ItemResponse<IdentityUser>();

            response.Item = _userService.GetCurrentUser();

            return Request.CreateResponse(response);
        }

        [Route("forgot"), HttpPost]
        public async Task<HttpResponseMessage> ForgotPassword(ForgotPasswordRequest model)
        {

            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }

            ItemResponse<bool> response = new ItemResponse<bool>();

            response.Item = await _userService.RecoverPassword(model.EmailAddress);

            return Request.CreateResponse(response);
        }

        [Route("resetpassword"), HttpPut]
        public async Task<HttpResponseMessage> UpdatePassword(UpdatePasswordRequest model)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }

            await _userService.ChangePassword(model.UserId, model.Code, model.Password);

            SuccessResponse response = new SuccessResponse();

            return Request.CreateResponse(response);
        }

        [Route("dashboard/{PageIndex:int}/{PageSize:int}"), HttpGet]
        public HttpResponseMessage GetAllUsers(int PageIndex, int PageSize)
        {
            ItemResponse<PagedList<UserAvatar>> response = new ItemResponse<PagedList<UserAvatar>>();

            response.Item = _userService.AdminDashboardPagedList(PageIndex, PageSize);

            return Request.CreateResponse(response);
        }

        [Route("dashboard/{id:int}/{roleId}"), HttpDelete]
        public HttpResponseMessage DeleteRoleFromUser(int id, string roleId)
        {
            SuccessResponse response = new SuccessResponse();
                
            _userService.DeleteRoleFromUser(id, roleId);

            return Request.CreateResponse(response);
        }

        [Route("dashboard/{id}/{roleId}"), HttpPost]
        public HttpResponseMessage AddRoleToUser(string id, string roleId)
        {
            SuccessResponse response = new SuccessResponse();

            _userService.AddRoleToUser(id, roleId);

            return Request.CreateResponse(response);
        }

        [Route("register"), HttpPost]
        public HttpResponseMessage Create(RegisterRequest model)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }

            SuccessResponse response = new SuccessResponse();

            try
            {
                _userService.CreateUser(model);


                return Request.CreateResponse(new SuccessResponse());

            }
            catch (IdentityResultException ier)
            {
                ErrorResponse err = new ErrorResponse(ier.Result.Errors);

                return Request.CreateResponse(HttpStatusCode.InternalServerError, err);
            }

            catch (Exception ex)
            {
                ErrorResponse err = new ErrorResponse(ex.Message);

                return Request.CreateResponse(HttpStatusCode.InternalServerError, err);
            }

        }

        [Route("getroles"), HttpGet]
        public HttpResponseMessage GetRoleId()
        {
            ItemsResponse<UserRole> response = new ItemsResponse<UserRole>();

            response.Items = _userService.GetRoleId();

            return Request.CreateResponse(response);
        }
       
        [Route("roles/{id:int?}"), HttpGet]
        public HttpResponseMessage GetUserRoles(int id) 
        {

            ItemsResponse<UserRole> response = new ItemsResponse<UserRole>();

            response.Items = _userService.GetUserRoles(id);

            return Request.CreateResponse(response);

        }

        [Route("customui"), HttpPost]
        public HttpResponseMessage AddCustomUI(CustomUIRequest model)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }

            SuccessResponse response = new SuccessResponse();

            _userService.InsertCustomUI(model);

            return Request.CreateResponse(response);
        }

        [Route("add/photo"), HttpPost]
        public HttpResponseMessage AddUserByAvatarPhoto(AddUserbyAvatar model)
        {
            model.userId = _userService.GetCurrentUserId();
            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }


            ItemResponse<int> response = new ItemResponse<int>();

            response.Item = _userService.InsertByAvatarProfile(model);

            return Request.CreateResponse(response);
        }
        [Route("customui/all"), HttpGet]
        public HttpResponseMessage GetCustomUIUserId()
        {
            ItemsResponse<CustomUIRequest> response = new ItemsResponse<CustomUIRequest>();

            response.Items = _userService.GetCustomUI();
       
            return Request.CreateResponse(response);
        }

        [Route("customui"), HttpGet]
        public HttpResponseMessage GetCondensedCustomUI()
        {
            ItemsResponse<CustomUICondensedRequest> response = new ItemsResponse<CustomUICondensedRequest>();

            response.Items = _userService.GetCustomUICondensed();

            return Request.CreateResponse(response);
        }

        [Route("customui"), HttpPut]
        public HttpResponseMessage PutCustomUI(CustomUIRequest model)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }

            SuccessResponse response = new SuccessResponse();

            _userService.UpdateCustomUI(model);

            return Request.CreateResponse(response);
        }

        [Route("customui"), HttpDelete]
        public HttpResponseMessage DelCustomUI(CustomUIRequest model)
        {
            SuccessResponse response = new SuccessResponse();

            _userService.DeleteCustomUI(model);
       
            return Request.CreateResponse(response);
        }
    }
}
