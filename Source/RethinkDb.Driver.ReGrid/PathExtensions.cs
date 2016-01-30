using System.IO;

namespace RethinkDb.Driver.ReGrid
{
    public static class PathExtensions
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
    }
}