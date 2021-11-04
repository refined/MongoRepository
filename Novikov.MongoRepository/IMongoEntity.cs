using System;

namespace Novikov.MongoRepository
{
    public interface IMongoEntity<TIdentifier>
    {
        TIdentifier Id { get; set; }

        DateTime CreatedDate { get; set; }

        DateTime UpdatedDate { get; set; }

        bool IsTransient();
    }
}
