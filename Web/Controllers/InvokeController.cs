﻿using System;
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

            var function = await FunctionsProvider
                .Get(functionId: functionId)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (function == null)
            {
                return this.Request.CreateResponse(statusCode: HttpStatusCode.NotFound);
            }

            var request = new ExecutionRequest
            {
                ExecutionId = Guid.NewGuid().ToString(),
                Input = input
            };

            var response = await ExecutionProvider
                .Execute(
                    function: function,
                    request: request)
                .ConfigureAwait(continueOnCapturedContext: false);

            response.Logs = new ExecutionLog
            {
                Name = "InvokeController.Post",
                Duration = stopwatch.ElapsedMilliseconds,
                SubLogs = new[] { response.Logs }
            };

            return this.Request.CreateResponse(
                statusCode: HttpStatusCode.OK,
                value: response);
        }
    }
}
