﻿using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Bson.Serialization.Serializers;

namespace Novikov.MongoRepository
{
    public static class PublicMongoExtensions
    {
        public static void RegisterObjectIdMapper<TEntity, TIdentifier>() 
            where TEntity : class, IMongoEntity<TIdentifier>
        {
            var conventionPack = new ConventionPack { new IgnoreExtraElementsConvention(true) };
            ConventionRegistry.Register("IgnoreExtraElements", conventionPack, type => true);

            BsonClassMap.RegisterClassMap<TEntity>(cm =>
            {
                cm.AutoMap();
                cm.MapIdProperty(c => c.Id)
                    .SetIdGenerator(StringObjectIdGenerator.Instance)
                    .SetSerializer(new StringSerializer(BsonType.ObjectId));
                cm.SetIgnoreExtraElements(true);
                cm.SetIgnoreExtraElementsIsInherited(true);
            });
        }

        public static IServiceCollection AddRepositoriesBsonMapper<TEntity, TIdentifier>(this IServiceCollection services)
            where TEntity : class, IMongoEntity<TIdentifier>
        {
            RegisterObjectIdMapper<TEntity, TIdentifier>();
            return services;
        }

        public static void RegisterStringIdConvention()
        {
            var conventionPack = new ConventionPack {
                new IgnoreExtraElementsConvention(true),
                new StringIdStoredAsObjectIdConvention()
            };
            ConventionRegistry.Register("Conversion for Id string and extra elements", conventionPack, type => true);
        }

        public static IServiceCollection AddRegisterStringIdConvention(this IServiceCollection services)
        {
            RegisterStringIdConvention();
            return services;
        }
    }
}