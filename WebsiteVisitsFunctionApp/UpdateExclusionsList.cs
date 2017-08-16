using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Flurl.Http;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson;

namespace WebsiteVisitsFunctionApp
{
    public static class UpdateExclusionsList
    {
        private const string ExclusionsListBackendAddressParamName = "Exclusions:BackendAddress";
        [FunctionName("UpdateExclusionsList")]
        public static async Task Run([TimerTrigger("0 0 6 * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info("Starting UpdateExclusionsList function");

            // The function should not take more than 4 minutes to process (by default Azure functions time out after 5 minutes)
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMinutes(4));

            var exclusionsListResult = await LoadExclusionListFromBackend(log, cts.Token);

            if (exclusionsListResult.success)
            {
                var dbProcessResult = await UpdateExclusionsListInDB(exclusionsListResult.exclusionsList, log, cts.Token);

                if (!dbProcessResult.success)
                    log.Error($"Failed to update database with new exclusions list: {dbProcessResult.message}");
            }
            else
                log.Error($"Failed to load exclusions list items from backend. Not updating anything in database: {exclusionsListResult.message}");
        }

        private static async Task<(bool success, string message, List<ExclusionListItem> exclusionsList)> LoadExclusionListFromBackend(TraceWriter log, CancellationToken ct = default(CancellationToken))
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                string backendAddress = Environment.GetEnvironmentVariable(ExclusionsListBackendAddressParamName);

                if (string.IsNullOrEmpty(backendAddress))
                    throw new ArgumentException(ExclusionsListBackendAddressParamName);

                var result = await backendAddress.GetAsync(ct).ReceiveJson<List<ExclusionListItem>>();

                return (true, "", result);
            }
            catch (OperationCanceledException)
            {
                string err = "operation cancelled";
                log.Error(err);
                return (false, err, null);
            }
            catch (ArgumentException ex)
            {
                string err = $"parameter {ex.ParamName} is missing from config";
                log.Error(err, ex);
                return (false, err, null);
            }
            catch (Exception ex)
            {
                log.Error("Caught exception", ex);
                return (false, ex.Message, null);
            }

        }

        private static async Task<(bool success, string message)> UpdateExclusionsListInDB(IEnumerable<ExclusionListItem> exclusionsList, TraceWriter log, CancellationToken ct = default(CancellationToken))
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                log.Info($"Preparing to process {exclusionsList?.Count() ?? 0} exclusions list items");

                var collection = MongoDBHelper.GetExclusionsListCollection();

                // Since we only want to keep the latest state of the exclusions list in the database we can simply delete all existing records and insert new ones
                // This is faster than scanning each record to see if it needs to be added/updated/dropped

                // 1. Delete existing records (if any)
                var deleteResult = await collection.DeleteManyAsync(new BsonDocument(), ct);
                log.Info($"Deleted {deleteResult?.DeletedCount ?? 0} existing exclusions list items from database");

                // 2. Insert new records (if any)
                if (exclusionsList?.Count() > 0)
                {
                    await collection.InsertManyAsync(exclusionsList, cancellationToken: ct);
                    log.Info($"Successfully inserted {exclusionsList.Count()} exclusions list items in database");
                }
                else
                    log.Info("No new exclusions list item to insert");

                return (true, "");
            }
            catch (OperationCanceledException)
            {
                string err = "operation cancelled";
                log.Error(err);
                return (false, err);
            }
            catch (ArgumentException ex)
            {
                string err = $"parameter {ex.ParamName} is missing from config";
                log.Error(err, ex);
                return (false, err);
            }
            catch (Exception ex)
            {
                log.Error("Caught exception", ex);
                return (false, ex.Message);
            }
        }
    }
}
