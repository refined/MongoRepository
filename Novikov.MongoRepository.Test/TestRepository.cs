using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Novikov.MongoRepository.Test
{
    public class TestEntity : MongoEntity<string>
    {
        public string? MyVar { get; set; }
    }

    public class TestRepository
    {
        private MongoRepository<TestEntity, string> _repository;

        [SetUp]
        public void Setup()
        {
            _repository = new MongoRepository<TestEntity, string>();
        }

        [TearDown]
        public async Task TearDown()
        {
            await _repository.DropCollection();
        }

        [Test]
        public async Task Test1()
        {
            string? id = await _repository.Save(new TestEntity { MyVar = "1" });
            Assert.NotNull(id);
            
            await _repository.UpdateField(r => r.Id == id, r => r.MyVar, "2");

            var entity = await _repository.Get(id);
            Assert.AreEqual(id, entity.Id);
            Assert.AreEqual("2", entity.MyVar);

            entity = await _repository.Get(entity.Id);
        }

        [Test]
        public async Task Test2()
        {
            var now = DateTime.UtcNow;
            string id = await _repository.Save(new TestEntity { MyVar = "1" });
            Assert.NotNull(id);

            await _repository.Update(new TestEntity { Id = id, MyVar = "2" }, Array.Empty<string>());

            var entity = await _repository.Get(id);
            Assert.AreEqual(id, entity.Id);
            Assert.AreEqual("2", entity.MyVar);
            Assert.Greater(entity.UpdatedDate, entity.CreatedDate);

            IEnumerable<string> e = Enumerable.Empty<string>();
            e = e.Append("1");
            Assert.AreEqual("1", e.First());
        }
    }
}