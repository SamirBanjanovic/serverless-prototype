using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using Serverless.Common.Configuration;
using Serverless.Web.Entities;
using Serverless.Web.Extensions;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Serverless.Web.Providers
{
    public static class FunctionsProvider
    {
        public static async Task<IEnumerable<Function>> List()
        {
            var functionsTable = await FunctionsProvider
                .GetFunctionsTable()
                .ConfigureAwait(continueOnCapturedContext: false);

            var rangeQuery = new TableQuery<Function>().Where(TableQuery.GenerateFilterCondition(
                propertyName: "PartitionKey",
                operation: QueryComparisons.Equal,
                givenValue: "functions"));

            return await functionsTable
                .ExecuteQueryAsync(query: rangeQuery)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public static async Task<Function> Get(string functionId)
        {
            var functionsTable = await FunctionsProvider
                .GetFunctionsTable()
                .ConfigureAwait(continueOnCapturedContext: false);

            var retrieveOperation = TableOperation.Retrieve<Function>(
                partitionKey: "functions",
                rowkey: functionId);

            var tableResult = await functionsTable
                .ExecuteAsync(operation: retrieveOperation)
                .ConfigureAwait(continueOnCapturedContext: false);

            return (Function)tableResult.Result;
        }

        public static async Task Create(Function function)
        {
            var functionsTable = await FunctionsProvider
                .GetFunctionsTable()
                .ConfigureAwait(continueOnCapturedContext: false);

            await functionsTable
                .ExecuteAsync(operation: TableOperation.Insert(entity: function))
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public static async Task Replace(Function function)
        {
            var functionsTable = await FunctionsProvider
                .GetFunctionsTable()
                .ConfigureAwait(continueOnCapturedContext: false);

            try
            {
                await functionsTable
                    .ExecuteAsync(operation: TableOperation.Replace(entity: function))
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
            catch (StorageException exception)
            {
                if (exception.RequestInformation.HttpStatusCode == 404)
                {
                    throw new HttpResponseException(statusCode: HttpStatusCode.NotFound);
                }

                throw;
            }
        }

        public static async Task Delete(string functionId)
        {
            var functionsTable = await FunctionsProvider
                .GetFunctionsTable()
                .ConfigureAwait(continueOnCapturedContext: false);

            var deleteOperation = TableOperation.Delete(entity: Function.FromId(functionId: functionId));
            
            try
            {
                await functionsTable
                    .ExecuteAsync(operation: deleteOperation)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
            catch (StorageException exception)
            {
                if (exception.RequestInformation.HttpStatusCode == 404)
                {
                    throw new HttpResponseException(statusCode: HttpStatusCode.NotFound);
                }

                throw;
            }
        }

        private static async Task<CloudTable> GetFunctionsTable()
        {
            var tableClient = ServerlessConfiguration.StorageAccount.CreateCloudTableClient();

            var functionsTable = tableClient.GetTableReference(tableName: "functions");

            await functionsTable
                .CreateIfNotExistsAsync()
                .ConfigureAwait(continueOnCapturedContext: false);

            return functionsTable;
        }
    }
}