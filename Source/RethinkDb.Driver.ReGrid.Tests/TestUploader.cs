using System.Threading;

namespace RethinkDb.Driver.ReGrid.Tests
{
    public static class TestFiles
    {
        public static void DifferentPathsAndRevisions(Bucket bucket)
        {
            bucket.Upload("/monkey.mp4", TestBytes.NoChunks);
            bucket.Upload("/animals/dog.mp4", TestBytes.NoChunks);
            Thread.Sleep(1500);
            bucket.Upload("/animals/cat.mp4", TestBytes.NoChunks);
            Thread.Sleep(1500);
            bucket.Upload("/animals/cat.mp4", TestBytes.NoChunks);
            Thread.Sleep(1500);
            bucket.Upload("/animals/cat.mp4", TestBytes.NoChunks);
            bucket.Upload("/animals/fish.mp4", TestBytes.NoChunks);
            bucket.Upload("/people/father.mp4", TestBytes.NoChunks);
            bucket.Upload("/people/mother.mp4", TestBytes.NoChunks);
        }

        public static void DifferentPathsNoRevisions(Bucket bucket)
        {
            bucket.Upload("/monkey.mp4", TestBytes.NoChunks);
            bucket.Upload("/animals/dog.mp4", TestBytes.NoChunks);
            bucket.Upload("/animals/cat.mp4", TestBytes.NoChunks);
            bucket.Upload("/animals/fish.mp4", TestBytes.NoChunks);
            bucket.Upload("/people/father.mp4", TestBytes.NoChunks);
            bucket.Upload("/people/mother.mp4", TestBytes.NoChunks);
        }
    }
}