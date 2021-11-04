using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace Novikov.MongoRepository
{
    public static class PublicMongoExtensions
    {
        public static void RegisterObjectIdMapper()
        {
            BsonClassMap.RegisterClassMap<IMongoEntity<string>>(cm =>
            {
                cm.AutoMap();
                cm.MapIdProperty(c => c.Id)
                    .SetIdGenerator(StringObjectIdGenerator.Instance)
                    .SetSerializer(new StringSerializer(BsonType.ObjectId));
                cm.SetIgnoreExtraElements(true);
                cm.SetIgnoreExtraElementsIsInherited(true);
            });

            BsonClassMap.RegisterClassMap<IMongoEntity<ObjectId>>(cm =>
            {
                cm.AutoMap();
                cm.SetIgnoreExtraElements(true);
                cm.SetIgnoreExtraElementsIsInherited(true);
            });
        }

        public static IServiceCollection AddRepositoriesBsonMapper(this IServiceCollection services)
        {
            var conventionPack = new ConventionPack { new IgnoreExtraElementsConvention(true) };
            ConventionRegistry.Register("IgnoreExtraElements", conventionPack, type => true);

            BsonClassMap.RegisterClassMap<MongoEntity> (cm =>
            {
                cm.AutoMap();
                cm.MapIdProperty(c => c.Id)
                    .SetIdGenerator(StringObjectIdGenerator.Instance)
                    .SetSerializer(new StringSerializer(BsonType.ObjectId));
                cm.SetIgnoreExtraElements(true);
                cm.SetIgnoreExtraElementsIsInherited(true);
            });

            return services;
        }
    }
}