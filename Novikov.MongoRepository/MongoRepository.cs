﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Novikov.MongoRepository
{
    public class MongoRepository<TEntity, TIdentifier>
        where TEntity : class, IEntity<TIdentifier>
    {
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
            Collection = new MongoClient(clientSettings)
                .GetDatabase(dbName.OrDefaultDbName<TEntity, TIdentifier>())
                .GetCollection<TEntity>(collectionName.OrDefaultCollectionName<TEntity, TIdentifier>());
        }


        public Task<TEntity> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> searchExpression, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Find(searchExpression).FirstOrDefaultAsync(cancellationToken);
        }

        public Task<TEntity> UpdateAsync<TField>(
            Expression<Func<TEntity, bool>> searchExpression,
            Expression<Func<TEntity, TField>> fieldExpression,
            TField fieldValue,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var updateDef = Builders<TEntity>.Update.Set(fieldExpression, fieldValue);
            var filter = searchExpression;
            var updateOptions = new FindOneAndUpdateOptions<TEntity> { ReturnDocument = ReturnDocument.After };
            return Collection.FindOneAndUpdateAsync(filter, updateDef, updateOptions, cancellationToken);
        }

        public async Task<TEntity> GetAsync(TIdentifier id, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Collection.Find(x => x.Id.Equals(id))
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public TEntity Get(TIdentifier id)
        {
            return Collection.Find(x => x.Id.Equals(id)).FirstOrDefault();
        }

        public async Task<IReadOnlyCollection<TEntity>> FindAllAsync(Expression<Func<TEntity, bool>> searchExpression, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await Find(searchExpression)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public IReadOnlyCollection<TEntity> FindAll(Expression<Func<TEntity, bool>> searchExpression)
        {
            return Find(searchExpression).ToList();
        }

        public async Task SaveAsync(TEntity entity, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (entity.IsTransient())
            {
                entity.CreatedDate = DateTime.UtcNow;
                entity.UpdatedDate = DateTime.UtcNow;
                await Collection
                    .InsertOneAsync(entity, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                entity.UpdatedDate = DateTime.UtcNow;
                await Collection
                    .ReplaceOneAsync(
                        Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id), 
                        entity, 
                        new ReplaceOptions { IsUpsert = true },
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public void Save(TEntity entity)
        {
            if (entity.IsTransient())
            {
                entity.CreatedDate = DateTime.UtcNow;
                entity.UpdatedDate = DateTime.UtcNow;
                Collection.InsertOne(entity);
            }
            else
            {
                entity.UpdatedDate = DateTime.UtcNow;
                Collection.ReplaceOne(
                    Builders<TEntity>.Filter.Eq(e => e.Id, entity.Id), 
                    entity, 
                    new ReplaceOptions { IsUpsert = true });
            }
        }

        public async Task DeleteAsync(TIdentifier id, CancellationToken cancellationToken = default(CancellationToken))
        {
            await Collection
                .DeleteOneAsync(x => x.Id.Equals(id), cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task DeleteBulkAsync(Expression<Func<TEntity, bool>> searchExpression, CancellationToken cancellationToken = default)
        {
            await Collection
                .DeleteManyAsync(searchExpression, cancellationToken)
                .ConfigureAwait(false);
        }

        public void Delete(TIdentifier id)
        {
            Collection.DeleteOne(x => x.Id.Equals(id));
        }

        public async Task<int> CountAsync(Expression<Func<TEntity, bool>> searchExpression, CancellationToken cancellationToken = default(CancellationToken))
        {
            var count = await Find(searchExpression)
                .CountDocumentsAsync(cancellationToken)
                .ConfigureAwait(false);

            return (int)count;
        }

        public int Count(Expression<Func<TEntity, bool>> searchExpression)
        {
            var count = Find(searchExpression).CountDocuments();
            return (int)count;
        }

        public async Task BulkInsertAsync(ICollection<TEntity> entities, CancellationToken cancellationToken = default(CancellationToken))
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

        public void BulkInsert(ICollection<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                entity.CreatedDate = DateTime.UtcNow;
                entity.UpdatedDate = DateTime.UtcNow;
            }

            Collection.InsertMany(entities);
        }

        public async Task BulkUpsertAsync(ICollection<TEntity> entities, CancellationToken cancellationToken = default(CancellationToken))
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

        public void BulkUpsert(ICollection<TEntity> entities)
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

            Collection.BulkWrite(operations, new BulkWriteOptions { IsOrdered = false });
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

        public IQueryable<TEntity> AsQueryable()
        {
            return Collection.AsQueryable();
        }
    }
}