using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Serverless.Worker.Providers;

namespace Serverless.Worker.Controllers
{
    public class ContainersController : ApiController
    {
        public async Task<HttpResponseMessage> Delete(string containerName)
        {
            if (!MemoryProvider.ReservationExists(containerName: containerName))
            {
                return this.Request.CreateResponse(statusCode: HttpStatusCode.Gone);
            }

            await ContainerProvider
                .DeleteContainerIfExists(containerName: containerName)
                .ConfigureAwait(continueOnCapturedContext: false);

            return this.Request.CreateResponse(statusCode: HttpStatusCode.NoContent);
        }
    }
}
