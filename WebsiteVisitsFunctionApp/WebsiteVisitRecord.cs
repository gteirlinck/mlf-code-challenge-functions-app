using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace WebsiteVisitsFunctionApp
{
    internal class WebsiteVisitRecord
    {
        public ObjectId Id { get; set; }

        [BsonElement("website")]
        public string Website { get; set; }

        [BsonElement("date")]
        public DateTime Date { get; set; }

        [BsonElement("visitsCount")]
        public int VisitsCount { get; set; }
    }
}
