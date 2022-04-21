using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Novikov.MongoRepository
{
    [Serializable]
    public abstract class MongoEntity<TIdentifier> : IMongoEntity<TIdentifier>
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public TIdentifier Id { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime UpdatedDate { get; set; }

        public virtual bool IsTransient()
        {
            return Id == null || Id.Equals(default);
        }
    }
}