using System.Threading.Tasks;
using NUnit.Framework;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.ReGrid.Tests
{
    [TestFixture]
    public class PrefixTest : BucketTest
    {
        [Test]
        public async Task can_list_prefix()
        {
            ClearBucket();

            TestFiles.DifferentPathsAndRevisions(bucket);

            var files = await bucket.ListFilesByPrefixAsync("/animals");

            foreach( var fileInfo in files )
            {
                fileInfo.Dump();
            }
        }
    }
}