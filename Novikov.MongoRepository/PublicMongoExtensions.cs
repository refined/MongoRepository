using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace Novikov.MongoRepository
{
    public static class PublicMongoExtensions
    {
        public static void RegisterObjectIdMapper()
        {
            BsonClassMap.RegisterClassMap<IEntity<string>>(cm =>
            {
                cm.AutoMap();
                cm.MapIdProperty(c => c.Id)
                    .SetIdGenerator(StringObjectIdGenerator.Instance)
                    .SetSerializer(new StringSerializer(BsonType.ObjectId));
                cm.SetIgnoreExtraElements(true);
                cm.SetIgnoreExtraElementsIsInherited(true);
            });

            BsonClassMap.RegisterClassMap<IEntity<ObjectId>>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
                cm.SetIgnoreExtraElementsIsInherited(true);
            });
        }
    }
}