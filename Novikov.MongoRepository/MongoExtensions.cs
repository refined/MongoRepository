namespace Novikov.MongoRepository
{
    internal static class MongoExtensions
    {
        public static string OrLocalHost(this string mongoUrl)
        {
            return mongoUrl ?? "mongodb://localhost";
        }

        public static string OrDefaultDbName<TEntity, TIdentifier>(this string dbName) where TEntity : class, IMongoEntity<TIdentifier>
        {
            return !string.IsNullOrWhiteSpace(dbName)
                ? dbName
                : $"{typeof(TEntity).Name}DB";
        }

        public static string OrDefaultCollectionName<TEntity, TIdentifier>(this string collectionName) where TEntity : class, IMongoEntity<TIdentifier>
        {
            return !string.IsNullOrWhiteSpace(collectionName)
                ? collectionName
                : typeof(TEntity).Name;
        }
    }
}
