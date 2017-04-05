using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Serverless.Common.Entities;
using Serverless.Common.Providers;
using Serverless.Web.Extensions;
using Serverless.Web.Models;

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

        public async Task<HttpResponseMessage> Delete()
        {
            var functions = await FunctionsProvider
                .List()
                .ConfigureAwait(continueOnCapturedContext: false);

            await Task
                .WhenAll(functions.Select(async function =>
                {
                    await FunctionsProvider
                        .Delete(functionId: function.Id)
                        .ConfigureAwait(continueOnCapturedContext: false);

                    await QueueProvider
                        .DeleteQueue(queueName: function.Id)
                        .ConfigureAwait(continueOnCapturedContext: false);
                }))
                .ConfigureAwait(continueOnCapturedContext: false);

            return this.Request.CreateResponse(statusCode: HttpStatusCode.NoContent);
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

        public async Task<HttpResponseMessage> Post([FromBody]FunctionUpload upload)
        {
            var function = await upload
                .ToFunction()
                .ConfigureAwait(continueOnCapturedContext: false);

            await FunctionsProvider
                .Create(function: function)
                .ConfigureAwait(continueOnCapturedContext: false);

            var response = this.Request.CreateResponse(
                value: function.ToModel(),
                statusCode: HttpStatusCode.Created);

            response.Headers.Location = this.Request.RequestUri.Append(suffix: function.Id);

            return response;
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
