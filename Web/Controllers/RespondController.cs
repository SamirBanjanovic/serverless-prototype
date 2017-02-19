using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Serverless.Web.Models;
using Serverless.Web.Providers;

namespace Serverless.Web.Controllers
{
    public class RespondController : ApiController
    {
        public HttpResponseMessage Post(string executionId, [FromBody]ExecutionResponse response)
        {
            ExecutionProvider.Respond(
                executionId: executionId,
                response: response);

            return this.Request.CreateResponse(statusCode: HttpStatusCode.OK);
        }
    }
}