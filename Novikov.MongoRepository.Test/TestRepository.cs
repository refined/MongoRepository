using NUnit.Framework;
using System.Threading.Tasks;

namespace Novikov.MongoRepository.Test
{
    public class TestEntity : MongoEntity<string>
    {
        public string? MyVar { get; set; }
    }

    public class TestRepository
    {
        private MongoRepository<TestEntity, string>? _repository;

        [SetUp]
        public void Setup()
        {
            PublicMongoExtensions.RegisterObjectIdMapper<string>();
            _repository = new MongoRepository<TestEntity, string>();
        }

        [Test]
        public async Task Test1()
        {
            string? id = await _repository?.Save(new TestEntity { MyVar = "1" });
            Assert.NotNull(id);
            
            await _repository.UpdateField(r => r.Id == id, r => r.MyVar, "2");

            var entity = await _repository.Get(id);
            Assert.AreEqual(id, entity.Id);
            Assert.AreEqual("2", entity.MyVar);

            entity = await _repository.Get(entity.Id);
        }
    }
}