using FluentFs.Core;

namespace Builder.Extensions
{
    public static class ExtensionsForString
    {
        public static string With( this string format, params object[] args )
        {
            return string.Format( format, args );
        }
    }

    public static class ExtensionsForBuildFolders
    {
        public static Directory Wipe(this Directory f)
        {
            return f.Delete( OnError.Continue ).Create();
        }
    }
}