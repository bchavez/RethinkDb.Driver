using NUnit.Framework;

namespace RethinkDb.Driver.ReGrid.Tests
{
    [TestFixture]
    public class DeleteTests : BucketTest
    {
        [Test]
        public void delete_test()
        {
            CreateBucketWithTwoFileRevisions();

            //bucket.FindFile(testFile);

        }
    }
}