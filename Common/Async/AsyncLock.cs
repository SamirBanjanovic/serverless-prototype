using System;
using System.Threading;
using System.Threading.Tasks;

namespace Serverless.Common.Async
{
    public class AsyncLock
    {
        private SemaphoreSlim Semaphore { get; set; }

        public AsyncLock()
        {
            this.Semaphore = new SemaphoreSlim(initialCount: 1);
        }

        public async Task<AsyncLockReleaser> WaitAsync()
        {
            await this.Semaphore
                .WaitAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            return new AsyncLockReleaser(semaphore: this.Semaphore);
        }

        public class AsyncLockReleaser : IDisposable
        {
            private SemaphoreSlim Semaphore { get; set; }

            public AsyncLockReleaser(SemaphoreSlim semaphore)
            {
                this.Semaphore = semaphore;
            }

            public void Dispose()
            {
                this.Semaphore.Release();
            }
        }
    }
}
