using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Serverless.Common.Extensions
{
    public static class CloudTableExtensions
    {
        public static async Task<IEnumerable<T>> ExecuteQueryAsync<T>(this CloudTable table, TableQuery<T> query) where T : ITableEntity, new()
        {
            var result = new List<T>();
            TableContinuationToken token = null;

            do
            {
                var segment = await table
                    .ExecuteQuerySegmentedAsync(
                        query: query,
                        token: token)
                    .ConfigureAwait(continueOnCapturedContext: false);

                result.AddRange(segment);
                token = segment.ContinuationToken;
            }
            while (token != null);

            return result;
        }
    }
}