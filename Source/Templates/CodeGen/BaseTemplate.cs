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

        public RazorTemplateBase ChildTemplate { get; set; }

        public override string RenderSection(string name)
        {
            //render child first
            if( this.ChildTemplate != null && this.ChildTemplate.sections.ContainsKey(name) )
            {
                //check child if they have a section.

                //render it. because we need it
                var content = this.content;
                this.ChildTemplate.Clear();
                this.ChildTemplate.sections[name]();
                var sectionContent = this.ChildTemplate.generatingEnvironment.ToString();
                this.content = content;
                return sectionContent;

            }
            else
            {
                return base.RenderSection(name);
            }
        }

        public virtual RazorTemplateBase UseParentLayout(RazorTemplateBase child)
        {
            return null;
        }
    }
}