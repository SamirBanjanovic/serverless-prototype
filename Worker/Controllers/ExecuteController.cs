using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Serverless.Common.Models;
using Serverless.Worker.Providers;

namespace Serverless.Worker.Controllers
{
    public class ExecuteController : ApiController
    {
        public async Task<HttpResponseMessage> Post(string containerName, [FromBody]ExecutionRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var logs = new List<ExecutionLog>();

            var reservationExists = await MemoryProvider
                .ReservationExists(containerName: containerName)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (!reservationExists)
            {
                return this.Request.CreateResponse(statusCode: HttpStatusCode.NotFound);
            }

            var downloaded = await CodeProvider
                .DownloadIfNotExists(
                    functionId: request.Function.Id,
                    blobUri: request.Function.BlobUri)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (downloaded)
            {
                logs.Add(new ExecutionLog
                {
                    Name = "DownloadCodeIfNotExists",
                    Duration = stopwatch.ElapsedMilliseconds - (logs.Count > 0 ? logs.Last().Duration : 0)
                });
            }

            var created = await ContainerProvider
                .CreateContainerIfNotExists(
                    containerName: containerName,
                    functionId: request.Function.Id,
                    memorySize: request.Function.MemorySize)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (created)
            {
                logs.Add(new ExecutionLog
                {
                    Name = "CreateContainerIfNotExists",
                    Duration = stopwatch.ElapsedMilliseconds - (logs.Count > 0 ? logs.Last().Duration : 0)
                });
            }

            var container = await ContainerProvider
                .TryReserve(containerName: containerName)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (container == null)
            {
                return this.Request.CreateResponse(statusCode: HttpStatusCode.NotFound);
            }

            var response = await container
                .Execute(request: request)
                .ConfigureAwait(continueOnCapturedContext: false);

            logs.Add(new ExecutionLog
            {
                Name = "Execute",
                Duration = stopwatch.ElapsedMilliseconds - (logs.Count > 0 ? logs.Last().Duration : 0)
            });

            await ContainerProvider
                .ReleaseContainer(containerName: containerName)
                .ConfigureAwait(continueOnCapturedContext: false);

            MemoryProvider.SendWarmReservation(
                queueName: request.Function.Id,
                containerName: containerName);

            response.Logs = new ExecutionLog
            {
                Name = "ExecuteController",
                Duration = stopwatch.ElapsedMilliseconds,
                SubLogs = logs.ToArray()
            };

            return this.Request.CreateResponse(
                statusCode: HttpStatusCode.OK,
                value: response);
        }
    }
}
