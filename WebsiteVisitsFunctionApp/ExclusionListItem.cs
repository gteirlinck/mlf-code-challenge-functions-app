using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace WebsiteVisitsFunctionApp
{
    internal class ExclusionListItem
    {
        [BsonElement("host")]
        public string Host { get; set; }

        [BsonElement("excludedSince")]
        public DateTime ExcludedSince { get; set; }

        [BsonElement("excludedTill")]
        [BsonIgnoreIfNull]
        public DateTime? ExcludedTill { get; set; }
    }
}
