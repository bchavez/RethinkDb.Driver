using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace RethinkDb.Driver.ReGrid.Tests
{
    [TestFixture]
    public class DownloadTests : BucketTest
    {

        [Test]
        public void test_download_as_bytes()
        {
            CreateBucketWithTwoFileRevisions();

            Console.WriteLine(">>>>> DOWNLOAD");
            var bytes = bucket.DownloadAsBytesByName(testFile);
            bytes.Should().Equal(TestBytes.OneHalfChunkReversed);

            bytes = bucket.DownloadAsBytesByName(testFile, revision: 0);
            bytes.Should().Equal(TestBytes.OneHalfChunk);
        }


        [Test]
        public void test_download_as_steream()
        {
            CreateBucketWithTwoFileRevisions();


            Console.WriteLine(">>>>> DOWNLOAD LATEST");
            var fs = File.Open("foobar_latest.mp3", FileMode.Create);
            bucket.DownloadToStreamByName(testFile, fs);
            fs.Close();

            var outBytes = File.ReadAllBytes("foobar_latest.mp3");

            TestBytes.OneHalfChunkReversed.Should().Equal(outBytes);



            Console.WriteLine(">>>>> DOWNLOAD ORIGINAL");

            fs = File.Open("foobar_original.mp3", FileMode.Create);
            bucket.DownloadToStreamByName(testFile, fs, revision: 0);
            fs.Close();

            outBytes = File.ReadAllBytes("foobar_original.mp3");

            TestBytes.OneHalfChunk.Should().Equal(outBytes);


        }

    }
}