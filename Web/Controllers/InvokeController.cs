using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json.Linq;
using Serverless.Common.Models;
using Serverless.Web.Providers;

namespace Serverless.Web.Controllers
{
    public class InvokeController : ApiController
    {
        public async Task<HttpResponseMessage> Post(string functionId, [FromBody]JToken input)
        {
            var stopwatch = Stopwatch.StartNew();
            var logs = new List<ExecutionLog>();

            var function = await FunctionsProvider
                .Get(functionId: functionId)
                .ConfigureAwait(continueOnCapturedContext: false);

            logs.Add(new ExecutionLog
            {
                Name = "GetFunction",
                Duration = stopwatch.ElapsedMilliseconds
            });

            if (function == null)
            {
                return this.Request.CreateResponse(statusCode: HttpStatusCode.NotFound);
            }

            var request = new ExecutionRequest
            {
                Function = function.ToModel(),
                Input = input
            };

            var response = await ExecutionProvider
                .Execute(request: request)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (response == null)
            {
                return this.Request.CreateResponse(statusCode: HttpStatusCode.ServiceUnavailable);
            }

            logs.Add(response.Logs);

            response.Logs = new ExecutionLog
            {
                Name = "InvokeController",
                Duration = stopwatch.ElapsedMilliseconds,
                SubLogs = logs.ToArray()
            };

            return this.Request.CreateResponse(
                statusCode: HttpStatusCode.OK,
                value: response);
        }
    }
}
