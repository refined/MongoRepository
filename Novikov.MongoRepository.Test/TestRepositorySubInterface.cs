using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Novikov.MongoRepository.Test
{
    public class FixedClassWithId
    {
        public string Id { get; set; }
    }

    public class TestEntitySubInterface : FixedClassWithId, IMongoEntity<string>
    {
        public string? MyVar { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        public bool IsTransient()
        {
            return Id == null;
        }
    }

    public class TestRepositorySubInterface
    {
        private MongoRepository<TestEntitySubInterface, string>? _repository;

        [SetUp]
        public void Setup()
        {
            PublicMongoExtensions.RegisterStringIdConvention();
            _repository = new MongoRepository<TestEntitySubInterface, string>();
        }

        [Test]
        public async Task Test1()
        {
            string? id = await _repository?.Save(new TestEntitySubInterface { MyVar = "10" });
            Assert.NotNull(id);

            await _repository.UpdateField(r => r.Id == id, r => r.MyVar, "20");

            var entity = await _repository.Get(id);
            Assert.AreEqual(id, entity.Id);
            Assert.AreEqual("20", entity.MyVar);

            entity = await _repository.Get(entity.Id);
        }
    }
}