using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Novikov.MongoRepository.Test
{
    public class TestEntityFromInterface : IMongoEntity<string>
    {
        public string? MyVar { get; set; }
        public string Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        public bool IsTransient()
        {
            return Id == null;
        }
    }

    public class TestRepositoryInterface
    {
        private MongoRepository<TestEntityFromInterface, string>? _repository;

        [SetUp]
        public void Setup()
        {
            PublicMongoExtensions.RegisterObjectIdMapper<TestEntityFromInterface, string>();
            _repository = new MongoRepository<TestEntityFromInterface, string>();
        }

        [Test]
        public async Task Test1()
        {
            string? id = await _repository?.Save(new TestEntityFromInterface { MyVar = "1" });
            Assert.NotNull(id);

            await _repository.UpdateField(r => r.Id == id, r => r.MyVar, "2");

            var entity = await _repository.Get(id);
            Assert.AreEqual(id, entity.Id);
            Assert.AreEqual("2", entity.MyVar);

            entity = await _repository.Get(entity.Id);
        }
    }
}