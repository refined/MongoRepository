using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Novikov.MongoRepository
{
    public class MongoRepository<TEntity, TIdentifier>
        where TEntity : class, IMongoEntity<TIdentifier>
    {
        public string CollectionName { get; }

        protected readonly IMongoCollection<TEntity> Collection;

        public IMongoIndexManager<TEntity> Indexes => Collection.Indexes;

        public MongoRepository(IOptions<MongoDbSettings> options, string collectionName = null) : this(options.Value, collectionName)
        {
        }

        public MongoRepository(MongoDbSettings options, string collectionName = null) : this(options.Url, options.DbName, collectionName ?? typeof(TEntity).Name)
        {
        }

        public MongoRepository(string mongoUrl = null, string dbName = null, string collectionName = null)
            : this(MongoClientSettings.FromConnectionString(mongoUrl.OrLocalHost()), dbName, collectionName)
        {
        }

        public MongoRepository(MongoClientSettings clientSettings, string dbName = null, string collectionName = null)
        {
            CollectionName = collectionName.OrDefaultCollectionName<TEntity, TIdentifier>();
            Collection = new MongoClient(clientSettings)
                .GetDatabase(dbName.OrDefaultDbName<TEntity, TIdentifier>())
                .GetCollection<TEntity>(collectionName);
        }

        public Task<TEntity> FirstOrDefault(Expression<Func<TEntity, bool>> searchExpression, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Find(searchExpression).FirstOrDefaultAsync(cancellationToken);
        }

        public Task<TEntity> UpdateField<TField>(
            Expression<Func<TEntity, bool>> searchExpression,
            Expression<Func<TEntity, TField>> fieldExpression,
            TField fieldValue,
            CancellationToken cancellationToken = default)
        {
            var updateDef = Builders<TEntity>
                .Update
                .Set(fieldExpression, fieldValue)
                .Set(e => e.UpdatedDate, DateTime.UtcNow);
            var updateOptions = new FindOneAndUpdateOptions<TEntity> { ReturnDocument = ReturnDocument.After };
            return Collection.FindOneAndUpdateAsync(searchExpression, updateDef, updateOptions, cancellationToken);
        }

        public async Task<TEntity> Update(
            TEntity entity,
            IEnumerable<string> fieldsToCopyFromPrev,
            CancellationToken cancellationToken = default)
        {
            var prev = await Collection
                .Find(Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id))
                .Limit(1)
                .FirstOrDefaultAsync(cancellationToken: cancellationToken);
            if (prev == null)
            {
                await Save(entity, cancellationToken);
                return entity;
            }

            entity.UpdatedDate = DateTime.UtcNow;
            var bsonPrev = prev.ToBsonDocument();
            var bson = entity.ToBsonDocument();
            bson.SetElement(bsonPrev.GetElement(nameof(entity.CreatedDate)));

            foreach (var field in fieldsToCopyFromPrev)
            {
                bson.SetElement(bsonPrev.GetElement(field));
            }            

            return await Collection.FindOneAndUpdateAsync(
                Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id),
                bson,
                new FindOneAndUpdateOptions<TEntity> { ReturnDocument = ReturnDocument.After },
                cancellationToken
            );
        }

        public async Task<TEntity> Get(TIdentifier id, CancellationToken cancellationToken = default)
        {
            return await Collection.Find(x => x.Id.Equals(id))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<TEntity>> FindAll(Expression<Func<TEntity, bool>> searchExpression, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Find(searchExpression)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<TIdentifier> Save(TEntity entity, CancellationToken cancellationToken = default)
        {
            entity.UpdatedDate = DateTime.UtcNow;
            if (entity.IsTransient())
            {
                entity.CreatedDate = DateTime.UtcNow;
                await Collection
                    .InsertOneAsync(entity, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await Collection
                     .ReplaceOneAsync(
                         Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id),
                         entity,
                         new ReplaceOptions { IsUpsert = true },
                         cancellationToken)
                     .ConfigureAwait(false);
            }
            return entity.Id;
        }

        public async Task Delete(TIdentifier id, CancellationToken cancellationToken = default(CancellationToken))
        {
            await Collection
                .DeleteOneAsync(x => x.Id.Equals(id), cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task DeleteBulk(Expression<Func<TEntity, bool>> searchExpression, CancellationToken cancellationToken = default)
        {
            await Collection
                .DeleteManyAsync(searchExpression, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<int> Count(Expression<Func<TEntity, bool>> searchExpression, CancellationToken cancellationToken = default)
        {
            var count = await Find(searchExpression)
                .CountDocumentsAsync(cancellationToken)
                .ConfigureAwait(false);

            return (int)count;
        }

        public async Task BulkInsert(ICollection<TEntity> entities, CancellationToken cancellationToken = default)
        {
            foreach (var entity in entities)
            {
                entity.CreatedDate = DateTime.UtcNow;
                entity.UpdatedDate = DateTime.UtcNow;
            }

            await Collection
                .InsertManyAsync(entities, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task BulkUpsert(ICollection<TEntity> entities, CancellationToken cancellationToken = default)
        {
            var operations = entities.Select(entity =>
            {
                if (entity.IsTransient())
                {
                    entity.CreatedDate = DateTime.UtcNow;
                    entity.UpdatedDate = DateTime.UtcNow;
                    return new InsertOneModel<TEntity>(entity) as WriteModel<TEntity>;
                }

                entity.UpdatedDate = DateTime.UtcNow;
                return new ReplaceOneModel<TEntity>(Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id), entity) as WriteModel<TEntity>;
            });

            await Collection
                .BulkWriteAsync(operations, new BulkWriteOptions { IsOrdered = false }, cancellationToken)
                .ConfigureAwait(false);
        }

        protected IFindFluent<TEntity, TEntity> Find(Expression<Func<TEntity, bool>> searchExpression)
        {
            var find = Collection.Find(searchExpression);

            //if (sort != null)
            //{
            //    var sortBuilder = Builders<TEntity>.Sort;
            //    var sortDefinitions = sort.Select(sortBy => sortBy.Direction == SortDirection.Ascending
            //            ? sortBuilder.Ascending(sortBy.Expression)
            //            : sortBuilder.Descending(sortBy.Expression))
            //        .ToList();

            //    find = find.Sort(sortBuilder.Combine(sortDefinitions));
            //}

            return find;
        }

        public async Task DropCollection()
        {
            await Collection.Database.DropCollectionAsync(CollectionName);
        }

        public IMongoQueryable<TEntity> AsQueryable()
        {
            return Collection.AsQueryable();
        }
    }
}