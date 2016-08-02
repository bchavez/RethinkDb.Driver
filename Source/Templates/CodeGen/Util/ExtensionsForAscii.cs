using System;
using System.Text;

namespace Templates.CodeGen.Util
{
    public static class ExtensionsForAscii
    {
        internal static string GetAsciiStringAsLong(this string str)
        {
            if( str.Length != 8 ) throw new InvalidCastException("The string must be exactly 8 characters long.");
            var ascii = Encoding.ASCII.GetBytes(str);
            return $"{BitConverter.ToInt64(ascii, 0)}; // {str}";
        }
    }
}