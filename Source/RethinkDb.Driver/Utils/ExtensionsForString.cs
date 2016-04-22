namespace RethinkDb.Driver.Utils
{
    internal static class ExtensionsForString
    {
        internal static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }
        internal static bool IsNullOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        internal static bool IsNotNullOrEmpty(this string str)
        {
            return !string.IsNullOrEmpty(str);
        }
        internal static bool IsNotNullOrWhiteSpace(this string str)
        {
            return !string.IsNullOrWhiteSpace(str);
        }
    }
}