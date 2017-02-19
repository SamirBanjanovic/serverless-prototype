using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Serverless.Web.Entities;
using Serverless.Web.Extensions;
using Serverless.Web.Models;
using Serverless.Web.Providers;

namespace Serverless.Web.Controllers
{
    public class FunctionsController : ApiController
    {
        public async Task<HttpResponseMessage> Get()
        {
            var functions = await FunctionsProvider
                .List()
                .ConfigureAwait(continueOnCapturedContext: false);

            return this.Request.CreateResponse(
                value: functions.Select(function => function.ToModel()),
                statusCode: HttpStatusCode.OK);
        }

        public async Task<HttpResponseMessage> Get(string functionId)
        {
            var function = await FunctionsProvider
                .Get(functionId: functionId)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (function == null)
            {
                return this.Request.CreateResponse(statusCode: HttpStatusCode.NotFound);
            }

            return this.Request.CreateResponse(
                value: function.ToModel(),
                statusCode: HttpStatusCode.OK);
        }

        public async Task<HttpResponseMessage> Post([FromBody]FunctionModel function)
        {
            function.Id = Guid.NewGuid().ToString();
            function.DeploymentId = Guid.NewGuid().ToString();

            await FunctionsProvider
                .Create(function: Function.FromModel(model: function))
                .ConfigureAwait(continueOnCapturedContext: false);

            var response = this.Request.CreateResponse(
                value: function,
                statusCode: HttpStatusCode.Created);

            response.Headers.Location = this.Request.RequestUri.Append(suffix: function.Id);

            return response;
        }

        public async Task<HttpResponseMessage> Put(string functionId, [FromBody]FunctionModel function)
        {
            function.Id = functionId;
            function.DeploymentId = Guid.NewGuid().ToString();

            await FunctionsProvider
                .Replace(function: Function.FromModel(model: function))
                .ConfigureAwait(continueOnCapturedContext: false);

            return this.Request.CreateResponse(statusCode: HttpStatusCode.NoContent);
        }

        public async Task<HttpResponseMessage> Delete(string functionId)
        {
            await FunctionsProvider
                .Delete(functionId: functionId)
                .ConfigureAwait(continueOnCapturedContext: false);

            return this.Request.CreateResponse(statusCode: HttpStatusCode.NoContent);
        }
    }
}
