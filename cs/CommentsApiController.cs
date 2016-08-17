using System.Net;
using System.Net.Http;
using System.Web.Http;
using RapidRents.Web.Models.Requests.Comments;
using RapidRents.Web.Models.Responses;
using RapidRents.Web.Services.Comments;
using RapidRents.Web.Services;
using RapidRents.Web.Domain;


namespace RapidRents.Web.Controllers.Api
{
    [RoutePrefix("api/comments")]
    public class CommentsApiController : ApiController
    {
        public IUserService _userService = null;

        private ICommentsService _service;
        public CommentsApiController(ICommentsService svc, IUserService UserService)
        {
            _service = svc;
            _userService = UserService;
        }

        
        [Route, HttpPost]
        public HttpResponseMessage Add(CommentAddRequest model)
        {
            string userId = _userService.GetCurrentUserId();

            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }

            ItemResponse<int> response = new ItemResponse<int>();

            response.Item = _service.Insert(model, userId);

            return Request.CreateResponse(response);
        }

        [Route("{id:int}"), HttpPut]
        public HttpResponseMessage Update(CommentUpdateRequest model, int id)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }

            SuccessResponse response = new SuccessResponse();

            _service.Update(model);

            return Request.CreateResponse(response);
        }

        [Route, HttpGet]
        public HttpResponseMessage GetList()
        {
            ItemsResponse<Comment> response = new ItemsResponse<Comment>();

            response.Items = _service.Get();

            return Request.CreateResponse(response);
        }

        [Route("{id:int}"), HttpGet]
        public HttpResponseMessage Get(int id)
        {
            ItemResponse<Comment> response = new ItemResponse<Comment>();

            response.Item = _service.GetById(id);

            return Request.CreateResponse(response);
        }


        [Route("{id:int}/get"), HttpGet]
        public HttpResponseMessage GetComm(int EntityId, string typeId)
        {
            ItemsResponse<Comment> response = new ItemsResponse<Comment>();

            response.Items = _service.GetById_typeId(EntityId, typeId);

            return Request.CreateResponse(response);
        }

        [Route("{id:int}"), HttpDelete]
        public HttpResponseMessage DeleteById(int id)
        {
            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }

            _service.Delete(id);

            SuccessResponse response = new SuccessResponse();

            return Request.CreateResponse(response);
        }
    }
}
