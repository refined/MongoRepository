using System;

namespace Novikov.MongoRepository
{
    [Serializable]
    public abstract class MongoEntity<TIdentifier> : IMongoEntity<TIdentifier>
    {
        public TIdentifier Id { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime UpdatedDate { get; set; }

        public virtual bool IsTransient()
        {
            return Id == null || Id.Equals(default);
        }
    }

    [Serializable]
    public abstract class MongoEntity : MongoEntity<string>
    {
    }    
}