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

    public static class InterfaceEntityExtension
    {
        public static bool IsTransient<TIdentifier>(this IEntity<TIdentifier> entity)
        {
            return entity.Id == null || entity.Id.Equals(default(TIdentifier));
        }
    }
}
