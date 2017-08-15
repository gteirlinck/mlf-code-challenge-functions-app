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

namespace WebsiteVisitsFunctionApp
{
    public static class UploadRecords
    {
        [FunctionName("UploadRecords")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "upload-records")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

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
            string connString = Environment.GetEnvironmentVariable("MongoDB:ConnectionString");

            if (string.IsNullOrEmpty(connString))
                return (false, "Failed to get MongoDB connection string from configuration");

            string dbName = Environment.GetEnvironmentVariable("MongoDB:DBName");

            if (string.IsNullOrEmpty(dbName))
                return (false, "Failed to get MongoDB DB name from configuration");

            string collectionName = Environment.GetEnvironmentVariable("MongoDB:WebsiteVisitsRecordsCollection");

            if (string.IsNullOrEmpty(collectionName))
                return (false, "Failed to get collection name from configuration");

            try
            {
                MongoClient mongoClient = new MongoClient(connString);
                var db = mongoClient.GetDatabase(dbName);

                var collection = db.GetCollection<WebsiteVisitRecord>(collectionName);

                foreach (var record in records)
                {
                    // Search if a record already exists for the same website/date: if such a record exists then we update the visits count, otherwise we add a new record

                    var fdb = Builders<WebsiteVisitRecord>.Filter;
                    var filter = fdb.Eq("website", record.Website) & fdb.Eq("date", record.Date);

                    var update = Builders<WebsiteVisitRecord>.Update.Set("visitsCount", record.VisitsCount);

                    await collection.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<WebsiteVisitRecord, WebsiteVisitRecord>() { IsUpsert = true });
                }

                return (true, $"Successfully added/updated {records.Count()} in database");
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
