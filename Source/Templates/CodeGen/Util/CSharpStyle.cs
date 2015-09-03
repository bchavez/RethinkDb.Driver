using Humanizer;

namespace Templates.CodeGen.Util
{
    public static class CSharpStyle
    {
        public static string ClassName( this string str)
        {
            return str.Pascalize();
        }

        public static string MethodName( this string str)
        {
            return str.Camelize();
        }

        public static string PropertyName( this string str)
        {
            return str.Pascalize();
        }

        public static string ArgumentName(this string str)
        {
            return str.Camelize();
        }

        public static string ArgumentTypeName(this string str)
        {
            if( str == "Object..." )
                return "params object[]";
            return str.Pascalize();
        }
    }
}