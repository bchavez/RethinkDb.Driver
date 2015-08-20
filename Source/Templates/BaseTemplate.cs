using Humanizer;
using RazorGenerator.Templating;

namespace Templates
{
    public class BaseTemplate : RazorTemplateBase
    {
        public string Class(string str)
        {
            return str.Pascalize();
        }

        public string Method(string str)
        {
            return str.Camelize();
        }

        public string Property(string str)
        {
            return str.Pascalize();
        }

        public string Argument(string str)
        {
            return str.Camelize();
        }
    }
}