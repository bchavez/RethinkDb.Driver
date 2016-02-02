using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace RethinkDb.Driver.ReGrid.Tests
{
    [TestFixture]
    public class DeleteTests : BucketTest
    {
        [Test]
        public void soft_delete_test()
        {
            CreateBucketWithOneFileTwoChunks();

            var file = bucket.GetFileInfoByName(testfile);

            //soft delete
            bucket.DeleteRevision(file.Id, mode: DeleteMode.Soft);

            Action act = () => file = bucket.GetFileInfoByName(testfile);

            act.ShouldThrow<FileNotFoundException>();

            var deletedFile = GridUtility.EnumerateFileEntries(bucket, testfile, Status.Deleted)
                .FirstOrDefault();

            deletedFile.Should().NotBeNull();

            file.Id.Should().Be(deletedFile.Id);

        }

        [Test]
        public void hard_delete_test()
        {
            CreateBucketWithOneFileTwoChunks();

            var file = bucket.GetFileInfoByName(testfile);

            //soft delete
            bucket.DeleteRevision(file.Id, mode: DeleteMode.Hard);

            var fileEntries = GridUtility.EnumerateFileEntries(bucket, testfile)
                .ToList();

            fileEntries.Should().BeEmpty();


            var chunks = GridUtility.EnumerateChunks(bucket, file.Id)
                .ToList();

            chunks.Should().BeEmpty();

        }

        [Test]
        public void delete_all_revisions_soft_delete()
        {
            CreateBucketWithTwoFileRevisions();

            bucket.DeleteAllRevisions(testfile, mode: DeleteMode.Soft);


            var fileEntries = GridUtility.EnumerateFileEntries(bucket, testfile)
                .ToList();

            foreach( var fileInfo in fileEntries )
            {
                fileInfo.Status.Should().Be(Status.Deleted);
            }
        }

        [Test]
        public void delete_all_revisions_hard_delete()
        {
            CreateBucketWithTwoFileRevisions();

            bucket.DeleteAllRevisions(testfile, mode: DeleteMode.Hard);

            var fileEntries = GridUtility.EnumerateFileEntries(bucket, testfile)
                .ToList();

            fileEntries.Should().BeEmpty();
        }
    }
}