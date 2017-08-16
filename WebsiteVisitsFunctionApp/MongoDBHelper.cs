using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Security.Authentication;

namespace WebsiteVisitsFunctionApp
{
    internal static class MongoDBHelper
    {
        private const string MongoHostParamName = "MongoDB:Host";
        private const string MongoPortParamName = "MongoDB:Port";
        private const string MongoUserParamName = "MongoDB:User";
        private const string MongoPasswordParamName = "MongoDB:Password";
        private const string MongoDbNameParamName = "MongoDB:DBName";
        private const string MongoExclusionsListCollectionName = "MongoDB:ExclusionsCollection";
        private const string MongoWebsiteVisitsRecordsCollectionName = "MongoDB:WebsiteVisitsRecordsCollection";

        private static IMongoDatabase GetDatabase()
        {
            string dbHost = Environment.GetEnvironmentVariable(MongoHostParamName);

            if (string.IsNullOrEmpty(dbHost))
                throw new ArgumentException(MongoHostParamName);

            if (!int.TryParse(Environment.GetEnvironmentVariable(MongoPortParamName), out int dbPort))
                throw new ArgumentException(MongoPortParamName);

            string dbUser = Environment.GetEnvironmentVariable(MongoUserParamName);
            if (string.IsNullOrEmpty(dbUser))
                throw new ArgumentException(MongoUserParamName);

            string dbPassword = Environment.GetEnvironmentVariable(MongoPasswordParamName);
            if (string.IsNullOrEmpty(dbPassword))
                throw new ArgumentException(MongoPasswordParamName);

            string dbName = Environment.GetEnvironmentVariable(MongoDbNameParamName);

            if (string.IsNullOrEmpty(dbName))
                throw new ArgumentException(MongoDbNameParamName);

            //MongoClient mongoClient = new MongoClient(connString);

            MongoClientSettings settings = new MongoClientSettings
            {
                Server = new MongoServerAddress(dbHost, dbPort),
                UseSsl = true,
                SslSettings = new SslSettings()
                {
                    EnabledSslProtocols = SslProtocols.Tls12
                },
                Credentials = new List<MongoCredential>()
                {
                    new MongoCredential("SCRAM-SHA-1", new MongoInternalIdentity(dbName, dbUser), new PasswordEvidence(dbPassword))
                }
            };

            MongoClient mongoClient = new MongoClient(settings);
            return mongoClient.GetDatabase(dbName);
        }

        public static IMongoCollection<ExclusionListItem> GetExclusionsListCollection()
        {
            string collectionName = Environment.GetEnvironmentVariable(MongoExclusionsListCollectionName);

            if (string.IsNullOrEmpty(collectionName))
                throw new ArgumentException(MongoExclusionsListCollectionName);

            var db = GetDatabase();

            return db.GetCollection<ExclusionListItem>(collectionName);
        }

        public static IMongoCollection<WebsiteVisitRecord> GetWebsiteVisitsRecordsCollection()
        {
            string collectionName = Environment.GetEnvironmentVariable(MongoWebsiteVisitsRecordsCollectionName);

            if (string.IsNullOrEmpty(collectionName))
                throw new ArgumentException(MongoWebsiteVisitsRecordsCollectionName);

            var db = GetDatabase();

            return db.GetCollection<WebsiteVisitRecord>(collectionName);
        }
    }
}
