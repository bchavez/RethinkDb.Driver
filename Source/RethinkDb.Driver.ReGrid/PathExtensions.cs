using System.IO;

namespace RethinkDb.Driver.ReGrid
{
    internal static class PathExtensions
    {
        public static string SafePath(this string filename)
        {
            var rootedPath = Path.IsPathRooted(filename) ? filename : Path.Combine("/", filename);
            if( rootedPath.EndsWith("/") )
            {
                throw new InvalidPathException($"The filename is not valid: {filename}. Specify a filename without trailing /.");
            }
            return rootedPath;
        }

        public static string SafePrefix(this string path)
        {
            var rootedPath = Path.IsPathRooted(path) ? path : Path.Combine("/", path);
            return rootedPath;
        }
    }
}