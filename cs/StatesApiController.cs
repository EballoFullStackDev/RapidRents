using RapidRents.Web.Domain.State;
using RapidRents.Web.Models.Requests.State;
using RapidRents.Web.Models.Responses;
using RapidRents.Web.Services.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace RapidRents.Web.Controllers.Api
{
    [RoutePrefix("api/states")]
    public class StatesApiController : ApiController
    {
        private IStatesService _service;
        public StatesApiController(IStatesService svc)
        {
            _service = svc;
        }

        [Route, HttpPost]
        public HttpResponseMessage GetStates(StatesGetByCountryCode model)
        {

            if (!ModelState.IsValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState);
            }

            ItemsResponse<Territory> response = new ItemsResponse<Territory>();

            response.Items = _service.GetByCountryCode(model.CountryRegionCode);

            return Request.CreateResponse(response);
        }
    }
}
