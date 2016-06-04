using System;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Extras.Dao;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.Dao
{
    public class MyDoc : Document<Guid>
    {
        public string Foo { get; set; }
        public string Bar { get; set; }
    }

    public class MyDocDao : RethinkDao<MyDoc,Guid>
    {
        public MyDocDao(IConnection conn, string dbName, string tableName) : base(conn, dbName, tableName)
        {
        }
    }

    [TestFixture]
    public class DaoTestFixture : QueryTestFixture
    {
        private MyDocDao dao;

        [SetUp]
        public void BeforeEachTest()
        {
            this.dao = new MyDocDao(conn, "query", "test");
        }


        [Test]
        public void should_have_error_if_doc_aready_exists()
        {
            ClearDefaultTable();

            var doc = new MyDoc
                {
                    Id = new Guid("3D6279F5-256F-4E94-BDF2-75FE8096140E"),
                    Foo = "Foooo",
                    Bar = "Baaar"
                };

            dao.Save(doc);
            Action act = () => {
                                   dao.Save(doc);
            };

            act.ShouldThrow<ReqlAssertFailure>();
        }

        [Test]
        public void update_should_throw_if_document_doesnt_exist()
        {
            ClearDefaultTable();

            var doc = new MyDoc
                {
                    Id = new Guid("3E8F4FCE-A6B8-4236-96D9-F4A479B5FA92"),
                    Foo = "update",
                    Bar = "should throw"
                };

            Action act = () =>
                {
                    dao.Update(doc);
                };

            act.ShouldThrow<ReqlAssertFailure>();
        }

        [Test]
        public void save_or_update()
        {
            ClearDefaultTable();
            var doc = new MyDoc
                {
                    Foo = "SaveOrUpdate",
                    Bar = "SaveOrUpdate"
                };

            var newDoc = dao.SaveOrUpdate(doc);
            newDoc.Id.Should().NotBeEmpty();

            newDoc.Dump();

            newDoc.Bar = "BarUpdate";

            var updated = dao.SaveOrUpdate(newDoc);

            updated.Id.Should().Be(newDoc.Id);
            updated.Bar.Should().Be(newDoc.Bar);
        }

        [Test]
        public void delete()
        {
            ClearDefaultTable();
            var doc = new MyDoc
                {
                    Foo = "SaveOrUpdate",
                    Bar = "SaveOrUpdate"
                };

            var newDoc = dao.Save(doc);
            newDoc.Id.Should().NotBeEmpty();

            dao.Delete(newDoc);

            dao.GetById(newDoc.Id).Should().BeNull();
        }
    }

}