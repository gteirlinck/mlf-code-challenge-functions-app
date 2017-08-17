using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using System;
using MongoDB.Driver;
using System.Threading;

namespace WebsiteVisitsFunctionApp
{
    public static class UploadRecords
    {
        [FunctionName("UploadRecords")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "upload-records")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("Starting UploadRecords function");

            // Get request body
            var data = await req.Content.ReadAsAsync<List<WebsiteVisitRecord>>();

            if (data?.Count > 0)
            {
                var result = await ProcessRecords(data, log);
                var statusCode = result.success ? HttpStatusCode.OK : HttpStatusCode.InternalServerError;
                return req.CreateResponse(statusCode, result.message);
            }
            else
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Invalid body. Expected an array of WebsiteVisitRecord");
            }
        }

        private static async Task<(bool success, string message)> ProcessRecords(IEnumerable<WebsiteVisitRecord> records, TraceWriter log)
        {
            // The function should not take more than 4 minutes to process (by default Azure functions time out after 5 minutes)
            CancellationTokenSource cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMinutes(4));

            try
            {
                log.Info($"Preparing to process {records.Count()} records");

                var collection = MongoDBHelper.GetWebsiteVisitsRecordsCollection();

                int insertedCount = 0;
                int skippedCount = 0;

                foreach (var record in records)
                {
                    // Search if a record already exists for the same website/date: if such a record exists then we skip it, otherwise we add a new record
                    var fdb = Builders<WebsiteVisitRecord>.Filter;
                    var filter = fdb.Eq("website", record.Website) & fdb.Eq("date", record.Date);

                    var existing = await collection.Find(filter).SingleOrDefaultAsync(cancellationToken: cts.Token);

                    if (existing != null)
                    {
                        log.Info($"Skipping record {record.Website}-{record.Date.ToShortDateString()}: already in database");
                        skippedCount++;
                    }
                    else
                    {
                        log.Info($"Inserting new record {record.Website}-{record.Date.ToShortDateString()}");
                        await collection.InsertOneAsync(record, cancellationToken: cts.Token);
                        insertedCount++;
                    }
                }

                return (true, $"Successfully added {insertedCount} new records in database. {skippedCount} existing records were skipped");
            }
            catch (OperationCanceledException)
            {
                string err = "Not inserting records in DB: operation cancelled";
                log.Error(err);
                return (false, err);
            }
            catch (ArgumentException ex)
            {
                string err = $"Not inserting records in DB: parameter {ex.ParamName} is missing from config";
                log.Error(err, ex);
                return (false, err);
            }
            catch (Exception ex)
            {
                string err = "Failed to insert records in DB";
                log.Error(err, ex);
                return (false, $"{err}: {ex.Message}");
            }
        }
    }
}
