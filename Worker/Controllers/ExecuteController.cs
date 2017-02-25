using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Serverless.Common.Models;
using Serverless.Worker.Entities;
using Serverless.Worker.Providers;

namespace Serverless.Worker.Controllers
{
    public class ExecuteController : ApiController
    {
        public async Task<HttpResponseMessage> Post(string containerName, [FromBody]ExecutionRequest request)
        {
            if (!MemoryProvider.ReservationExists(containerName: containerName))
            {
                return this.Request.CreateResponse(statusCode: HttpStatusCode.Gone);
            }

            await CodeProvider
                .DownloadIfNotExists(
                    functionId: request.Function.Id,
                    blobUri: request.Function.BlobUri)
                .ConfigureAwait(continueOnCapturedContext: false);

            var created = await ContainerProvider
                .CreateContainerIfNotExists(
                    containerName: containerName,
                    functionId: request.Function.Id,
                    memorySize: request.Function.MemorySize)
                .ConfigureAwait(continueOnCapturedContext: false);

            Container container;
            if (!ContainerProvider.TryReserve(containerName, out container))
            {
                return this.Request.CreateResponse(statusCode: HttpStatusCode.Gone);
            }

            var response = await container
                .Execute(request: request)
                .ConfigureAwait(continueOnCapturedContext: false);

            ContainerProvider.ReleaseContainer(containerName: containerName);

            if (created)
            {
                MemoryProvider.SendReservation(
                    queueName: request.Function.Id,
                    containerName: containerName);
            }

            return this.Request.CreateResponse(
                statusCode: HttpStatusCode.OK,
                value: response);
        }
    }
}
