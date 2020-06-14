using System;

namespace Novikov.MongoRepository
{
    public interface IEntity<TIdentifier>
    {
        TIdentifier Id { get; set; }

        DateTime CreatedDate { get; set; }

        DateTime UpdatedDate { get; set; }

        bool IsTransient();
    }
}
